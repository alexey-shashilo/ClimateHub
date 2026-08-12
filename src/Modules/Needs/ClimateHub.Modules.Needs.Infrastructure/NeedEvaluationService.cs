using System.Text.Json;
using ClimateHub.Infrastructure.Events;
using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Modules.Climate.Contracts;
using ClimateHub.Modules.Commands.Contracts;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Needs.Contracts;
using ClimateHub.Modules.Needs.Domain;
using ClimateHub.Modules.Needs.Domain.Repositories;
using NeedEvaluationTrigger = ClimateHub.Modules.Needs.Domain.NeedEvaluationTrigger;
using NeedStatus = ClimateHub.Modules.Needs.Domain.NeedStatus;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using CmdId = ClimateHub.Modules.Needs.Domain.CommandId;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class NeedEvaluationService
{
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly IRoomPolicyReader _policyReader;
    private readonly INeedRepository _needRepo;
    private readonly IRoomParameterEvaluationStateRepository _evalStateRepo;
    private readonly IDevicesModule _devicesModule;
    private readonly ICommandsModule _commandsModule;
    private readonly ICapabilityCatalog _capabilityCatalog;
    private readonly IClimateModule _climateModule;
    private readonly EnvironmentEventBus _eventBus;
    private readonly InternalEventOutboxRepository _internalEventOutbox;
    private readonly RoomEvaluationLock _roomLock;
    private readonly AntiOscillationOptions _antiOscillation;
    private readonly NeedDeviceResolver _deviceResolver;
    private readonly IRoomBuildingResolver _roomBuildingResolver;
    private readonly ILogger<NeedEvaluationService> _logger;

    public NeedEvaluationService(
        IRoomEnvironmentStateReader envReader,
        IRoomPolicyReader policyReader,
        INeedRepository needRepo,
        IRoomParameterEvaluationStateRepository evalStateRepo,
        IDevicesModule devicesModule,
        ICommandsModule commandsModule,
        ICapabilityCatalog capabilityCatalog,
        IClimateModule climateModule,
        EnvironmentEventBus eventBus,
        InternalEventOutboxRepository internalEventOutbox,
        RoomEvaluationLock roomLock,
        AntiOscillationOptions antiOscillation,
        NeedDeviceResolver deviceResolver,
        IRoomBuildingResolver roomBuildingResolver,
        ILogger<NeedEvaluationService> logger)
    {
        _envReader = envReader; _policyReader = policyReader; _needRepo = needRepo;
        _evalStateRepo = evalStateRepo; _devicesModule = devicesModule;
        _commandsModule = commandsModule; _capabilityCatalog = capabilityCatalog;
        _climateModule = climateModule;
        _eventBus = eventBus;
        _internalEventOutbox = internalEventOutbox; _roomLock = roomLock;
        _antiOscillation = antiOscillation; _deviceResolver = deviceResolver;
        _roomBuildingResolver = roomBuildingResolver;
        _logger = logger;
    }

    public async Task EvaluateRoomAsync(RoomId roomId,
        NeedEvaluationTrigger trigger = NeedEvaluationTrigger.PeriodicReconciliation,
        string? correlationId = null, string? causationId = null,
        CancellationToken ct = default)
    {
        var buildingId = await _roomBuildingResolver.ResolveBuildingIdAsync(roomId, ct);
        if (buildingId is null)
        {
            _logger.LogWarning("Room {RoomId} has no associated building, skipping evaluation", roomId);
            return;
        }

        var acquired = await _roomLock.TryAcquireAsync(roomId, ct);
        if (!acquired)
        {
            _logger.LogDebug("Room {RoomId} evaluation lock not acquired, deferring", roomId);
            return;
        }

        var parameters = await _envReader.GetParametersAsync(roomId, ct);
        var policy = await _policyReader.GetByRoomAsync(roomId, ct);
        var now = DateTimeOffset.UtcNow;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RoomId"] = roomId.ToString(),
            ["EvaluationTrigger"] = trigger.ToString(),
            ["CorrelationId"] = correlationId ?? "",
            ["CausationId"] = causationId ?? ""
        }))
        {
            foreach (var param in parameters)
            {
                if (param.Value is null) continue;
                var current = param.Value.Value;

                if (param.Quality is "sensorError" or "rejected" or "unknown")
                    continue;

                var opts = _antiOscillation.GetForParameter(param.Parameter);
                var (needType, severity, deviation, min, max, pref) = ComputeFromContract(param.Parameter, current, policy);

                if (needType is null)
                {
                    await HandleParameterInRangeAsync(roomId, buildingId.Value.Value, param.Parameter, current, now, opts, trigger, correlationId, causationId, ct);
                    continue;
                }

                await HandleParameterOutOfRangeAsync(roomId, buildingId.Value.Value, param.Parameter, current, needType.Value, severity, deviation, min, max, pref, policy, now, opts, trigger, correlationId, causationId, ct);
            }
        }
    }

    private async Task HandleParameterInRangeAsync(RoomId roomId, Guid buildingId, string parameter, double current, DateTimeOffset now,
        ParameterOptions opts, NeedEvaluationTrigger trigger, string? correlationId, string? causationId,
        CancellationToken ct)
    {
        var evalState = await _evalStateRepo.GetByRoomAndParameterAsync(roomId, parameter, ct);
        if (evalState is not null)
        {
            var targetMax = MapParameterToDefaultMax(parameter);
            if (current <= targetMax - opts.ResolutionHysteresis)
            {
                evalState.ClearViolation(now);
            }
            evalState.UpdateEvaluation(current, "Valid", now, now);
            await _evalStateRepo.UpdateAsync(evalState, ct);
        }

        var existingNeed = await _needRepo.GetActiveByTypeAsync(roomId, MapParameterToNeedType(parameter), ct);
        if (existingNeed is null) return;

        if (existingNeed.Status == NeedStatus.WaitingForEffect)
        {
            existingNeed.SetStableSince();
            var duration = now - existingNeed.StableSince!.Value;
            if (duration >= opts.MinimumSatisfactionDuration)
            {
                existingNeed.Satisfy();
                await _needRepo.UpdateAsync(existingNeed, ct);
                await SaveEvaluationAsync(existingNeed, trigger, "WaitingForEffect", "Satisfied", correlationId, causationId, ct);
                var eventId = await PublishNeedEventAsync("need.satisfied", existingNeed, correlationId, causationId, ct);
                _eventBus.Publish(new EnvironmentUpdatedEvent
                {
                    RoomId = roomId,
                    Timestamp = now,
                    EventType = "need.satisfied",
                    CorrelationId = correlationId,
                    CausationId = causationId,
                    PayloadJson = JsonSerializer.Serialize(new { eventId = eventId.ToString() })
                });
                return;
            }
            existingNeed.UpdateEvaluation(current, 0, NeedSeverity.Low);
            await _needRepo.UpdateAsync(existingNeed, ct);
            _eventBus.Publish(new EnvironmentUpdatedEvent
            {
                RoomId = roomId,
                Timestamp = now,
                EventType = "need.updated",
                CorrelationId = correlationId,
                CausationId = causationId
            });
            return;
        }

        if (existingNeed.Status is NeedStatus.Detected or NeedStatus.Planning or NeedStatus.Planned or NeedStatus.Executing)
        {
            existingNeed.UpdateEvaluation(current, 0, NeedSeverity.Low);
            await _needRepo.UpdateAsync(existingNeed, ct);
            _eventBus.Publish(new EnvironmentUpdatedEvent
            {
                RoomId = roomId,
                Timestamp = now,
                EventType = "need.updated",
                CorrelationId = correlationId,
                CausationId = causationId
            });
        }
    }

    private async Task HandleParameterOutOfRangeAsync(RoomId roomId, Guid buildingId, string parameter, double current,
        NeedType needType, NeedSeverity severity, double deviation, double min, double max, double pref,
        RoomPolicyDto? policy, DateTimeOffset now, ParameterOptions opts,
        NeedEvaluationTrigger trigger, string? correlationId, string? causationId,
        CancellationToken ct)
    {
        // Deadband check
        if (deviation < opts.DetectionDeadband)
        {
            var existingDetected = await _needRepo.GetActiveByTypeAsync(roomId, needType, ct);
            if (existingDetected is not null)
            {
                existingDetected.UpdateEvaluation(current, deviation, severity);
                await _needRepo.UpdateAsync(existingDetected, ct);
            }
            return;
        }

        // Persisted violation state
        var evalState = await _evalStateRepo.GetByRoomAndParameterAsync(roomId, parameter, ct);
        if (evalState is null)
        {
            evalState = RoomParameterEvaluationState.Create(BuildingId.From(buildingId), roomId, parameter);
            evalState.RecordViolation(now);
            evalState.UpdateEvaluation(current, "Valid", now, now);
            await _evalStateRepo.AddAsync(evalState, ct);
        }
        else
        {
            evalState.RecordViolation(now);
            evalState.UpdateEvaluation(current, "Valid", now, now);
            await _evalStateRepo.UpdateAsync(evalState, ct);
        }

        var mode = FromPolicyMode(policy?.GetControlMode(parameter) ?? "monitorOnly");
        var activeNeed = await _needRepo.GetActiveByTypeAsync(roomId, needType, ct);

        // Check MinimumViolationDuration before creating need
        if (!evalState.IsViolationConfirmed(opts.MinimumViolationDuration, now))
        {
            if (activeNeed is not null)
            {
                activeNeed.UpdateEvaluation(current, deviation, severity);
                await _needRepo.UpdateAsync(activeNeed, ct);
            }
            return;
        }

        if (activeNeed is not null)
        {
            await UpdateExistingNeedAsync(activeNeed, current, deviation, severity, mode, policy, now, opts, trigger, correlationId, causationId, ct);
        }
        else
        {
            await CreateNewNeedAsync(roomId, BuildingId.From(buildingId), needType, severity, deviation, current, min, max, pref, parameter, mode, policy, now, opts, trigger, correlationId, causationId, ct);
        }
    }

    private async Task UpdateExistingNeedAsync(Need need, double current, double deviation, NeedSeverity severity,
        ControlMode mode, RoomPolicyDto? policy, DateTimeOffset now, ParameterOptions opts,
        NeedEvaluationTrigger trigger, string? correlationId, string? causationId,
        CancellationToken ct)
    {
        need.UpdateEvaluation(current, deviation, severity);

        if (need.Status == NeedStatus.WaitingForEffect)
        {
            var satisfactionThreshold = need.DesiredMax - opts.ResolutionHysteresis;
            if (need.Type == NeedType.Co2Reduction && current <= satisfactionThreshold)
            {
                need.SetStableSince();
                var duration = now - need.StableSince!.Value;
                if (duration >= opts.MinimumSatisfactionDuration)
                {
                    need.Satisfy();
                    await _needRepo.UpdateAsync(need, ct);
                    await SaveEvaluationAsync(need, trigger, "WaitingForEffect", "Satisfied", correlationId, causationId, ct);
                    var eventId = await PublishNeedEventAsync("need.satisfied", need, correlationId, causationId, ct);
                    _eventBus.Publish(new EnvironmentUpdatedEvent
                    {
                        RoomId = need.RoomId,
                        Timestamp = now,
                        EventType = "need.satisfied",
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        PayloadJson = JsonSerializer.Serialize(new { eventId = eventId.ToString() })
                    });
                    return;
                }
            }
            else
            {
                need.ClearStableSince();
            }

            await _needRepo.UpdateAsync(need, ct);
            await SaveEvaluationAsync(need, trigger, "WaitingForEffect", "WaitingForEffect", correlationId, causationId, ct);
            return;
        }

        if (need.Status == NeedStatus.Blocked)
        {
            await _needRepo.UpdateAsync(need, ct);
            return;
        }

        await _needRepo.UpdateAsync(need, ct);
        await SaveEvaluationAsync(need, trigger, need.Status.ToString(), need.Status.ToString(), correlationId, causationId, ct);

        if (need.CooldownUntil.HasValue && now < need.CooldownUntil.Value)
            return;

        if (need.ActiveCommandId is not null)
            return;

        if (mode == ControlMode.Automatic && need.Status == NeedStatus.Detected)
        {
            await PlanAndExecuteAsync(need, trigger, correlationId, causationId, ct);
        }
    }

    private async Task CreateNewNeedAsync(RoomId roomId, BuildingId buildingId, NeedType needType, NeedSeverity severity,
        double deviation, double current, double min, double max, double pref,
        string parameter, ControlMode mode, RoomPolicyDto? policy, DateTimeOffset now,
        ParameterOptions opts, NeedEvaluationTrigger trigger, string? correlationId, string? causationId,
        CancellationToken ct)
    {
        if (mode == ControlMode.Disabled) return;

        // Anti-oscillation: cooldown check — if a recent command was sent for this parameter, block new Need creation
        var recentNeed = await _needRepo.GetActiveByTypeAsync(roomId, needType, ct);
        if (recentNeed is not null)
        {
            if (recentNeed.CooldownUntil.HasValue && now < recentNeed.CooldownUntil.Value)
            {
                _logger.LogDebug("Need {NeedType} for room {RoomId} is in cooldown until {CooldownUntil}, suppressing new Need creation",
                    needType, roomId, recentNeed.CooldownUntil.Value);
                return;
            }
        }

        var need = Need.Create(
            buildingId, roomId, needType, severity,
            min, max, pref, current, deviation, parameter, mode);

        need.SetViolationSince();
        await _needRepo.AddAsync(need, ct);
        await SaveEvaluationAsync(need, trigger, null, "Detected", correlationId, causationId, ct);

        var eventId = await PublishNeedEventAsync("need.detected", need, correlationId, causationId, ct);
        _eventBus.Publish(new EnvironmentUpdatedEvent
        {
            RoomId = roomId,
            Timestamp = now,
            EventType = "need.detected",
            CorrelationId = correlationId,
            CausationId = causationId,
            PayloadJson = JsonSerializer.Serialize(new { eventId = eventId.ToString() })
        });

        if (mode == ControlMode.Automatic)
            await PlanAndExecuteAsync(need, trigger, correlationId, causationId, ct);
    }

    public async Task PlanAndExecuteAsync(Need need,
        NeedEvaluationTrigger trigger = NeedEvaluationTrigger.ManualRequest,
        string? correlationId = null, string? causationId = null,
        CancellationToken ct = default)
    {
        try
        {
            if (need.ActiveCommandId is not null || need.ActiveCommandPlanId is not null)
            {
                _logger.LogDebug("Need {NeedId} already has active command/plan", need.Id);
                return;
            }

            if (need.Mode != ControlMode.Automatic && trigger != NeedEvaluationTrigger.ManualRequest)
            {
                _logger.LogDebug("Need {NeedId} mode is {Mode}, skipping automatic planning", need.Id, need.Mode);
                return;
            }

            need.BeginPlanning();
            await _needRepo.UpdateAsync(need, ct);

            var engCapCode = CapabilityPlanner.GetEngineeringCapabilityCode(need.Type);
            if (engCapCode is null)
            {
                need.Block("CAPABILITY_PLAN_NOT_FOUND");
                await _needRepo.UpdateAsync(need, ct);
                await SaveEvaluationAsync(need, trigger, "Planning", "Blocked", correlationId, causationId, ct, failureCode: "CAPABILITY_PLAN_NOT_FOUND");
                await PublishNeedEventAsync("need.blocked", need, correlationId, causationId, ct);
                return;
            }

            await PlanThroughEngineeringSystemsAsync(need, engCapCode, trigger, correlationId, causationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Need {NeedId} planning failed", need.Id);
            need.Block("PLANNING_ERROR");
            await _needRepo.UpdateAsync(need, ct);
            await SaveEvaluationAsync(need, trigger, "Planning", "Blocked", correlationId, causationId, ct, failureCode: "PLANNING_ERROR");
            await PublishNeedEventAsync("need.blocked", need, correlationId, causationId, ct);
        }
    }

    private async Task PlanThroughEngineeringSystemsAsync(Need need, string engCapCode,
        NeedEvaluationTrigger trigger, string? correlationId, string? causationId,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Need {NeedId} forwarding to Climate Orchestrator", need.Id);
            var climateResult = await _climateModule.PlanRoomAsync(
                need.RoomId, "Comfort", ct);

            if (climateResult.Plan?.SubPlans.Any(sp =>
                sp.CapabilityCode == engCapCode && sp.Priority != "Blocked") == true)
            {
                var commandPlanId = climateResult.Plan.PlanId;
                need.MarkEngineeringPlanned("climate-orchestrator", engCapCode, commandPlanId);
                await _needRepo.UpdateAsync(need, ct);
                await SaveEvaluationAsync(need, trigger, "Planning", "Planned",
                    correlationId, causationId, ct);
                await PublishNeedEventAsync("need.planned", need, correlationId, causationId, ct);
                _logger.LogInformation("Need {NeedId} planned via Climate Orchestrator, PlanId {PlanId}",
                    need.Id, commandPlanId);
                return;
            }

            _logger.LogWarning("Need {NeedId} Climate Orchestrator had no matching plan for capability {CapabilityCode}. " +
                "Need → Climate → Engineering chain failed. Blocking need.",
                need.Id, engCapCode);

            need.Block("CLIMATE_PLAN_MISSING_CAPABILITY");
            await _needRepo.UpdateAsync(need, ct);
            await SaveEvaluationAsync(need, trigger, "Planning", "Blocked",
                correlationId, causationId, ct, failureCode: "CLIMATE_PLAN_MISSING_CAPABILITY");
            await PublishNeedEventAsync("need.blocked", need, correlationId, causationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Need {NeedId} climate planning failed", need.Id);
            need.Block("CLIMATE_PLANNING_ERROR");
            await _needRepo.UpdateAsync(need, ct);
            await SaveEvaluationAsync(need, trigger, "Planning", "Blocked",
                correlationId, causationId, ct, failureCode: "CLIMATE_PLANNING_ERROR");
            await PublishNeedEventAsync("need.blocked", need, correlationId, causationId, ct);
        }
    }

    public async Task HandleCommandTerminalEventAsync(NeedId needId, CmdId commandId, string commandStatus,
        NeedEvaluationTrigger trigger, string? correlationId = null, string? causationId = null,
        CancellationToken ct = default)
    {
        var need = await _needRepo.GetByIdAsync(needId, ct);
        if (need is null)
        {
            _logger.LogWarning("Need {NeedId} not found for command {CommandId} terminal event", needId, commandId);
            return;
        }

        if (need.ActiveCommandId != commandId)
        {
            _logger.LogWarning("Command {CommandId} does not match Need {NeedId} active command {ActiveCmd}",
                commandId, needId, need.ActiveCommandId);
            return;
        }

        var opts = _antiOscillation.GetForParameter(need.SourceParameterCode ?? "co2");

        switch (trigger)
        {
            case NeedEvaluationTrigger.CommandSucceeded:
                need.WaitForEffect(opts.EffectEvaluationDelay);
                await _needRepo.UpdateAsync(need, ct);
                await SaveEvaluationAsync(need, trigger, "Executing", "WaitingForEffect", correlationId, causationId, ct, commandId: commandId);
                await PublishNeedEventAsync("need.waitingForEffect", need, correlationId, causationId, ct);
                break;

            case NeedEvaluationTrigger.CommandFailed:
                need.SetCooldown(opts.CommandCooldown);
                need.ClearActiveCommand();
                await _needRepo.UpdateAsync(need, ct);
                await SaveEvaluationAsync(need, trigger, "Executing", "Detected", correlationId, causationId, ct, commandId: commandId, failureCode: "COMMAND_EXECUTION_FAILED");
                await PublishNeedEventAsync("need.updated", need, correlationId, causationId, ct);
                break;

            case NeedEvaluationTrigger.CommandTimedOut:
                if (need.CommandAttemptCount >= 2)
                {
                    need.Block("COMMAND_TIMED_OUT_RETRY_EXCEEDED");
                    await SaveEvaluationAsync(need, trigger, "Executing", "Blocked", correlationId, causationId, ct, commandId: commandId, failureCode: "COMMAND_TIMED_OUT_RETRY_EXCEEDED");
                }
                else
                {
                    need.SetCooldown(opts.CommandCooldown);
                    need.ClearActiveCommand();
                    await SaveEvaluationAsync(need, trigger, "Executing", "Detected", correlationId, causationId, ct, commandId: commandId, failureCode: "COMMAND_TIMED_OUT");
                }
                await _needRepo.UpdateAsync(need, ct);
                await PublishNeedEventAsync("need.updated", need, correlationId, causationId, ct);
                break;

            case NeedEvaluationTrigger.CommandCancelled:
                need.ClearActiveCommand();
                await _needRepo.UpdateAsync(need, ct);
                await SaveEvaluationAsync(need, trigger, "Executing", "Detected", correlationId, causationId, ct, commandId: commandId);
                await PublishNeedEventAsync("need.updated", need, correlationId, causationId, ct);
                break;

            case NeedEvaluationTrigger.CommandRejected:
                need.Block("COMMAND_REJECTED");
                need.ClearActiveCommand();
                await _needRepo.UpdateAsync(need, ct);
                await SaveEvaluationAsync(need, trigger, "Executing", "Blocked", correlationId, causationId, ct, commandId: commandId, failureCode: "COMMAND_REJECTED");
                await PublishNeedEventAsync("need.blocked", need, correlationId, causationId, ct);
                break;
        }
    }

    private async Task<Guid> PublishNeedEventAsync(string eventType, Need need, string? correlationId, string? causationId, CancellationToken ct)
    {
        var payload = new
        {
            needId = need.Id.ToString(),
            buildingId = need.BuildingId.ToString(),
            roomId = need.RoomId.ToString(),
            type = need.Type.ToString(),
            status = need.Status.ToString(),
            severity = need.Severity.ToString(),
            mode = need.Mode.ToString(),
            currentValue = need.CurrentValue,
            deviation = need.Deviation,
            activeCommandId = need.ActiveCommandId?.ToString(),
            activeCommandPlanId = need.ActiveCommandPlanId?.ToString(),
            selectedDeviceId = need.SelectedDeviceId?.ToString(),
            selectedCapabilityCode = need.SelectedCapabilityCode,
            selectedEngineeringSystemId = need.SelectedEngineeringSystemId?.ToString(),
            selectedEngineeringCapabilityCode = need.SelectedEngineeringCapabilityCode,
            planningFailureCode = need.PlanningFailureCode,
            violationSince = need.ViolationSince,
            stableSince = need.StableSince,
            cooldownUntil = need.CooldownUntil,
            effectEvaluationDueAt = need.EffectEvaluationDueAt,
            lastEvaluationAt = need.LastEvaluationAt,
            version = need.Version
        };

        return await _internalEventOutbox.CreateAsync(
            eventType: eventType,
            aggregateType: "Need",
            aggregateId: need.Id.ToString(),
            buildingId: need.BuildingId.Value,
            roomId: need.RoomId.Value,
            payload: payload,
            headers: null,
            correlationId: correlationId,
            causationId: causationId,
            occurredAt: DateTimeOffset.UtcNow,
            ct: ct);
    }

    private async Task SaveEvaluationAsync(Need need, NeedEvaluationTrigger trigger, string? previousStatus, string? newStatus,
        string? correlationId, string? causationId, CancellationToken ct,
        CmdId? commandId = null, string? failureCode = null)
    {
        try
        {
            var evaluation = NeedEvaluation.Create(
                need.Id, need.BuildingId, need.RoomId, trigger,
                previousStatus, newStatus,
                calculationResultJson: JsonSerializer.Serialize(new
                {
                    currentValue = need.CurrentValue,
                    deviation = need.Deviation,
                    severity = need.Severity.ToString()
                }),
                capabilityPlanJson: need.SelectedCapabilityCode is not null
                    ? JsonSerializer.Serialize(new { capabilityCode = need.SelectedCapabilityCode, deviceId = need.SelectedDeviceId?.ToString() })
                    : null,
                commandId: commandId,
                outcome: newStatus,
                failureCode: failureCode,
                correlationId: correlationId,
                causationId: causationId);

            await _needRepo.AddEvaluationAsync(evaluation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save evaluation history for need {NeedId}", need.Id);
        }
    }

    private static ControlMode FromPolicyMode(string mode) => mode switch
    {
        "automatic" => ControlMode.Automatic,
        "manual" => ControlMode.Manual,
        "disabled" => ControlMode.Disabled,
        _ => ControlMode.MonitorOnly
    };

    private static double MapParameterToDefaultMax(string parameter) => parameter switch
    {
        "temperature" => 26,
        "humidity" => 60,
        "co2" => 1000,
        "illuminance" => 750,
        _ => 1000
    };

    private static NeedType MapParameterToNeedType(string parameter) => parameter switch
    {
        "temperature" => NeedType.TemperatureHeating,
        "humidity" => NeedType.HumidityIncrease,
        "co2" => NeedType.Co2Reduction,
        "illuminance" => NeedType.IlluminanceIncrease,
        _ => NeedType.Co2Reduction
    };

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeFromContract(
        string parameter, double current, RoomPolicyDto? policy) => parameter switch
        {
            "temperature" => ComputeTemperature(current, policy),
            "humidity" => ComputeHumidity(current, policy),
            "co2" => ComputeCo2(current, policy),
            "illuminance" => ComputeIlluminance(current, policy),
            _ => (null, NeedSeverity.Low, 0.0, 0.0, 0.0, 0.0)
        };

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeTemperature(
        double current, RoomPolicyDto? p)
    {
        var (min, max, pref) = (p?.TemperatureMin ?? 20, p?.TemperatureMax ?? 26, p?.TemperaturePreferred ?? 23);
        if (current >= min && current <= max) return (null, NeedSeverity.Low, 0, min, max, pref);
        var dev = current < min ? min - current : current - max;
        var sev = dev switch { > 8 => NeedSeverity.Critical, > 4 => NeedSeverity.High, > 2 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (current < min ? NeedType.TemperatureHeating : NeedType.TemperatureCooling, sev, dev, min, max, pref);
    }

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeHumidity(
        double current, RoomPolicyDto? p)
    {
        var (min, max, pref) = (p?.HumidityMin ?? 30, p?.HumidityMax ?? 60, p?.HumidityPreferred ?? 45);
        if (current >= min && current <= max) return (null, NeedSeverity.Low, 0, min, max, pref);
        var dev = current < min ? min - current : current - max;
        var sev = dev switch { > 20 => NeedSeverity.Critical, > 10 => NeedSeverity.High, > 5 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (current < min ? NeedType.HumidityIncrease : NeedType.HumidityDecrease, sev, dev, min, max, pref);
    }

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeCo2(
        double current, RoomPolicyDto? p)
    {
        var max = p?.Co2Max ?? 1000;
        if (current <= max) return (null, NeedSeverity.Low, 0, 0, max, p?.Co2Preferred ?? 600);
        var dev = current - max;
        var sev = dev switch { > 1000 => NeedSeverity.Critical, > 500 => NeedSeverity.High, > 200 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (NeedType.Co2Reduction, sev, dev, 0, max, p?.Co2Preferred ?? 600);
    }

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeIlluminance(
        double current, RoomPolicyDto? p)
    {
        var (min, max, pref) = (p?.IlluminanceMin ?? 300, p?.IlluminanceMax ?? 750, p?.IlluminancePreferred ?? 500);
        if (current >= min && current <= max) return (null, NeedSeverity.Low, 0, min, max, pref);
        var dev = current < min ? min - current : current - max;
        var sev = dev switch { > 300 => NeedSeverity.Critical, > 150 => NeedSeverity.High, > 50 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (current < min ? NeedType.IlluminanceIncrease : NeedType.IlluminanceDecrease, sev, dev, min, max, pref);
    }
}
