using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.Environment.Contracts;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public class StandardEngineeringPlanningService : IStandardEngineeringPlanningService
{
    private readonly ICommandPlanRepository _planRepo;
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly StrategyEngine _strategyEngine;
    private readonly CommandPlanExecutor _executor;

    public StandardEngineeringPlanningService(
        ICommandPlanRepository planRepo,
        IRoomEnvironmentStateReader envReader,
        StrategyEngine strategyEngine,
        CommandPlanExecutor executor)
    {
        _planRepo = planRepo;
        _envReader = envReader;
        _strategyEngine = strategyEngine;
        _executor = executor;
    }

    public async Task<CapabilityPlanResultDto> PlanStandardAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CapabilityRoutingResult routing,
        CancellationToken ct)
    {
        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var co2Param = parameters.FirstOrDefault(p => p.Parameter == "co2");
        var currentValue = co2Param?.Value ?? request.CurrentValue ?? 400;
        var deviation = request.Severity switch
        {
            "Critical" => 1000, "High" => 500, "Medium" => 200, _ => 100
        };

        var strategyResult = await _strategyEngine.PlanAsync(
            system, routing.CapabilityCode!, currentValue, deviation,
            request.TargetValue ?? 1000, ct);

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var plan = CommandPlan.Create(
            system.Id, strategyResult.Plan!.NeedType, strategyResult.Plan.CapabilityCode,
            strategyResult.Plan.RequestedValue, strategyResult.Plan.ValueUnit,
            strategyResult.Plan.StrategyName, strategyResult.Plan.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(plan, ct);

        var execResult = await _executor.ExecuteAsync(plan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(plan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Command plan execution failed");
    }
}