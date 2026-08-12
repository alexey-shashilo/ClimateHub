using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.Environment.Contracts;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public class VentilationPlanningService : IVentilationPlanningService
{
    private readonly ICommandPlanRepository _planRepo;
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly VentilationControlStrategy _ventilationStrategy;
    private readonly VentilationDemandFactory _demandFactory;
    private readonly CommandPlanExecutor _executor;

    public VentilationPlanningService(
        ICommandPlanRepository planRepo,
        IRoomEnvironmentStateReader envReader,
        VentilationControlStrategy ventilationStrategy,
        VentilationDemandFactory demandFactory,
        CommandPlanExecutor executor)
    {
        _planRepo = planRepo;
        _envReader = envReader;
        _ventilationStrategy = ventilationStrategy;
        _demandFactory = demandFactory;
        _executor = executor;
    }

    public async Task<CapabilityPlanResultDto> PlanVentilationAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.VentilationConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.VentilationConfigurationInvalid,
                "Ventilation system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var co2Param = parameters.FirstOrDefault(p => p.Parameter == "co2");
        var currentCo2 = co2Param?.Value ?? request.CurrentValue ?? 800;
        var targetCo2 = request.TargetValue ?? 1000;

        var outdoorTemp = 5.0;
        var outdoorParam = parameters.FirstOrDefault(p => p.Parameter == "outdoor_temperature");
        if (outdoorParam?.Value.HasValue == true)
            outdoorTemp = outdoorParam.Value.Value;

        var indoorTemp = 23.0;
        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        if (tempParam?.Value.HasValue == true)
            indoorTemp = tempParam.Value.Value;

        var roomMaxAirflow = config.DesignSupplyAirflow;
        var roomMinAirflow = config.MinimumSupplyAirflow;

        var demands = new List<VentilationDemand>
        {
            _demandFactory.CreateFromNeed(
                request.NeedId, request.RoomId, request.Severity,
                currentCo2, targetCo2, roomMaxAirflow, roomMinAirflow,
                system.Id.Value)
        };

        var strategyResult = _ventilationStrategy.Plan(
            demands, config, system, outdoorTemp, indoorTemp, null);

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var hvacPlan = strategyResult.Plan!;
        var totalAirflow = hvacPlan.TotalSupplyAirflow;

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            totalAirflow, "m3/h", "VentilationControlStrategy",
            hvacPlan.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: $"Ventilation: supply={hvacPlan.TotalSupplyAirflow:F0}, exhaust={hvacPlan.TotalExhaustAirflow:F0} m3/h",
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);

        var execResult = await _executor.ExecuteHvacPlanAsync(commandPlan, request.RoomId, system, hvacPlan, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "HVAC command plan execution failed");
    }
}
