using ClimateHub.SharedKernel.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct DomesticHotWaterSystemId(Guid Value)
{
    public static DomesticHotWaterSystemId New() => new(Guid.NewGuid());
    public static DomesticHotWaterSystemId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum DhwStatus { Idle, Heating, Satisfied, LegionellaCycle, Faulted }

public class DomesticHotWaterSystem : Entity<DomesticHotWaterSystemId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public double StorageVolumeLiters { get; private set; }
    public double TargetTemperatureC { get; private set; }
    public double MinimumTemperatureC { get; private set; }
    public double MaximumTemperatureC { get; private set; }
    public double? CurrentTemperatureC { get; private set; }
    public double LegionellaProtectionTemperatureC { get; private set; }
    public bool LegionellaCycleEnabled { get; private set; }
    public string PriorityMode { get; private set; } = "DhwPriority";
    public HydraulicCircuitId? HeatingCircuitId { get; private set; }
    public Guid? CirculationPumpBindingId { get; private set; }
    public DhwStatus Status { get; private set; }
    public DateTimeOffset? LastSatisfiedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private DomesticHotWaterSystem() { }

    public DomesticHotWaterSystem(EngineeringSystemId engId, double storageVolumeLiters,
        double targetTemp = 55, double minTemp = 10, double maxTemp = 75,
        double legionellaTemp = 65, bool legionellaEnabled = true,
        string priorityMode = "DhwPriority", Guid? circuitId = null)
    {
        Id = DomesticHotWaterSystemId.New(); EngineeringSystemId = engId;
        StorageVolumeLiters = storageVolumeLiters;
        TargetTemperatureC = targetTemp; MinimumTemperatureC = minTemp; MaximumTemperatureC = maxTemp;
        LegionellaProtectionTemperatureC = legionellaTemp; LegionellaCycleEnabled = legionellaEnabled;
        PriorityMode = priorityMode; Status = DhwStatus.Idle;
        if (circuitId.HasValue) HeatingCircuitId = HydraulicCircuitId.From(circuitId.Value);
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public void UpdateCurrentTemperature(double temp) { CurrentTemperatureC = temp; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetTargetTemperature(double temp) { TargetTemperatureC = Math.Clamp(temp, MinimumTemperatureC, MaximumTemperatureC); UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetPriorityMode(string mode) { PriorityMode = mode; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void StartHeating() { Status = DhwStatus.Heating; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Satisfy() { Status = DhwStatus.Satisfied; LastSatisfiedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetIdle() { Status = DhwStatus.Idle; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void StartLegionellaCycle() { Status = DhwStatus.LegionellaCycle; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void CompleteLegionellaCycle() { Status = DhwStatus.Satisfied; LastSatisfiedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Fail() { Status = DhwStatus.Faulted; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetCirculationPumpBinding(Guid bindingId) { CirculationPumpBindingId = bindingId; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
}