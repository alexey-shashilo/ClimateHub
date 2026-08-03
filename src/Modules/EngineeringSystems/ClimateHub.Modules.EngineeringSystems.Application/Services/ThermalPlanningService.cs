using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.Environment.Contracts;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public class ThermalPlanningService : IThermalPlanningService
{
    private readonly ICommandPlanRepository _planRepo;
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly ThermalStrategyEngine _thermalStrategy;
    private readonly CommandPlanExecutor _executor;

    public ThermalPlanningService(
        ICommandPlanRepository planRepo,
        IRoomEnvironmentStateReader envReader,
        ThermalStrategyEngine thermalStrategy,
        CommandPlanExecutor executor)
    {
        _planRepo = planRepo;
        _envReader = envReader;
        _thermalStrategy = thermalStrategy;
        _executor = executor;
    }

    public async Task<CapabilityPlanResultDto> PlanThermalAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.ThermalConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.ThermalConfigurationInvalid,
                "Thermal system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var outdoorTemp = 5.0;
        var outdoorParam = parameters.FirstOrDefault(p => p.Parameter == "outdoor_temperature");
        if (outdoorParam?.Value.HasValue == true) outdoorTemp = outdoorParam.Value.Value;

        var indoorTemp = 22.0;
        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        if (tempParam?.Value.HasValue == true) indoorTemp = tempParam.Value.Value;

        var isHeating = request.CapabilityCode == EngineeringCapabilityCodes.IncreaseTemperature;
        ThermalStrategyResult strategyResult;

        if (isHeating)
        {
            strategyResult = await _thermalStrategy.PlanHeatingAsync(system, outdoorTemp, indoorTemp,
                0, request.TargetValue ?? 22, indoorTemp, ct: ct);
        }
        else
        {
            strategyResult = _thermalStrategy.PlanCooling(system, outdoorTemp, indoorTemp,
                request.TargetValue ?? 22, indoorTemp);
        }

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var requestedValue = strategyResult.RequiredPowerKw;
        var valueUnit = "W";
        var effectDesc = isHeating
            ? $"Heating: source={strategyResult.SelectedHeatSource?.SourceType}, supply={strategyResult.TargetSupplyTemperature:F0}°C, power={requestedValue:F0}W"
            : $"Cooling: supply={strategyResult.TargetSupplyTemperature:F0}°C";

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            requestedValue, valueUnit, "ThermalStrategyEngine",
            strategyResult.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: effectDesc,
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);

        var execResult = await _executor.ExecuteAsync(commandPlan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Thermal command plan execution failed");
    }
}