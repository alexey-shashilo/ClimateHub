using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain;

public class VentilationDemand
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public string NeedId { get; private set; }
    public RoomId RoomId { get; private set; }
    public string Severity { get; private set; }
    public double CurrentCo2 { get; private set; }
    public double TargetCo2 { get; private set; }
    public double MinimumAirflow { get; private set; }
    public double PreferredAirflow { get; private set; }
    public double MaximumAirflow { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset DetectedAt { get; private init; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public uint Version { get; private set; }

    private VentilationDemand() { NeedId = string.Empty; Severity = string.Empty; }

    public VentilationDemand(Guid engineeringSystemId, string needId, RoomId roomId,
        string severity, double currentCo2, double targetCo2,
        double minAirflow, double preferredAirflow, double maxAirflow,
        int priority = 100, DateTimeOffset? expiresAt = null)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engineeringSystemId;
        NeedId = needId; RoomId = roomId; Severity = severity;
        CurrentCo2 = currentCo2; TargetCo2 = targetCo2;
        MinimumAirflow = minAirflow; PreferredAirflow = preferredAirflow;
        MaximumAirflow = maxAirflow; Priority = priority;
        IsActive = true; DetectedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt; Version = 1;
    }

    public void Deactivate() { IsActive = false; Version++; }
    public void SetAirflowLimits(double min, double preferred, double max) { MinimumAirflow = min; PreferredAirflow = preferred; MaximumAirflow = max; Version++; }
    public void Expire() { IsActive = false; ExpiresAt = DateTimeOffset.UtcNow; Version++; }

    public static double CalculatePreferredAirflow(string severity, double roomMaxAirflow)
    {
        return severity switch
        {
            "Critical" => roomMaxAirflow,
            "High" => roomMaxAirflow * 0.75,
            "Medium" => roomMaxAirflow * 0.5,
            "Low" => roomMaxAirflow * 0.3,
            _ => roomMaxAirflow * 0.3
        };
    }
}

public class RoomAirflowAllocation
{
    public Guid Id { get; private set; }
    public Guid CommandPlanId { get; private set; }
    public RoomId RoomId { get; private set; }
    public double RequestedAirflow { get; private set; }
    public double AllocatedSupplyAirflow { get; private set; }
    public double AllocatedExhaustAirflow { get; private set; }
    public int Priority { get; private set; }
    public string AllocationQuality { get; private set; }
    public uint Version { get; private set; }

    private RoomAirflowAllocation() { AllocationQuality = "Full"; }

    public RoomAirflowAllocation(Guid commandPlanId, RoomId roomId,
        double requestedAirflow, double allocatedSupplyAirflow,
        double allocatedExhaustAirflow, int priority = 100,
        string allocationQuality = "Full")
    {
        Id = Guid.NewGuid(); CommandPlanId = commandPlanId;
        RoomId = roomId; RequestedAirflow = requestedAirflow;
        AllocatedSupplyAirflow = allocatedSupplyAirflow;
        AllocatedExhaustAirflow = allocatedExhaustAirflow;
        Priority = priority; AllocationQuality = allocationQuality;
        Version = 1;
    }
}

public class BalancedAirflowPlan
{
    public Guid Id { get; private set; }
    public double TotalSupplyAirflow { get; private set; }
    public double TotalExhaustAirflow { get; private set; }
    public double ImbalanceAirflow { get; private set; }
    public double ImbalancePct { get; private set; }
    public BalanceStatus Status { get; private set; }

    private BalancedAirflowPlan() { }

    public BalancedAirflowPlan(double totalSupply, double totalExhaust, double maxImbalancePct)
    {
        Id = Guid.NewGuid(); TotalSupplyAirflow = totalSupply;
        TotalExhaustAirflow = totalExhaust;
        ImbalanceAirflow = Math.Abs(totalSupply - totalExhaust);
        var maxFlow = Math.Max(totalSupply, totalExhaust);
        ImbalancePct = maxFlow > 0 ? ImbalanceAirflow / maxFlow * 100 : 0;
        Status = ImbalancePct <= maxImbalancePct ? BalanceStatus.Balanced : BalanceStatus.Imbalanced;
    }
}

public enum BalanceStatus { Balanced, Imbalanced, Unachievable }

public class VentilationDemandPlan
{
    public Guid EngineeringSystemId { get; private set; }
    public List<VentilationDemand> Demands { get; private set; } = new();
    public List<RoomAirflowAllocation> Allocations { get; private set; } = new();
    public double TotalRequestedSupplyAirflow { get; private set; }
    public double TotalRequestedExhaustAirflow { get; private set; }
    public double TotalAllocatedSupplyAirflow { get; private set; }
    public double TotalAllocatedExhaustAirflow { get; private set; }
    public double AvailableSupplyCapacity { get; private set; }
    public double AvailableExhaustCapacity { get; private set; }
    public double UnservedDemand { get; set; }
    public AllocationResult Result { get; private set; }
    public BalancedAirflowPlan? BalancePlan { get; private set; }

    private VentilationDemandPlan() { }

    public VentilationDemandPlan(Guid engineeringSystemId,
        List<VentilationDemand> demands, double availableSupply, double availableExhaust)
    {
        EngineeringSystemId = engineeringSystemId; Demands = demands;
        TotalRequestedSupplyAirflow = demands.Sum(d => d.PreferredAirflow);
        TotalRequestedExhaustAirflow = demands.Sum(d => d.PreferredAirflow);
        AvailableSupplyCapacity = availableSupply;
        AvailableExhaustCapacity = availableExhaust;
        Result = AllocationResult.Pending;
    }

    public void SetResult(AllocationResult result) { Result = result; }
    public void AddAllocation(RoomAirflowAllocation allocation) { Allocations.Add(allocation); }
    public void SetBalance(BalancedAirflowPlan balance) { BalancePlan = balance; }
}

public enum AllocationResult { Pending, FullyAllocated, PartiallyAllocated, InsufficientCapacity }