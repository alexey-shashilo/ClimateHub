using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Application.Execution;

public class ClimateExecutionResult
{
    public string CapabilityCode { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? CommandPlanId { get; init; }
    public string? FailureCode { get; init; }
}

public class ClimateExecutionCoordinator
{
    private readonly IEngineeringSystemsModule _engineeringSystems;

    public ClimateExecutionCoordinator(IEngineeringSystemsModule engineeringSystems)
    {
        _engineeringSystems = engineeringSystems;
    }

    public async Task<List<ClimateExecutionResult>> ExecuteSubPlansAsync(
        ClimatePlan plan, RoomId roomId,
        CancellationToken ct = default)
    {
        var results = new List<ClimateExecutionResult>();
        var orderedPlans = plan.SubPlans
            .Where(sp => sp.PriorityCategory != "Blocked")
            .OrderBy(sp => sp.ExecutionOrder)
            .ToList();

        foreach (var subPlan in orderedPlans)
        {
            var request = new EngineeringCapabilityRequestDto(
                EngineeringCapabilityRequestId.New(),
                plan.Id.ToString(),
                plan.BuildingId,
                roomId,
                subPlan.EngineeringCapabilityCode,
                subPlan.PriorityCategory == "High" ? "High" : "Medium",
                1, subPlan.RequestedEffect,
                null, null,
                DateTimeOffset.UtcNow, null,
                null, null,
                $"climate:{plan.Id}:{subPlan.Id}");

            try
            {
                var engResult = await _engineeringSystems.PlanAsync(request, ct);
                if (engResult.Success && engResult.CommandPlanId.HasValue)
                {
                    subPlan.AssignCommandPlan(engResult.CommandPlanId.Value.ToString());
                    results.Add(new ClimateExecutionResult
                    {
                        CapabilityCode = subPlan.EngineeringCapabilityCode,
                        Success = true,
                        CommandPlanId = engResult.CommandPlanId.Value.ToString()
                    });
                }
                else
                {
                    subPlan.MarkEngineeringFailed(engResult.FailureCode);
                    results.Add(new ClimateExecutionResult
                    {
                        CapabilityCode = subPlan.EngineeringCapabilityCode,
                        Success = false,
                        FailureCode = engResult.FailureCode
                    });
                }
            }
            catch (Exception ex)
            {
                subPlan.MarkEngineeringFailed(ex.GetType().Name);
                results.Add(new ClimateExecutionResult
                {
                    CapabilityCode = subPlan.EngineeringCapabilityCode,
                    Success = false,
                    FailureCode = ex.GetType().Name
                });
            }
        }

        return results;
    }
}