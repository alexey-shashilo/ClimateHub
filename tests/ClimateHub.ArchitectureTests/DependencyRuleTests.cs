using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace ClimateHub.ArchitectureTests;

public class DependencyRuleTests
{
    private static readonly Assembly[] DomainAssemblies;
    private static readonly Assembly[] InfraAssemblies;

    static DependencyRuleTests()
    {
        var solutionDir = FindSlnDir();
        DomainAssemblies = LoadFromDir(Path.Combine(solutionDir, "src"), "*.Domain.dll");
        InfraAssemblies = LoadFromDir(Path.Combine(solutionDir, "src"), "*.Infrastructure.dll");
    }

    private static Assembly[] LoadFromDir(string root, string pattern)
    {
        var files = Directory.GetFiles(root, pattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj"))
            .ToArray();
        var result = new List<Assembly>();
        var errors = new List<string>();
        foreach (var file in files)
        {
            try
            {
                var asm = Assembly.LoadFrom(file);
                if (asm.GetName().Name?.StartsWith("ClimateHub.Modules") == true)
                    result.Add(asm);
            }
            catch (Exception ex)
            {
                errors.Add($"{file}: {ex.Message}");
            }
        }
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Failed to load {errors.Count} assemblies:\n{string.Join("\n", errors.Take(5))}");
        return result.ToArray();
    }

    private static string FindSlnDir()
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
    public void NeedDomain_ShouldNotReference_EngineeringInfrastructure()
    {
        var needDomain = GetAssembly("ClimateHub.Modules.Needs.Domain");
        var refs = needDomain.GetReferencedAssemblies();
        Assert.DoesNotContain(refs, r => r.Name!.Contains("EngineeringSystems.Infrastructure"));
    }

    [Fact]
    public void NeedInfrastructure_ShouldNotReference_EngineeringApplication()
    {
        var needInfra = GetAssembly("ClimateHub.Modules.Needs.Infrastructure");
        var refs = needInfra.GetReferencedAssemblies();
        Assert.DoesNotContain(refs, r => r.Name!.Contains("EngineeringSystems.Application"));
    }

    [Fact]
    public void ClimateDomain_ShouldNotReference_DevicesInfrastructure()
    {
        var climateDomain = GetAssembly("ClimateHub.Modules.Climate.Domain");
        var refs = climateDomain.GetReferencedAssemblies();
        Assert.DoesNotContain(refs, r => r.Name!.Contains("Devices.Infrastructure"));
    }

    [Fact]
    public void EngineeringDomain_ShouldNotReference_EnvironmentInfrastructure()
    {
        var engDomain = GetAssembly("ClimateHub.Modules.EngineeringSystems.Domain");
        var refs = engDomain.GetReferencedAssemblies();
        Assert.DoesNotContain(refs, r => r.Name!.Contains("Environment.Infrastructure"));
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        foreach (var asm in DomainAssemblies)
        {
            var refs = asm.GetReferencedAssemblies();
            Assert.DoesNotContain(refs, r => r.Name!.Contains("Infrastructure") && !r.Name!.Contains("Domain"));
        }
    }

    [Fact]
    public void Infrastructure_DoesNotReference_ApplicationFromOtherModules()
    {
        var issues = new List<string>();
        foreach (var infra in InfraAssemblies)
        {
            var infraName = infra.GetName().Name!;
            var moduleName = ExtractModuleName(infraName);

            foreach (var refName in infra.GetReferencedAssemblies().Select(r => r.Name!))
            {
                if (refName.Contains(".Application") && !refName.Contains(moduleName) &&
                    refName.StartsWith("ClimateHub.Modules"))
                {
                    issues.Add($"{infraName} references {refName}");
                }
            }
        }
        Assert.Empty(issues);
    }

    [Fact]
    public void NoGuidEmptyBuildingId()
    {
        var srcDir = FindSourceDir();
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin") && !f.Contains("Migrations") && !f.Contains("node_modules"));

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file, System.Text.Encoding.UTF8);
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
        var srcDir = FindSourceDir();
        var codesFiles = Directory.GetFiles(srcDir, "*CapabilityCodes*", SearchOption.AllDirectories);

        foreach (var file in codesFiles)
        {
            var content = File.ReadAllText(file, System.Text.Encoding.UTF8);
            var matches = Regex.Matches(content, @"const string (\w+)\s*=\s*""([^""]+)""");
            var codes = new HashSet<string>();
            foreach (Match match in matches)
            {
                var code = match.Groups[2].Value;
                if (!codes.Add(code))
                    Assert.Fail($"File {file}: Duplicate capability code '{code}' in {match.Groups[1].Value}");
            }
        }
    }

    [Fact]
    public void Api_DoesNotReference_WorkerTypes()
    {
        var apiDll = Path.Combine(FindSlnDir(), "src", "ClimateHub.Api", "bin", "Debug", "net9.0", "ClimateHub.Api.dll");
        if (!File.Exists(apiDll))
            return;
        var api = Assembly.LoadFrom(apiDll);
        var types = api.GetTypes();
        var workerImpls = types.Where(t =>
            t.IsAssignableTo(typeof(Microsoft.Extensions.Hosting.IHostedService)) &&
            t.Namespace?.Contains("Worker") == true);
        Assert.Empty(workerImpls);
    }

    private static Assembly GetAssembly(string name)
    {
        var sln = FindSlnDir();
        // First check if assembly is already loaded
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == name);
        if (loaded != null) return loaded;

        var files = Directory.GetFiles(Path.Combine(sln, "src"), $"{name}.dll", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj"))
            .ToArray();
        if (files.Length == 0)
            throw new FileNotFoundException($"Assembly {name}.dll not found under src/");
        return Assembly.LoadFrom(files[0]);
    }

    private static string FindSourceDir()
    {
        return Path.Combine(FindSlnDir(), "src");
    }

    private static string ExtractModuleName(string assemblyName)
    {
        // ClimateHub.Modules.Commands.Infrastructure -> "Commands"
        var parts = assemblyName.Split('.');
        if (parts.Length >= 3 && parts[0] == "ClimateHub" && parts[1] == "Modules")
            return parts[2];
        return assemblyName;
    }
}
