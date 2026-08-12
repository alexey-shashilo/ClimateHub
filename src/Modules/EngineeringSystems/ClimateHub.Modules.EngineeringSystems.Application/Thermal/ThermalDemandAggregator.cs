using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Application.Thermal;

public class ThermalZoneDemandPlan
{
    public Guid ThermalZoneId { get; init; }
    public List<ThermalDemand> RoomDemands { get; init; } = new();
    public double TotalRequiredHeatingPowerKw { get; set; }
    public double PeakRequiredHeatingPowerKw { get; set; }
    public double WeightedTargetTemperatureC { get; set; }
    public int Priority { get; set; }
    public double UnservedDemandKw { get; set; }
    public string Quality { get; set; } = "Estimated";
}

public class ThermalDemandAggregator
{
    public ThermalZoneDemandPlan Aggregate(ThermalZone zone, List<ThermalDemand> activeDemands)
    {
        var sortedDemands = activeDemands
            .Where(d => d.Status == "Active")
            .OrderBy(d => d.Severity switch { "Critical" => 0, "High" => 1, "Medium" => 2, "Low" => 3, _ => 4 })
            .ThenBy(d => d.Priority)
            .ToList();

        var totalPower = sortedDemands.Sum(d => Math.Min(d.RequiredHeatingPowerKw, zone.DesignHeatLoadKw));
        totalPower = Math.Min(totalPower, zone.DesignHeatLoadKw);

        var weightedTemp = sortedDemands.Count > 0
            ? sortedDemands.Average(d => d.TargetTemperatureC)
            : zone.TargetTemperatureC;

        return new ThermalZoneDemandPlan
        {
            ThermalZoneId = zone.Id.Value,
            RoomDemands = sortedDemands,
            TotalRequiredHeatingPowerKw = totalPower,
            PeakRequiredHeatingPowerKw = totalPower,
            WeightedTargetTemperatureC = weightedTemp,
            Priority = zone.Priority,
            Quality = "Estimated"
        };
    }
}
