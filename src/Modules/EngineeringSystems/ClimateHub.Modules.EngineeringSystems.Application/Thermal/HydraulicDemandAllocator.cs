using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

namespace ClimateHub.Modules.EngineeringSystems.Application.Thermal;

public class HydraulicAllocationPlan
{
    public List<(HydraulicCircuit Circuit, double AllocatedPowerKw)> CircuitAllocations { get; init; } = new();
    public List<(Guid ZoneId, double AllocatedPowerKw)> ZoneAllocations { get; init; } = new();
    public double RequiredFlowM3h { get; set; }
    public double AvailableFlowM3h { get; set; }
    public double RequiredPowerKw { get; set; }
    public double AllocatedPowerKw { get; set; }
    public double UnservedPowerKw { get; set; }
    public string BalanceStatus { get; set; } = "Balanced";
}

public class HydraulicDemandAllocator
{
    public HydraulicAllocationPlan Allocate(
        List<HydraulicCircuit> circuits, 
        List<(Guid ZoneId, double RequiredPowerKw, int Priority)> zoneDemands,
        double totalAvailablePowerKw)
    {
        var plan = new HydraulicAllocationPlan();
        double remainingPower = totalAvailablePowerKw;

        var sortedZones = zoneDemands.OrderBy(z => z.Priority).ToList();

        foreach (var (zoneId, requiredPower, priority) in sortedZones)
        {
            var allocated = Math.Min(requiredPower, remainingPower);
            plan.ZoneAllocations.Add((zoneId, allocated));
            plan.RequiredPowerKw += requiredPower;
            plan.AllocatedPowerKw += allocated;
            remainingPower -= allocated;
        }

        foreach (var circuit in circuits.Where(c => c.LifecycleStatus == LifecycleStatus.Active))
        {
            var circuitPower = totalAvailablePowerKw * 0.5;
            plan.CircuitAllocations.Add((circuit, circuitPower));
            plan.RequiredFlowM3h += circuit.DesignFlowM3h;
            plan.AvailableFlowM3h = circuit.MaximumFlowM3h;
        }

        plan.UnservedPowerKw = Math.Max(0, plan.RequiredPowerKw - plan.AllocatedPowerKw);
        plan.BalanceStatus = plan.UnservedPowerKw > 0 ? "Insufficient" : "Balanced";

        return plan;
    }
}