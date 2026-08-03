using ClimateHub.Modules.EngineeringSystems.Application.Resolvers;
using ClimateHub.Modules.EngineeringSystems.Domain;
using Xunit;

namespace ClimateHub.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly string SolutionDir = FindSolutionDir();

    private static string FindSolutionDir()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            if (File.Exists(Path.Combine(dir, "ClimateHub.sln")))
                return dir;
            var parent = Path.GetDirectoryName(dir);
            if (parent is null || parent == dir) break;
            dir = parent;
        }
        throw new DirectoryNotFoundException($"Could not find solution dir from {AppContext.BaseDirectory}");
    }

    [Fact]
    public void NeedsDomain_ShouldNotReference_Infrastructure()
    {
        var refs = GetProjectReferences(@"src\Modules\Needs\ClimateHub.Modules.Needs.Domain\ClimateHub.Modules.Needs.Domain.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Infrastructure"));
    }

    [Fact]
    public void NeedsContracts_ShouldOnlyReference_SharedKernel()
    {
        var refs = GetProjectReferences(@"src\Modules\Needs\ClimateHub.Modules.Needs.Contracts\ClimateHub.Modules.Needs.Contracts.csproj");
        Assert.All(refs, r => Assert.StartsWith("ClimateHub.SharedKernel", r.Name));
    }

    [Fact]
    public void NeedsInfrastructure_ShouldNotReference_EnvironmentInfrastructure()
    {
        var refs = GetProjectReferences(@"src\Modules\Needs\ClimateHub.Modules.Needs.Infrastructure\ClimateHub.Modules.Needs.Infrastructure.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Environment.Infrastructure"));
    }

    [Fact]
    public void NeedsInfrastructure_ShouldNotReference_CommandsApplication()
    {
        var refs = GetProjectReferences(@"src\Modules\Needs\ClimateHub.Modules.Needs.Infrastructure\ClimateHub.Modules.Needs.Infrastructure.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Commands.Application"));
    }

    [Fact]
    public void CommandsDomain_ShouldNotReference_Needs()
    {
        var refs = GetProjectReferences(@"src\Modules\Commands\ClimateHub.Modules.Commands.Domain\ClimateHub.Modules.Commands.Domain.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Needs"));
    }

    [Fact]
    public void EnvironmentDomain_ShouldNotReference_Needs()
    {
        var refs = GetProjectReferences(@"src\Modules\Environment\ClimateHub.Modules.Environment.Domain\ClimateHub.Modules.Environment.Domain.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Needs"));
    }

    [Fact]
    public void DevicesDomain_ShouldNotReference_Needs()
    {
        var refs = GetProjectReferences(@"src\Modules\Devices\ClimateHub.Modules.Devices.Domain\ClimateHub.Modules.Devices.Domain.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Needs"));
    }

    [Fact]
    public void BuildingDomain_ShouldNotReference_Needs()
    {
        var refs = GetProjectReferences(@"src\Modules\Building\ClimateHub.Modules.Building.Domain\ClimateHub.Modules.Building.Domain.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Needs"));
    }

    [Fact]
    public void CommandsApplication_ShouldNotReference_NeedsInfrastructure()
    {
        var refs = GetProjectReferences(@"src\Modules\Commands\ClimateHub.Modules.Commands.Application\ClimateHub.Modules.Commands.Application.csproj");
        Assert.DoesNotContain(refs, r => r.Name.Contains("Needs.Infrastructure"));
    }

    [Fact]
    public void EveryEngineeringCapabilityCode_MapsToExactlyOneFamily()
    {
        var resolver = new EngineeringCapabilityFamilyResolver();
        var mappings = resolver.GetAllMappings();

        var capabilityFields = typeof(EngineeringCapabilityCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToHashSet();

        var mappedCodes = mappings.Keys.ToHashSet();

        var unmapped = capabilityFields.Except(mappedCodes).ToList();
        var extraMapped = mappedCodes.Except(capabilityFields).ToList();

        Assert.Empty(unmapped);
        Assert.Empty(extraMapped);
    }

    [Fact]
    public void NoDuplicateCapabilityFamilyRegistrations()
    {
        var resolver = new EngineeringCapabilityFamilyResolver();
        var exception = Record.Exception(() => resolver.ValidateNoOverlaps());
        Assert.Null(exception);
    }

    [Fact]
    public void CapabilityPlanner_HasNoDuplicateDeviceCapabilitySelection()
    {
        var needToEngFields = typeof(ClimateHub.Modules.Needs.Infrastructure.CapabilityPlanner)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name is "GetEngineeringCapabilityCode" or "GetCapabilityCode");

        Assert.NotEmpty(needToEngFields);
    }

    [Fact]
    public void CapabilityPlanner_EngMapping_Matches_EngineeringCapabilityCodes()
    {
        var plannerEngMapping = typeof(ClimateHub.Modules.Needs.Infrastructure.CapabilityPlanner)
            .GetMethod("GetEngineeringCapabilityCode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        var engCodesType = typeof(EngineeringCapabilityCodes);

        Assert.NotNull(plannerEngMapping);
        Assert.NotNull(engCodesType.GetField("IncreaseTemperature", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static));
    }

    [Fact]
    public void CapabilityPlanner_References_EngineeringCapabilityCodes_NotDuplicateStrings()
    {
        var needToEngField = typeof(ClimateHub.Modules.Needs.Infrastructure.CapabilityPlanner)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .FirstOrDefault(f => f.Name == "NeedToEngCapability" && f.FieldType == typeof(Dictionary<ClimateHub.Modules.Needs.Domain.NeedType, string>));

        Assert.NotNull(needToEngField);

        var dict = (System.Collections.IDictionary)needToEngField.GetValue(null)!;
        foreach (var entry in dict.Values)
        {
            var value = entry?.GetType().GetProperty("Value")?.GetValue(entry) as string;
            if (value is not null && value.StartsWith("eng."))
            {
                // All eng.* values should match a constant in EngineeringCapabilityCodes
                var constantFound = typeof(EngineeringCapabilityCodes)
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(string))
                    .Any(f => (string)f.GetValue(null)! == value);

                Assert.True(constantFound,
                    $"CapabilityPlanner uses '{value}' which is not a constant defined in EngineeringCapabilityCodes");
            }
        }
    }

    [Fact]
    public void NoDuplicateCapabilityToFamilyMappings()
    {
        var resolver = new EngineeringCapabilityFamilyResolver();
        var mappings = resolver.GetAllMappings();
        var seen = new HashSet<string>();
        foreach (var key in mappings.Keys)
        {
            Assert.True(seen.Add(key), $"Duplicate capability -> family mapping detected: {key}");
        }
    }

    [Fact]
    public async Task EndpointsWithRoomId_HaveRequireRoomAccessOrRequireBuildingAccess()
    {
        var endpointFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints"), "*.cs");

        foreach (var file in endpointFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\"{roomId}\"") || lines[i].Contains("\"{roomId}"))
                {
                    // Scan forward up to 20 lines to find the filter chain
                    var chain = string.Join(" ", lines.Skip(i).Take(20));
                    var hasAccess = chain.Contains("RequireRoomAccess") || chain.Contains("RequireBuildingAccess");
                    Assert.True(hasAccess,
                        $"File {Path.GetFileName(file)} line {i + 1}: route with {{roomId}} missing RequireRoomAccess or RequireBuildingAccess");
                }
            }
        }
    }

    [Fact]
    public async Task EndpointsWithDeviceId_HaveRequireDeviceAccessOrRequireBuildingAccess()
    {
        var endpointFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints"), "*.cs");

        foreach (var file in endpointFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\"{deviceId}\"") || lines[i].Contains("\"{deviceId}"))
                {
                    var chain = string.Join(" ", lines.Skip(i).Take(20));
                    var hasAccess = chain.Contains("RequireDeviceAccess") || chain.Contains("RequireBuildingAccess");
                    Assert.True(hasAccess,
                        $"File {Path.GetFileName(file)} line {i + 1}: route with {{deviceId}} missing RequireDeviceAccess or RequireBuildingAccess");
                }
            }
        }
    }

    [Fact]
    public async Task EndpointsWithNeedId_HaveRequireNeedAccessOrRequireBuildingAccess()
    {
        var endpointFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints"), "*.cs");

        foreach (var file in endpointFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\"{needId}\"") || lines[i].Contains("\"{needId}"))
                {
                    var chain = string.Join(" ", lines.Skip(i).Take(20));
                    var hasAccess = chain.Contains("RequireNeedAccess") || chain.Contains("RequireBuildingAccess");
                    Assert.True(hasAccess,
                        $"File {Path.GetFileName(file)} line {i + 1}: route with {{needId}} missing RequireNeedAccess or RequireBuildingAccess");
                }
            }
        }
    }

    [Fact]
    public async Task EndpointsWithCommandId_HaveRequireCommandAccess()
    {
        var endpointFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints"), "*.cs");

        foreach (var file in endpointFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\"{commandId}\"") || lines[i].Contains("\"{commandId}"))
                {
                    var chain = string.Join(" ", lines.Skip(i).Take(20));
                    var hasAccess = chain.Contains("RequireCommandAccess");
                    Assert.True(hasAccess,
                        $"File {Path.GetFileName(file)} line {i + 1}: route with {{commandId}} missing RequireCommandAccess");
                }
            }
        }
    }

    [Fact]
    public void EngCapabilityToDeviceCodes_HasNoDuplicateKeys()
    {
        var dictField = typeof(EngineeringCapabilityCodes)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .First(f => f.Name == "EngCapabilityToDeviceCodes" &&
                        f.FieldType == typeof(Dictionary<string, string[]>));

        var dict = (System.Collections.IDictionary)dictField.GetValue(null)!;

        var keys = new HashSet<string>();
        foreach (var key in dict.Keys)
        {
            var keyStr = key as string;
            Assert.NotNull(keyStr);
            Assert.True(keys.Add(keyStr), $"Duplicate key in EngCapabilityToDeviceCodes: {keyStr}");
        }
    }

    [Fact]
    public async Task EndpointsWithBuildingId_HaveRequireBuildingAccess()
    {
        var endpointFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Endpoints"), "*.cs");

        foreach (var file in endpointFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\"{buildingId}\"") || lines[i].Contains("\"{buildingId}"))
                {
                    var chain = string.Join(" ", lines.Skip(i).Take(20));
                    var hasAccess = chain.Contains("RequireBuildingAccess");
                    Assert.True(hasAccess,
                        $"File {Path.GetFileName(file)} line {i + 1}: route with {{buildingId}} missing RequireBuildingAccess");
                }
            }
        }
    }

    private List<ProjectReference> GetProjectReferences(string relativePath)
    {
        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(SolutionDir, normalized));
        var content = File.ReadAllText(fullPath);
        var refs = new List<ProjectReference>();

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("ProjectReference"))
            {
                var includeStart = trimmed.IndexOf("Include=\"") + 9;
                var includeEnd = trimmed.IndexOf("\"", includeStart);
                if (includeStart > 8 && includeEnd > includeStart)
                {
                    var include = trimmed[includeStart..includeEnd];
                    var name = Path.GetFileNameWithoutExtension(include);
                    refs.Add(new ProjectReference(name, include));
                }
            }
        }

        return refs;
    }

    public record ProjectReference(string Name, string Path);
}