using ClimateHub.Modules.Climate.Domain.ClimatePlans;

namespace ClimateHub.Modules.Climate.Application.DependencyGraph;

public class DependencyValidationResult
{
    public bool IsValid { get; init; }
    public bool HasCycle { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<Guid> ReadySubPlanIds { get; init; } = new();
    public string? CycleDescription { get; init; }
}

public class ClimateDependencyGraphValidator
{
    public DependencyValidationResult Validate(ClimatePlan plan)
    {
        var errors = new List<string>();
        var subPlans = plan.SubPlans.ToList();
        var dependencies = plan.Dependencies.ToList();
        var subPlanIds = subPlans.Select(s => s.Id).ToHashSet();

        foreach (var dep in dependencies)
        {
            if (dep.PredecessorSubPlanId == dep.SuccessorSubPlanId)
                errors.Add($"Self-dependency on sub-plan {dep.PredecessorSubPlanId}");
        }

        foreach (var dep in dependencies)
        {
            if (!subPlanIds.Contains(dep.PredecessorSubPlanId))
                errors.Add($"Dependency predecessor {dep.PredecessorSubPlanId} not found in sub-plans");
            if (!subPlanIds.Contains(dep.SuccessorSubPlanId))
                errors.Add($"Dependency successor {dep.SuccessorSubPlanId} not found in sub-plans");
        }

        var edgeSet = new HashSet<(Guid, Guid)>();
        foreach (var dep in dependencies)
        {
            if (!edgeSet.Add((dep.PredecessorSubPlanId, dep.SuccessorSubPlanId)))
                errors.Add($"Duplicate dependency: {dep.PredecessorSubPlanId} → {dep.SuccessorSubPlanId}");
        }

        var cycleResult = DetectCycle(dependencies);
        if (cycleResult.HasCycle)
        {
            errors.Add($"Dependency cycle detected: {cycleResult.CycleDescription}");
        }

        if (errors.Count > 0)
            return new DependencyValidationResult { IsValid = false, HasCycle = cycleResult.HasCycle, Errors = errors, CycleDescription = cycleResult.CycleDescription };

        var readyIds = GetReadySubPlans(plan);

        return new DependencyValidationResult { IsValid = true, ReadySubPlanIds = readyIds };
    }

    public (bool HasCycle, string? CycleDescription) DetectCycle(List<ClimatePlanDependency> dependencies)
    {
        var graph = new Dictionary<Guid, List<Guid>>();
        foreach (var dep in dependencies)
        {
            if (!graph.ContainsKey(dep.PredecessorSubPlanId))
                graph[dep.PredecessorSubPlanId] = new List<Guid>();
            graph[dep.PredecessorSubPlanId].Add(dep.SuccessorSubPlanId);

            if (!graph.ContainsKey(dep.SuccessorSubPlanId))
                graph[dep.SuccessorSubPlanId] = new List<Guid>();
        }

        var white = new HashSet<Guid>(graph.Keys);
        var gray = new HashSet<Guid>();
        var black = new HashSet<Guid>();

        foreach (var node in new List<Guid>(white))
        {
            if (DfsCycleCheck(node, graph, white, gray, black, new List<Guid>(), out var cycle))
                return (true, string.Join(" → ", cycle.Select(g => g.ToString("N").Substring(0, 8))));
        }

        return (false, null);
    }

    private static bool DfsCycleCheck(Guid node, Dictionary<Guid, List<Guid>> graph,
        HashSet<Guid> white, HashSet<Guid> gray, HashSet<Guid> black,
        List<Guid> path, out List<Guid> cycle)
    {
        cycle = new List<Guid>();
        white.Remove(node);
        gray.Add(node);
        path.Add(node);

        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (black.Contains(neighbor)) continue;
                if (gray.Contains(neighbor))
                {
                    var cycleStart = path.IndexOf(neighbor);
                    cycle = path.Skip(cycleStart).Concat(new[] { neighbor }).ToList();
                    return true;
                }
                if (DfsCycleCheck(neighbor, graph, white, gray, black, path, out var subCycle))
                {
                    cycle = subCycle;
                    return true;
                }
            }
        }

        gray.Remove(node);
        black.Add(node);
        path.RemoveAt(path.Count - 1);
        return false;
    }

    public List<Guid> GetReadySubPlans(ClimatePlan plan)
    {
        if (plan.Status.IsTerminal()) return new List<Guid>();

        var ready = new List<Guid>();
        var deps = plan.Dependencies.ToList();
        var subPlans = plan.SubPlans.ToList();

        foreach (var subPlan in subPlans)
        {
            if (subPlan.Status != Domain.ClimatePlans.SubPlanStatus.Pending
                && subPlan.Status != Domain.ClimatePlans.SubPlanStatus.BlockedByDependency)
                continue;

            var predecessors = deps.Where(d => d.SuccessorSubPlanId == subPlan.Id).ToList();

            if (predecessors.Count == 0)
            {
                if (!HasUnsatisfiedSafetyGate(plan, subPlan.Id))
                    ready.Add(subPlan.Id);
                continue;
            }

            var allPredecessorsSatisfied = true;
            foreach (var pred in predecessors)
            {
                var predSubPlan = subPlans.FirstOrDefault(s => s.Id == pred.PredecessorSubPlanId);
                if (predSubPlan is null) { allPredecessorsSatisfied = false; break; }

                if (pred.Type == Domain.ClimatePlans.DependencyType.SafetyGate)
                {
                    if (predSubPlan.Status != Domain.ClimatePlans.SubPlanStatus.Completed
                        && predSubPlan.Status != Domain.ClimatePlans.SubPlanStatus.EngineeringSucceeded)
                        allPredecessorsSatisfied = false;
                }
                else if (pred.Required)
                {
                    if (predSubPlan.Status != Domain.ClimatePlans.SubPlanStatus.Completed
                        && predSubPlan.Status != Domain.ClimatePlans.SubPlanStatus.EngineeringSucceeded)
                        allPredecessorsSatisfied = false;
                }
            }

            if (allPredecessorsSatisfied)
                ready.Add(subPlan.Id);
        }

        return ready;
    }

    public static bool HasUnsatisfiedSafetyGate(ClimatePlan plan, Guid subPlanId)
    {
        var deps = plan.Dependencies.Where(d => d.SuccessorSubPlanId == subPlanId && d.Type == Domain.ClimatePlans.DependencyType.SafetyGate).ToList();
        foreach (var dep in deps)
        {
            var pred = plan.SubPlans.FirstOrDefault(s => s.Id == dep.PredecessorSubPlanId);
            if (pred is null || (pred.Status != Domain.ClimatePlans.SubPlanStatus.Completed
                && pred.Status != Domain.ClimatePlans.SubPlanStatus.EngineeringSucceeded))
                return true;
        }
        return false;
    }
}

public static class ClimatePlanStatusExtensions
{
    public static bool IsTerminal(this Domain.ClimatePlans.ClimatePlanStatus status) => status switch
    {
        Domain.ClimatePlans.ClimatePlanStatus.Completed => true,
        Domain.ClimatePlans.ClimatePlanStatus.PartiallyCompleted => true,
        Domain.ClimatePlans.ClimatePlanStatus.Failed => true,
        Domain.ClimatePlans.ClimatePlanStatus.Cancelled => true,
        Domain.ClimatePlans.ClimatePlanStatus.Expired => true,
        _ => false
    };
}