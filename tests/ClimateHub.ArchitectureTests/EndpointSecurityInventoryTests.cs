using System.Text.Json;
using Xunit;

namespace ClimateHub.ArchitectureTests;

public class EndpointSecurityInventoryTests
{
    private static readonly string SolutionDir = FindSolutionDir();
    private static readonly string ArtifactsDir = Path.Combine(SolutionDir, "artifacts");
    private static readonly string InventoryPath = Path.Combine(ArtifactsDir, "endpoint-security-inventory.json");

    private static string FindSolutionDir()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            if (File.Exists(Path.Combine(dir, "ClimateHub.sln")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null || parent.FullName == dir) break;
            dir = parent.FullName;
        }
        throw new DirectoryNotFoundException("Solution dir not found");
    }

    public record EndpointEntry(
        string Route,
        string Method,
        string File,
        List<string> AuthFilters,
        bool HasBuildingAccess,
        bool HasPermissionClaim,
        bool IsAnonymous,
        string Notes);

    [Fact]
    public async Task GenerateEndpointSecurityInventory()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var endpointDir = Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints");
        var entries = new List<EndpointEntry>();

        foreach (var file in Directory.GetFiles(endpointDir, "*.cs").OrderBy(f => f))
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');
            var fileName = Path.GetFileName(file);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!line.Contains("MapGet") && !line.Contains("MapPost") &&
                    !line.Contains("MapPut") && !line.Contains("MapDelete"))
                    continue;

                var route = ExtractRoute(line, lines, i);
                if (route is null) continue;

                var method = line.Contains("MapGet") ? "GET" :
                    line.Contains("MapPost") ? "POST" :
                    line.Contains("MapPut") ? "PUT" :
                    line.Contains("MapDelete") ? "DELETE" : "UNKNOWN";

                var chain = string.Join(" ", lines.Skip(i).Take(30));

                var filters = new List<string>();
                if (chain.Contains("RequireBuildingAccess")) filters.Add("RequireBuildingAccess");
                if (chain.Contains("RequireRoomAccess")) filters.Add("RequireRoomAccess");
                if (chain.Contains("RequireDeviceAccess")) filters.Add("RequireDeviceAccess");
                if (chain.Contains("RequireNeedAccess")) filters.Add("RequireNeedAccess");
                if (chain.Contains("RequireCommandAccess")) filters.Add("RequireCommandAccess");
                if (chain.Contains("RequireEngineeringSystemAccess")) filters.Add("RequireEngineeringSystemAccess");
                if (chain.Contains("RequireFloorAccess")) filters.Add("RequireFloorAccess");
                if (chain.Contains("RequirePermission")) filters.Add("RequirePermission");
                if (chain.Contains("AllowAnonymous")) filters.Add("AllowAnonymous");
                if (chain.Contains("RequireAuthorization")) filters.Add("RequireAuthorization");

                var hasBuildingAccess = filters.Any(f =>
                    f is "RequireBuildingAccess" or "RequireRoomAccess" or
                    "RequireDeviceAccess" or "RequireNeedAccess" or
                    "RequireCommandAccess" or "RequireEngineeringSystemAccess" or
                    "RequireFloorAccess");
                var hasPermissionClaim = filters.Contains("RequirePermission");
                var isAnonymous = filters.Contains("AllowAnonymous");

                var notes = "";
                if (!hasBuildingAccess && !isAnonymous)
                    notes = "MISSING_BUILDING_LEVEL_ACCESS";
                if (!hasPermissionClaim && !isAnonymous && !filters.Contains("RequireAuthorization"))
                    notes = "MISSING_PERMISSION_CLAIM";
                if (route.Contains("/dev/") || route.Contains("/development"))
                    notes = "DEVELOPMENT_ENDPOINT";

                entries.Add(new EndpointEntry(route, method, fileName, filters, hasBuildingAccess, hasPermissionClaim, isAnonymous, notes));
            }
        }

        var json = JsonSerializer.Serialize(new { GeneratedAt = DateTimeOffset.UtcNow, TotalEndpoints = entries.Count, Endpoints = entries }, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(InventoryPath, json);
    }

    [Fact]
    public async Task AllWriteEndpointsRequireAuth()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var endpointDir = Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints");

        foreach (var file in Directory.GetFiles(endpointDir, "*.cs").OrderBy(f => f))
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!line.Contains("MapPost") && !line.Contains("MapPut") && !line.Contains("MapDelete"))
                    continue;

                var chain = string.Join(" ", lines.Skip(i).Take(50));
                var hasAuth = chain.Contains("RequirePermission") || chain.Contains("AllowAnonymous") ||
                    chain.Contains("RequireAuthorization") || chain.Contains("RequireBuildingAccess") ||
                    chain.Contains("RequireRoomAccess") || chain.Contains("RequireDeviceAccess") ||
                    chain.Contains("RequireNeedAccess") || chain.Contains("RequireCommandAccess") ||
                    chain.Contains("RequireEngineeringSystemAccess") || chain.Contains("RequireFloorAccess");

                Assert.True(hasAuth,
                    $"Write endpoint in {Path.GetFileName(file)} line {i + 1} missing authorization");
            }
        }
    }

    [Fact]
    public async Task HealthAndLoginAreTheOnlyAnonymousOperationalEndpoints()
    {
        var endpointDir = Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints");
        var anonymousRoutes = new List<string>();

        foreach (var file in Directory.GetFiles(endpointDir, "*.cs").OrderBy(f => f))
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!line.Contains("AllowAnonymous")) continue;

                var route = ExtractRoute(line, lines, i);
                if (route is not null)
                    anonymousRoutes.Add(route);
            }
        }

        foreach (var route in anonymousRoutes)
        {
            var allowed = route == "/health/live" || route == "/health/ready" ||
                route == "/api/v1/auth/login" || route == "/api/v1/auth/refresh";
            Assert.True(allowed, $"Unexpected anonymous endpoint: {route}");
        }
    }

    private static string? ExtractRoute(string line, string[] lines, int idx)
    {
        var mapMatch = System.Text.RegularExpressions.Regex.Match(line,
            @"Map(Get|Post|Put|Delete)\s*\(\s*""([^""]+)""");
        if (mapMatch.Success)
            return mapMatch.Groups[2].Value;

        for (int j = Math.Max(0, idx - 5); j <= idx; j++)
        {
            mapMatch = System.Text.RegularExpressions.Regex.Match(lines[j],
                @"Map(Get|Post|Put|Delete)\s*\(\s*""([^""]+)""");
            if (mapMatch.Success)
                return mapMatch.Groups[2].Value;
        }
        return null;
    }
}