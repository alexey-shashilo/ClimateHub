using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Humidification;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.Environment.Contracts;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public class HumidificationPlanningService : IHumidificationPlanningService
{
    private readonly ICommandPlanRepository _planRepo;
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly HumidificationStrategyEngine _humidificationStrategy;
    private readonly CommandPlanExecutor _executor;

    public HumidificationPlanningService(
        ICommandPlanRepository planRepo,
        IRoomEnvironmentStateReader envReader,
        HumidificationStrategyEngine humidificationStrategy,
        CommandPlanExecutor executor)
    {
        _planRepo = planRepo;
        _envReader = envReader;
        _humidificationStrategy = humidificationStrategy;
        _executor = executor;
    }

    public async Task<CapabilityPlanResultDto> PlanHumidificationAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.HumidificationConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.HumidificationConfigurationInvalid,
                "Humidification system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var currentRh = 40.0;
        var rhParam = parameters.FirstOrDefault(p => p.Parameter == "humidity");
        if (rhParam?.Value.HasValue == true) currentRh = rhParam.Value.Value;

        var roomTemp = 22.0;
        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        if (tempParam?.Value.HasValue == true) roomTemp = tempParam.Value.Value;

        var outdoorTemp = 5.0;
        var outdoorParam = parameters.FirstOrDefault(p => p.Parameter == "outdoor_temperature");
        if (outdoorParam?.Value.HasValue == true) outdoorTemp = outdoorParam.Value.Value;

        var targetRh = config.TargetRhPercent;
        var airflow = 300.0;

        var strategyResult = _humidificationStrategy.PlanHumidification(
            config, currentRh, targetRh, roomTemp, 14, airflow,
            outdoorTemp, null, HumidifierType.SteamElectrode);

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            strategyResult.RequiredCapacityKgH, "kg/h", "HumidificationStrategyEngine",
            strategyResult.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: $"Humidification: {strategyResult.RequiredCapacityKgH:F1} kg/h, steam={strategyResult.SteamOutputPct:F0}%",
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);

        var execResult = await _executor.ExecuteAsync(commandPlan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Humidification command plan execution failed");
    }
}