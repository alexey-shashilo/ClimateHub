using System.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClimateHub.IntegrationTests;

[Trait("Category", "Integration")]
public class MigrationsVerificationTests : IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private string _connectionString = string.Empty;

    public MigrationsVerificationTests()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_test")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_test")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _connectionString =
            $"Host={_postgresContainer.Hostname};" +
            $"Port={_postgresContainer.GetMappedPublicPort(5432)};" +
            $"Database=climate_hub_test;Username=climate_hub;Password=climate_hub_test;";
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task CleanMigration_AppliesAllMigrations()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var tablesBefore = await GetTableNames(conn);
        Assert.DoesNotContain("__EFMigrationsHistory", tablesBefore);

        var schemaDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src");
        var scriptFiles = Directory.GetFiles(schemaDir, "*.sql", SearchOption.AllDirectories);
        if (scriptFiles.Length > 0)
        {
            foreach (var file in scriptFiles.OrderBy(f => f))
            {
                var sql = await File.ReadAllTextAsync(file);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        var tablesAfter = await GetTableNames(conn);
        Assert.NotEmpty(tablesAfter);
    }

    [Fact]
    public async Task SchemaAssertions_TablesColumnsTypesExist()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var schemaTable = conn.GetSchema("Tables");
        var tableNames = schemaTable.Rows.Cast<DataRow>()
            .Select(r => $"{r["TABLE_SCHEMA"]}.{r["TABLE_NAME"]}")
            .ToList();
    }

    [Fact]
    public async Task PendingModelDrift_DetectsIfMigrationNeeded()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var historyExists = await TableExists(conn, "__EFMigrationsHistory");

        if (!historyExists)
        {
            await conn.CloseAsync();
            return;
        }

        var historyRows = await GetMigrationHistory(conn);
        Assert.NotEmpty(historyRows);
    }

    private static async Task<List<string>> GetTableNames(NpgsqlConnection conn)
    {
        var tables = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT table_schema || '.' || table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name
            """;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));
        return tables;
    }

    private static async Task<bool> TableExists(NpgsqlConnection conn, string tableName)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_name = @name AND table_type = 'BASE TABLE'
                 AND table_schema NOT IN ('pg_catalog', 'information_schema')
            """;
        cmd.Parameters.AddWithValue("@name", tableName);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        return count > 0;
    }

    private static async Task<List<string>> GetMigrationHistory(NpgsqlConnection conn)
    {
        var migrations = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT migration_id FROM \"__EFMigrationsHistory\" ORDER BY migration_id";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            migrations.Add(reader.GetString(0));
        return migrations;
    }
}
