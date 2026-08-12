using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.EndToEndTests;

public class EndToEndFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private readonly IContainer _mqttContainer;
    private readonly IContainer _influxDbContainer;

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public int MqttPort { get; private set; }
    public string MqttHost { get; private set; } = "localhost";
    public string InfluxDbUrl { get; private set; } = string.Empty;
    public string InfluxDbToken { get; private set; } = "e2e-test-token";
    public IMqttClient AdminMqttClient { get; private set; } = null!;
    public string AuthToken { get; private set; } = string.Empty;

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

        var mosquittoConfig = new[]
        {
            "listener 1883",
            "protocol mqtt",
            "allow_anonymous true",
            "persistence false",
            "log_dest stdout",
            "log_type all",
            "connection_messages true"
        };

        _mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0.20")
            .WithPortBinding(1883, true)
            .WithResourceMapping(
                Encoding.UTF8.GetBytes(string.Join("\n", mosquittoConfig)),
                "/mosquitto/config/mosquitto.conf")
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
        await Task.Delay(2000);
        await _influxDbContainer.StartAsync();

        PostgresConnectionString =
            $"Host={_postgresContainer.Hostname};" +
            $"Port={_postgresContainer.GetMappedPublicPort(5432)};" +
            $"Database=climate_hub_e2e;Username=climate_hub;Password=climate_hub_e2e;";

        await ClimateHub.Migrator.MigrationRunner.ApplyAllAsync(PostgresConnectionString);

        MqttPort = _mqttContainer.GetMappedPublicPort(1883);
        MqttHost = _mqttContainer.Hostname ?? "localhost";
        InfluxDbUrl = $"http://{_influxDbContainer.Hostname}:{_influxDbContainer.GetMappedPublicPort(8086)}";

        AdminMqttClient = await ConnectAdminMqttClient();

        AuthToken = TestAuthHelper.GenerateToken();

        ApplyProcessEnvironment();
    }

    private void ApplyProcessEnvironment()
    {
        System.Environment.SetEnvironmentVariable("Postgres__ConnectionString", PostgresConnectionString);
        System.Environment.SetEnvironmentVariable("Mqtt__Host", MqttHost);
        System.Environment.SetEnvironmentVariable("Mqtt__Port", MqttPort.ToString());
        System.Environment.SetEnvironmentVariable("Mqtt__ClientId", "climate-hub-device-gateway");
        System.Environment.SetEnvironmentVariable("InfluxDb__Url", InfluxDbUrl);
        System.Environment.SetEnvironmentVariable("InfluxDb__Token", InfluxDbToken);
        System.Environment.SetEnvironmentVariable("InfluxDb__Organization", "climate-hub");
        System.Environment.SetEnvironmentVariable("InfluxDb__Bucket", "climate-hub");
        System.Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-signing-key-that-is-at-least-32-characters-long");
        System.Environment.SetEnvironmentVariable("Jwt__Issuer", "ClimateHub");
        System.Environment.SetEnvironmentVariable("Jwt__Audience", "ClimateHub.Api");
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            services.Configure<ClimateHub.SharedKernel.Configuration.MqttOptions>(o =>
            {
                o.Host = MqttHost;
                o.Port = MqttPort;
                o.ClientId = "climate-hub-device-gateway";
            });
            services.AddScoped<ClimateHub.DeviceGateway.Services.TelemetryIngestionHandler>();
            services.AddScoped<ClimateHub.DeviceGateway.Services.CommandEventConsumer>();
            services.AddHostedService<ClimateHub.DeviceGateway.DeviceGatewayWorker>();
        });
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = PostgresConnectionString,
                ["Mqtt:Host"] = MqttHost,
                ["Mqtt:Port"] = MqttPort.ToString(),
                ["Mqtt:ClientId"] = "climate-hub-device-gateway",
                ["InfluxDb:Url"] = InfluxDbUrl,
                ["InfluxDb:Token"] = InfluxDbToken,
                ["InfluxDb:Organization"] = "climate-hub",
                ["InfluxDb:Bucket"] = "climate-hub",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                ["Jwt:Issuer"] = "ClimateHub",
                ["Jwt:Audience"] = "ClimateHub.Api",
            });
        });
    }

    private async Task<IMqttClient> ConnectAdminMqttClient()
    {
        var factory = new MqttClientFactory();
        var client = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(MqttHost, MqttPort)
            .WithClientId($"e2e-admin-{Guid.NewGuid():N}"[..20])
            .WithCleanSession()
            .Build();
        await client.ConnectAsync(options);
        return client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (AdminMqttClient?.IsConnected == true)
            await AdminMqttClient.DisconnectAsync();
        AdminMqttClient?.Dispose();
        await _influxDbContainer.DisposeAsync();
        await _mqttContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await ((IAsyncLifetime)this).DisposeAsync();
    }
}
