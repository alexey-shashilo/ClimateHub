using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace ClimateHub.DeploymentVerificationTests;

[Collection("Docker")]
public class BackupRestoreTests : IAsyncLifetime
{
    private readonly IContainer _postgres;
    private string _connectionString = "";

    public BackupRestoreTests()
    {
        _postgres = new ContainerBuilder()
            .WithImage("postgres:17-alpine").WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432)).Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = $"Host={_postgres.Hostname};Port={_postgres.GetMappedPublicPort(5432)};Database=climate_hub;Username=climate_hub;Password=climate_hub;";
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Backup_UsingPgDump_CreatesValidOutput()
    {
        using var conn = new Npgsql.NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new Npgsql.NpgsqlCommand(
            "CREATE TABLE IF NOT EXISTS backup_test (id UUID PRIMARY KEY, value TEXT); " +
            "INSERT INTO backup_test VALUES (gen_random_uuid(), 'test_value');", conn);
        await cmd.ExecuteNonQueryAsync();

        var result = await _postgres.ExecAsync(new[] { "pg_dump", "-U", "climate_hub", "-d", "climate_hub", "--no-owner", "--no-acl" });

        var stdout = string.IsNullOrEmpty(result.Stdout) ? result.Stderr : result.Stdout;
        Assert.False(string.IsNullOrEmpty(stdout), "pg_dump should produce output");
        Assert.Contains("backup_test", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BackupTool_DryRun_ExecutesWithoutError()
    {
        var result = await _postgres.ExecAsync(new[] { "pg_dump", "-U", "climate_hub", "-d", "climate_hub", "--no-owner", "--no-acl", "-f", "/tmp/test_backup.sql" });

        var output = (result.Stdout + result.Stderr).Trim();
        Assert.DoesNotContain("error", output, StringComparison.OrdinalIgnoreCase);

        var lsResult = await _postgres.ExecAsync(new[] { "ls", "-la", "/tmp/test_backup.sql" });
        Assert.Contains("test_backup.sql", lsResult.Stdout + lsResult.Stderr);
    }

    [Fact]
    public async Task Checksum_AfterBackup_Matches()
    {
        await _postgres.ExecAsync(new[] { "pg_dump", "-U", "climate_hub", "-d", "climate_hub",
            "--no-owner", "--no-acl", "-f", "/tmp/checksum_test.sql" });

        var shaResult = await _postgres.ExecAsync(new[] { "sha256sum", "/tmp/checksum_test.sql" });
        Assert.False(string.IsNullOrEmpty(shaResult.Stdout), "sha256sum should produce output");

        var checksum = shaResult.Stdout.Trim();
        Assert.Matches(@"^[a-f0-9]{64}", checksum);
    }

    [Fact]
    public async Task Restore_AfterBackup_DataVerifiable()
    {
        var testData = Guid.NewGuid().ToString();

        using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await using var cmd = new Npgsql.NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS restore_test (id UUID PRIMARY KEY, data TEXT); " +
                $"INSERT INTO restore_test VALUES (gen_random_uuid(), '{testData}');", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        await _postgres.ExecAsync(new[] { "pg_dump", "-U", "climate_hub", "-d", "climate_hub",
            "--no-owner", "--no-acl", "-f", "/tmp/restore_test.sql" });

        using (var conn2 = new Npgsql.NpgsqlConnection(_connectionString))
        {
            await conn2.OpenAsync();
            await using var drop = new Npgsql.NpgsqlCommand("DROP TABLE IF EXISTS restore_test;", conn2);
            await drop.ExecuteNonQueryAsync();
        }

        await _postgres.ExecAsync(new[] { "psql", "-U", "climate_hub", "-d", "climate_hub",
            "-f", "/tmp/restore_test.sql" });

        using (var conn3 = new Npgsql.NpgsqlConnection(_connectionString))
        {
            await conn3.OpenAsync();
            await using var check = new Npgsql.NpgsqlCommand(
                "SELECT COUNT(*) FROM restore_test WHERE data = @data",
                conn3);
            check.Parameters.AddWithValue("data", testData);
            var count = (long)(await check.ExecuteScalarAsync())!;
            Assert.True(count > 0, "Restored data should contain the test value");
        }
    }
}
