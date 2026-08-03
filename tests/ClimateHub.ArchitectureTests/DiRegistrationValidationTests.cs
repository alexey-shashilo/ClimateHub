using System.Text.RegularExpressions;
using Xunit;

namespace ClimateHub.ArchitectureTests;

public class DiRegistrationValidationTests
{
    private static readonly string SolutionDir = FindSolutionDir();

    [Fact]
    public async Task ProgramFile_RegistersBuildingAccessHandlerAsScoped()
    {
        var programPath = Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Program.cs");
        var content = await File.ReadAllTextAsync(programPath);
        Assert.Contains("AddScoped<IAuthorizationHandler, ClimateHub.Api.Authorization.BuildingAccessHandler>()", content);
        Assert.Contains("AddSingleton<IAuthorizationHandler, ClimateHub.Api.Authorization.PermissionHandler>()", content);
    }

    [Fact]
    public async Task ProgramFile_NoAuthorizationHandlerIsSingletonWithScopedDependency()
    {
        var programPath = Path.Combine(SolutionDir, "src", "ClimateHub.Api", "Program.cs");
        var content = await File.ReadAllTextAsync(programPath);
        var singletonRegs = Regex.Matches(content, @"AddSingleton<IAuthorizationHandler,\s*([^>]+)>");
        var scopedRegs = Regex.Matches(content, @"AddScoped<IAuthorizationHandler,\s*([^>]+)>");
        Assert.NotEmpty(scopedRegs);
    }

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
}