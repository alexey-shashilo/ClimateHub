using System.Text.Json;

namespace ClimateHub.DeploymentVerificationTests;

[Trait("Category", "Deployment")]
public class DeploymentConfigTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void DockerComposeFile_Exists()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "deploy", "docker-compose.yml")));
    }

    [Fact]
    public void ProductionComposeFile_Exists()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "deploy", "docker-compose.production.yml")));
    }

    [Fact]
    public async Task DockerCompose_AllServices_HaveBuildContextsOrImages()
    {
        var compose = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "deploy", "docker-compose.yml"));
        Assert.Contains("image:", compose);
        Assert.Contains("build:", compose);
    }

    [Fact]
    public void ApiDockerfile_Exists()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "deploy", "docker", "Dockerfile.api")));
    }

    [Fact]
    public void GatewayDockerfile_Exists()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "deploy", "docker", "Dockerfile.gateway")));
    }

    [Fact]
    public void ProductionCompose_RequiredVars_Documented()
    {
        var compose = File.ReadAllText(Path.Combine(RepoRoot, "deploy", "docker-compose.production.yml"));
        // Verify required env vars exist
        Assert.Contains("POSTGRES_PASSWORD", compose);
        Assert.Contains("INFLUXDB_PASSWORD", compose);
        Assert.Contains("INFLUXDB_TOKEN", compose);
        Assert.Contains("JWT_SIGNING_KEY", compose);
    }

    [Fact]
    public void DevCompose_ContainsRequiredServices()
    {
        var compose = File.ReadAllText(Path.Combine(RepoRoot, "deploy", "docker-compose.yml"));
        Assert.Contains("postgres:", compose);
        Assert.Contains("mosquitto:", compose);
        Assert.Contains("influxdb:", compose);
        Assert.Contains("api:", compose);
        Assert.Contains("device-gateway:", compose);
        Assert.Contains("prometheus:", compose);
        Assert.Contains("grafana:", compose);
    }

    [Fact]
    public void ProductionCompose_ContainsAllServices()
    {
        var compose = File.ReadAllText(Path.Combine(RepoRoot, "deploy", "docker-compose.production.yml"));
        Assert.Contains("postgres:", compose);
        Assert.Contains("mosquitto:", compose);
        Assert.Contains("influxdb:", compose);
        Assert.Contains("api:", compose);
        Assert.Contains("device-gateway:", compose);
        Assert.Contains("migrator:", compose);
        Assert.Contains("prometheus:", compose);
        Assert.Contains("grafana:", compose);
        Assert.Contains("caddy:", compose);
    }

    [Fact]
    public void PrometheusConfig_Exists()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "deploy", "monitoring", "prometheus.yml")));
    }

    [Fact]
    public void GrafanaProvisioning_Exists()
    {
        Assert.True(Directory.Exists(Path.Combine(RepoRoot, "deploy", "monitoring", "grafana")));
    }

    [Fact]
    public void MosquittoConfigs_Exist()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot, "deploy", "mqtt", "mosquitto.dev.conf")));
        Assert.True(File.Exists(Path.Combine(RepoRoot, "deploy", "mqtt", "mosquitto.prod.conf")));
    }

    private static string FindRepoRoot()
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
        var cwd = Directory.GetCurrentDirectory();
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(cwd, "ClimateHub.sln")))
                return cwd;
            var parent = Directory.GetParent(cwd);
            if (parent is null || parent.FullName == cwd) break;
            cwd = parent.FullName;
        }
        throw new DirectoryNotFoundException($"Could not find solution dir. BaseDir={AppContext.BaseDirectory}, CWD={Directory.GetCurrentDirectory()}");
    }
}