using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClimateHub.EndToEndTests;

public class EndToEndFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private readonly IContainer _mqttContainer;
    private readonly IContainer _influxDbContainer;

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public int MqttPort { get; private set; }
    public string InfluxDbUrl { get; private set; } = string.Empty;
    public string InfluxDbToken { get; private set; } = "e2e-test-token";

    public EndToEndFixture()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_e2e")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_e2e")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        _mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0.20")
            .WithPortBinding(1883, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .Build();

        _influxDbContainer = new ContainerBuilder()
            .WithImage("influxdb:2.7.11-alpine")
            .WithPortBinding(8086, true)
            .WithEnvironment("DOCKER_INFLUXDB_INIT_MODE", "setup")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_USERNAME", "admin")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_PASSWORD", "admin123456")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_ORG", "climate-hub")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_BUCKET", "climate-hub")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_ADMIN_TOKEN", InfluxDbToken)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8086))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _mqttContainer.StartAsync();
        await Task.Delay(3000);
        await _influxDbContainer.StartAsync();

        PostgresConnectionString =
            $"Host={_postgresContainer.Hostname};" +
            $"Port={_postgresContainer.GetMappedPublicPort(5432)};" +
            $"Database=climate_hub_e2e;Username=climate_hub;Password=climate_hub_e2e;";

        MqttPort = _mqttContainer.GetMappedPublicPort(1883);
        InfluxDbUrl = $"http://{_influxDbContainer.Hostname}:{_influxDbContainer.GetMappedPublicPort(8086)}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = PostgresConnectionString,
                ["Mqtt:Host"] = _mqttContainer.Hostname ?? "localhost",
                ["Mqtt:Port"] = MqttPort.ToString(),
                ["Mqtt:Username"] = "test",
                ["Mqtt:Password"] = "test",
                ["InfluxDb:Url"] = InfluxDbUrl,
                ["InfluxDb:Token"] = InfluxDbToken,
                ["InfluxDb:Organization"] = "climate-hub",
                ["InfluxDb:Bucket"] = "climate-hub",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _influxDbContainer.DisposeAsync();
        await _mqttContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await ((IAsyncLifetime)this).DisposeAsync();
    }
}