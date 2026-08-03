using ClimateHub.SharedKernel.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct BufferTankId(Guid Value)
{
    public static BufferTankId New() => new(Guid.NewGuid());
    public static BufferTankId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum ChargeStatus { Idle, Charging, Discharging, Satisfied, Unavailable, Faulted }

public class BufferTank : Entity<BufferTankId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public double VolumeLiters { get; private set; }
    public double MinimumTemperatureC { get; private set; }
    public double MaximumTemperatureC { get; private set; }
    public double TargetTemperatureC { get; private set; }
    public double? CurrentTopTemperatureC { get; private set; }
    public double? CurrentBottomTemperatureC { get; private set; }
    public double? EstimatedStoredEnergyKwh { get; private set; }
    public double? StateOfChargePct { get; private set; }
    public ChargeStatus ChargeStatus { get; private set; }
    public string SensorQuality { get; private set; } = "Nominal";
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private BufferTank() { }

    public BufferTank(EngineeringSystemId engId, string name, double volumeLiters,
        double minTemp = 20, double maxTemp = 95, double targetTemp = 50)
    {
        Id = BufferTankId.New(); EngineeringSystemId = engId; Name = name;
        VolumeLiters = volumeLiters; MinimumTemperatureC = minTemp;
        MaximumTemperatureC = maxTemp; TargetTemperatureC = targetTemp;
        ChargeStatus = ChargeStatus.Idle;
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public void UpdateTemperatures(double? top, double? bottom, double? energyKwh = null, double? socPct = null)
    {
        if (top.HasValue) CurrentTopTemperatureC = top;
        if (bottom.HasValue) CurrentBottomTemperatureC = bottom;
        if (energyKwh.HasValue) EstimatedStoredEnergyKwh = energyKwh;
        if (socPct.HasValue) StateOfChargePct = socPct;
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void SetChargeStatus(ChargeStatus status) { ChargeStatus = status; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetTargetTemperature(double temp) { TargetTemperatureC = Math.Clamp(temp, MinimumTemperatureC, MaximumTemperatureC); UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetSensorQuality(string quality) { SensorQuality = quality; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
}