using System.Text;
using ClimateHub.Modules.Commands.Domain.Repositories;
using MQTTnet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClimateHub.SharedKernel.Configuration;

namespace ClimateHub.Modules.Commands.Infrastructure;

public class CommandOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandOutboxWorker> _logger;
    private IMqttClient? _mqttClient;
    private string _mqttHost = "localhost";
    private int _mqttPort = 1883;

    public CommandOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<CommandOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CommandOutboxWorker started");

        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();
        var opts = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId("climate-hub-command-outbox")
            .WithCleanSession().Build();

        await ConnectWithRetryAsync(opts, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outboxRepo = scope.ServiceProvider.GetRequiredService<ICommandOutboxRepository>();

                if (!_mqttClient!.IsConnected)
                    await ConnectWithRetryAsync(opts, stoppingToken);

                await outboxRepo.RecoverStuckAsync(TimeSpan.FromMinutes(5), stoppingToken);
                var pending = await outboxRepo.GetPendingAsync(20, stoppingToken);

                foreach (var item in pending)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    try
                    {
                        var acquired = await outboxRepo.TryAcquireAsync(item.Id, stoppingToken);
                        if (!acquired) continue;

                        var msg = new MqttApplicationMessageBuilder()
                            .WithTopic(item.Topic)
                            .WithPayload(Encoding.UTF8.GetBytes(item.Payload))
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                            .Build();

                        var result = await _mqttClient.PublishAsync(msg, stoppingToken);
                        if (result.IsSuccess)
                            await outboxRepo.MarkPublishedAsync(item.Id, stoppingToken);
                        else
                            await outboxRepo.MarkRetryableAsync(item.Id, "PUBLISH_FAILED", TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Command outbox publish failed for item {Id}", item.Id);
                        await outboxRepo.MarkRetryableAsync(item.Id, ex.Message, TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "CommandOutboxWorker error"); }

            await Task.Delay(2000, stoppingToken);
        }
    }

    private async Task ConnectWithRetryAsync(MqttClientOptions opts, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { var r = await _mqttClient!.ConnectAsync(opts, ct); if (r.ResultCode == MqttClientConnectResultCode.Success) return; }
            catch { _logger.LogWarning("MQTT connect failed, retrying…"); }
            await Task.Delay(3000, ct);
        }
    }
}