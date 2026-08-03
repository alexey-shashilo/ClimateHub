using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace ClimateHub.ArchitectureTests;

public class DependencyRuleTests
{
    private static readonly string SolutionDir = FindSolutionDir();
    private static string Src(params string[] p) => Path.Combine(SolutionDir, "src", Path.Combine(p));
    private static string R(string rel) => Path.GetFullPath(Path.Combine(SolutionDir, rel.Replace('\\', Path.DirectorySeparatorChar)));

    [Fact]
    public void NeedDomain_ShouldNotReference_Engineering()
    {
        var refs = GetProjectReferences(R(@"src\Modules\Needs\ClimateHub.Modules.Needs.Domain\ClimateHub.Modules.Needs.Domain.csproj"));
        Assert.DoesNotContain(refs, r => r.Name.Contains("Engineering"));
    }

    [Fact]
    public void NeedInfrastructure_ShouldNotReference_EngineeringApplication()
    {
        var refs = GetProjectReferences(R(@"src\Modules\Needs\ClimateHub.Modules.Needs.Infrastructure\ClimateHub.Modules.Needs.Infrastructure.csproj"));
        Assert.DoesNotContain(refs, r => r.Name.Contains("EngineeringSystems.Application"));
    }

    [Fact]
    public void ClimateDomain_ShouldNotReference_Devices()
    {
        var refs = GetProjectReferences(R(@"src\Modules\Climate\ClimateHub.Modules.Climate.Domain\ClimateHub.Modules.Climate.Domain.csproj"));
        Assert.DoesNotContain(refs, r => r.Name.Contains("Devices"));
    }

    [Fact]
    public void EngineeringDomain_ShouldNotReference_EnvironmentInfrastructure()
    {
        var refs = GetProjectReferences(R(@"src\Modules\EngineeringSystems\ClimateHub.Modules.EngineeringSystems.Domain\ClimateHub.Modules.EngineeringSystems.Domain.csproj"));
        Assert.DoesNotContain(refs, r => r.Name.Contains("Environment.Infrastructure"));
    }

    [Fact]
    public void Api_ShouldNotReference_InfrastructureEntities()
    {
        var refs = GetProjectReferences(R(@"src\ClimateHub.Api\ClimateHub.Api.csproj"));
        var infraEntities = refs.Where(r =>
            r.Name.Contains("ClimateHub.Modules") && r.Name.Contains("Domain"));
        var allowedDomainRefs = new[]
        {
            "ClimateHub.Modules.IAM.Domain",
            "ClimateHub.Modules.EngineeringSystems.Domain"
        };
        foreach (var r in infraEntities)
        {
            if (!allowedDomainRefs.Any(a => r.Name.Contains(a)))
            {
                Assert.Fail($"API should not reference domain entities directly: {r.Name}");
            }
        }
    }

    [Fact]
    public async Task NoStaticCapabilityRegistry()
    {
        var diFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src"), "DependencyInjection.cs", SearchOption.AllDirectories);

        foreach (var file in diFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            Assert.DoesNotContain("AddSingleton<ICapabilityRegistry", content);
        }
    }

    [Fact]
    public void NoGuidEmptyBuildingId()
    {
        var srcDir = Path.Combine(SolutionDir, "src");
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin") && !f.Contains("Migrations"));

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Guid.Empty") &&
                    (lines[i].Contains("BuildingId") || lines[i].Contains("buildingId")))
                {
                    Assert.Fail($"File {file} line {i + 1}: BuildingId should not use Guid.Empty");
                }
            }
        }
    }

    [Fact]
    public void NoDuplicateCapabilityCodes()
    {
        var codesFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src"), "*CapabilityCodes*", SearchOption.AllDirectories);

        foreach (var file in codesFiles)
        {
            var content = File.ReadAllText(file);
            var matches = Regex.Matches(content, @"const string (\w+)\s*=\s*""([^""]+)""");
            var codes = new HashSet<string>();
            foreach (Match match in matches)
            {
                var code = match.Groups[2].Value;
                if (!codes.Add(code))
                {
                    Assert.Fail($"File {file}: Duplicate capability code '{code}' in {match.Groups[1].Value}");
                }
            }
        }
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_ApplicationFromOtherModules()
    {
        var infraProjectFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src"), "*.Infrastructure.csproj", SearchOption.AllDirectories);

        foreach (var file in infraProjectFiles)
        {
            var content = File.ReadAllText(file);
            var projectName = Path.GetFileNameWithoutExtension(file);
            var moduleName = ExtractModuleName(projectName);

            var refMatches = Regex.Matches(content, @"ProjectReference.*Include=""([^""]+)""");
            foreach (Match match in refMatches)
            {
                var refPath = match.Groups[1].Value;
                if (refPath.Contains("Application") && !refPath.Contains(moduleName))
                {
                    Assert.Fail($"{projectName} references Application from other module: {refPath}");
                }
            }
        }
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        var domainProjectFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src"), "*.Domain.csproj", SearchOption.AllDirectories);

        foreach (var file in domainProjectFiles)
        {
            var content = File.ReadAllText(file);
            var refs = Regex.Matches(content, @"ProjectReference.*Include=""([^""]+)""");
            foreach (Match match in refs)
            {
                var refPath = match.Groups[1].Value;
                if (refPath.Contains("Infrastructure"))
                {
                    Assert.Fail($"Domain project {Path.GetFileNameWithoutExtension(file)} references Infrastructure: {refPath}");
                }
            }
        }
    }

    [Fact]
    public void Api_ShouldNotReference_WorkerHostedServices()
    {
        var apiRefs = GetProjectReferences(R(@"src\ClimateHub.Api\ClimateHub.Api.csproj"));
        foreach (var r in apiRefs)
        {
            if (r.Name.Contains("Worker") || r.Name.Contains("BackgroundService"))
            {
                Assert.Fail($"API should not reference worker services directly: {r.Name}");
            }
        }
    }

    [Fact]
    public void DomainProjects_DoNotReference_Infrastructure()
    {
        var domainProjectFiles = Directory.GetFiles(
            Path.Combine(SolutionDir, "src"), "*.Domain.csproj", SearchOption.AllDirectories);

        foreach (var file in domainProjectFiles)
        {
            var content = File.ReadAllText(file);
            var refs = Regex.Matches(content, @"ProjectReference.*Include=""([^""]+)""");
            foreach (Match match in refs)
            {
                var refPath = match.Groups[1].Value;
                if (refPath.Contains("Infrastructure"))
                {
                    Assert.Fail($"Domain project {Path.GetFileNameWithoutExtension(file)} references Infrastructure: {refPath}");
                }
            }
        }
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

    private static string ExtractModuleName(string projectName)
    {
        var match = Regex.Match(projectName, @"ClimateHub\.Modules\.(\w+)\.");
        return match.Success ? match.Groups[1].Value : projectName;
    }

    private List<ProjectReference> GetProjectReferences(string csprojPath)
    {
        var content = File.ReadAllText(csprojPath);
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