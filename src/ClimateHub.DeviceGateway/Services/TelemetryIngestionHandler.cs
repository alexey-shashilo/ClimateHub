using System.Diagnostics;
using System.Text.Json;
using ClimateHub.DeviceGateway.Contracts;
using ClimateHub.Infrastructure.Events;
using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Building.Contracts;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.Modules.Environment.Domain;
using ClimateHub.Modules.Environment.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure.Models;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.Modules.Environment.Infrastructure.Services;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimateHub.DeviceGateway.Services;

public class TelemetryIngestionHandler
{
    private readonly EnvironmentDbContext _dbContext;
    private readonly IMessageInboxRepository _inboxRepository;
    private readonly IRoomEnvironmentStateRepository _stateRepository;
    private readonly ITelemetryOutboxRepository _outboxRepository;
    private readonly IInfluxDbWriter _influxDbWriter;
    private readonly EnvironmentEventBus _eventBus;
    private readonly IBuildingModule _buildingModule;
    private readonly IDevicesModule _devicesModule;
    private readonly ILogger<TelemetryIngestionHandler> _logger;

    public TelemetryIngestionHandler(
        EnvironmentDbContext dbContext,
        IMessageInboxRepository inboxRepository,
        IRoomEnvironmentStateRepository stateRepository,
        ITelemetryOutboxRepository outboxRepository,
        IInfluxDbWriter influxDbWriter,
        IBuildingModule buildingModule,
        IDevicesModule devicesModule,
        EnvironmentEventBus eventBus,
        ILogger<TelemetryIngestionHandler> logger)
    {
        _dbContext = dbContext;
        _inboxRepository = inboxRepository;
        _stateRepository = stateRepository;
        _outboxRepository = outboxRepository;
        _influxDbWriter = influxDbWriter;
        _buildingModule = buildingModule;
        _devicesModule = devicesModule;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(
        string topic,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        using var activity = Diagnostics.ActivitySource.StartActivity(Diagnostics.Operations.EnvironmentTelemetryIngested);

        // 1. Parse topic
        if (!TelemetryTopicParser.TryParseTopic(topic, out var buildingIdStr, out var deviceIdStr))
        {
            _logger.LogWarning("Invalid topic format: {Topic}", topic);
            return;
        }

        // 2. Deserialize envelope
        EnvironmentTelemetryEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EnvironmentTelemetryEnvelope>(payloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize telemetry from topic {Topic}", topic);
            return;
        }

        if (envelope is null)
        {
            _logger.LogWarning("Null envelope from topic {Topic}", topic);
            return;
        }

        activity?.SetTag(Diagnostics.Tags.MessageId, envelope.MessageId);
        activity?.SetTag(Diagnostics.Tags.DeviceId, envelope.DeviceId);
        activity?.SetTag(Diagnostics.Tags.BuildingId, envelope.BuildingId);

        // 3. Validate envelope
        var envValidation = TelemetryValidator.Validate(envelope);
        if (!envValidation.Success)
        {
            _logger.LogWarning(
                "Telemetry rejected: {ErrorCode} {ErrorMessage} msg={MessageId}",
                envValidation.ErrorCode, envValidation.ErrorMessage, envelope.MessageId);
            return;
        }

        // 4. Parse IDs
        if (!Guid.TryParse(envelope.BuildingId, out var buildingGuid) ||
            !Guid.TryParse(envelope.DeviceId, out var deviceGuid))
        {
            _logger.LogWarning("Invalid UUID in topic {Topic}", topic);
            return;
        }

        var buildingId = BuildingId.From(buildingGuid);
        var deviceId = DeviceId.From(deviceGuid);

        // 5. Deduplication
        var isDuplicate = await _inboxRepository.IsDuplicateAsync("mqtt", envelope.MessageId, cancellationToken);
        if (isDuplicate)
        {
            _logger.LogInformation("Duplicate message skipped: {MessageId}", envelope.MessageId);
            return;
        }

        // 6. Verify device exists
        var deviceInfo = await _devicesModule.GetDeviceAsync(deviceId, cancellationToken);
        if (deviceInfo is null)
        {
            _logger.LogWarning("Unknown device {DeviceId} from topic {Topic}", deviceId, topic);
            return;
        }

        // 7. Verify building exists
        var buildingExists = await _buildingModule.BuildingExistsAsync(buildingId, cancellationToken);
        if (!buildingExists)
        {
            _logger.LogWarning("Unknown building {BuildingId} from topic {Topic}", buildingId, topic);
            return;
        }

        // 8. Get device room assignment
        var roomId = await _devicesModule.GetDeviceRoomAssignmentAsync(deviceId, cancellationToken);
        if (roomId is null)
        {
            _logger.LogWarning("Device {DeviceId} not assigned to any room", deviceId);
            return;
        }

        // 9. Parse measurement values
        var measuredAt = DateTimeOffset.TryParse(envelope.MeasuredAt, out var parsedMeasuredAt)
            ? parsedMeasuredAt
            : DateTimeOffset.UtcNow;

        EnvironmentTelemetryPayload payload;
        try
        {
            payload = envelope.Payload is not null
                ? JsonSerializer.Deserialize<EnvironmentTelemetryPayload>(envelope.Payload.Value.GetRawText()) ?? new EnvironmentTelemetryPayload()
                : new EnvironmentTelemetryPayload();
        }
        catch (JsonException)
        {
            _logger.LogWarning("Failed to parse payload for msg {MessageId}", envelope.MessageId);
            return;
        }

        // 10. Validate payload
        var payloadValidation = TelemetryValidator.ValidatePayload(payload);
        if (!payloadValidation.Success)
        {
            _logger.LogWarning(
                "Payload rejected: {ErrorCode} {ErrorMessage} msg={MessageId}",
                payloadValidation.ErrorCode, payloadValidation.ErrorMessage, envelope.MessageId);
            return;
        }

        // 11. Verify device capabilities match payload
        if (payload.TemperatureC is not null)
        {
            var hasCap = await _devicesModule.DeviceHasCapabilityAsync(deviceId, "measure.temperature", cancellationToken);
            if (!hasCap) { _logger.LogWarning("Device {DeviceId} lacks measure.temperature capability", deviceId); return; }
        }
        if (payload.RelativeHumidityPct is not null)
        {
            var hasCap = await _devicesModule.DeviceHasCapabilityAsync(deviceId, "measure.relative-humidity", cancellationToken);
            if (!hasCap) { _logger.LogWarning("Device {DeviceId} lacks measure.relative-humidity capability", deviceId); return; }
        }
        if (payload.Co2Ppm is not null)
        {
            var hasCap = await _devicesModule.DeviceHasCapabilityAsync(deviceId, "measure.co2", cancellationToken);
            if (!hasCap) { _logger.LogWarning("Device {DeviceId} lacks measure.co2 capability", deviceId); return; }
        }

        // 12. Determine quality
        var quality = Models.QualityClassifier.Classify(payload);

        // Track changed parameters
        var changedParameters = new List<string>();

        // 13. Atomic transaction: all persistence operations or none
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (payload.TemperatureC.HasValue)
            {
                await _stateRepository.UpsertParameterAsync(new RoomParameterEntity
                {
                    RoomId = roomId.Value,
                    Parameter = "temperature",
                    Value = payload.TemperatureC,
                    Unit = "celsius",
                    MeasuredAt = measuredAt,
                    ReceivedAt = now,
                    Quality = quality,
                    SourceDeviceId = deviceId
                }, cancellationToken);
                changedParameters.Add("temperature");
            }

            if (payload.RelativeHumidityPct.HasValue)
            {
                await _stateRepository.UpsertParameterAsync(new RoomParameterEntity
                {
                    RoomId = roomId.Value,
                    Parameter = "humidity",
                    Value = payload.RelativeHumidityPct,
                    Unit = "percent",
                    MeasuredAt = measuredAt,
                    ReceivedAt = now,
                    Quality = quality,
                    SourceDeviceId = deviceId
                }, cancellationToken);
                changedParameters.Add("humidity");
            }

            if (payload.Co2Ppm.HasValue)
            {
                await _stateRepository.UpsertParameterAsync(new RoomParameterEntity
                {
                    RoomId = roomId.Value,
                    Parameter = "co2",
                    Value = payload.Co2Ppm,
                    Unit = "ppm",
                    MeasuredAt = measuredAt,
                    ReceivedAt = now,
                    Quality = quality,
                    SourceDeviceId = deviceId
                }, cancellationToken);
                changedParameters.Add("co2");
            }

            await _outboxRepository.CreateAsync(new TelemetryOutboxEntity
            {
                RoomId = roomId.Value,
                DeviceId = deviceId,
                MeasuredAt = measuredAt,
                TemperatureC = payload.TemperatureC,
                RelativeHumidityPct = payload.RelativeHumidityPct,
                Co2Ppm = payload.Co2Ppm,
                Quality = quality,
                CreatedAt = now,
                Status = "Pending"
            }, cancellationToken);

            await _inboxRepository.MarkProcessedAsync("mqtt", envelope.MessageId, deviceId.ToString(),
                envelope.BootId, envelope.SequenceNumber, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // 16. Publish environment updated event (after all persistence)
        _eventBus.Publish(new EnvironmentUpdatedEvent
        {
            RoomId = roomId.Value,
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "environment.updated",
            PayloadJson = JsonSerializer.Serialize(new
            {
                changedParameters,
                measuredAt,
                quality
            })
        });

        // 17. Publish room environment state changed for Need Engine evaluation
        _eventBus.Publish(new EnvironmentUpdatedEvent
        {
            RoomId = roomId.Value,
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "environment.state.changed",
            PayloadJson = JsonSerializer.Serialize(new
            {
                buildingId = buildingId.ToString(),
                roomId = roomId.ToString(),
                changedParameters,
                measuredAt,
                receivedAt = now,
                sourceDeviceId = deviceId.ToString()
            })
        });

        _logger.LogInformation(
            "Processed telemetry: msg={MessageId} device={DeviceId} room={RoomId} t={Temp} h={Hum} co2={Co2} params=[{Params}]",
            envelope.MessageId, deviceId, roomId, payload.TemperatureC, payload.RelativeHumidityPct, payload.Co2Ppm,
            string.Join(",", changedParameters));
    }
}
