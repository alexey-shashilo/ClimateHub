using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public class ThermalDemand
{
    public Guid Id { get; private set; }
    public string? ClimatePlanId { get; private set; }
    public string? EngineeringSubPlanId { get; private set; }
    public Guid ThermalZoneId { get; private init; }
    public RoomId RoomId { get; private init; }
    public string DemandType { get; private set; } = "Heating";
    public double CurrentTemperatureC { get; private set; }
    public double TargetTemperatureC { get; private set; }
    public double DeviationC { get; private set; }
    public double RequiredHeatingPowerKw { get; private set; }
    public int Priority { get; private set; }
    public string Severity { get; private set; } = "Medium";
    public string Status { get; private set; } = "Active";
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private ThermalDemand() { RoomId = RoomId.From(Guid.Empty); }

    public ThermalDemand(Guid thermalZoneId, RoomId roomId, string demandType,
        double currentTemp, double targetTemp, double requiredPowerKw,
        int priority = 100, string severity = "Medium")
    {
        Id = Guid.NewGuid(); ThermalZoneId = thermalZoneId; RoomId = roomId;
        DemandType = demandType; CurrentTemperatureC = currentTemp;
        TargetTemperatureC = targetTemp; DeviationC = targetTemp - currentTemp;
        RequiredHeatingPowerKw = requiredPowerKw; Priority = priority;
        Severity = severity; Status = "Active"; CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Satisfy() { Status = "Satisfied"; }
    public void Expire() { Status = "Expired"; ExpiresAt = DateTimeOffset.UtcNow; }
}
