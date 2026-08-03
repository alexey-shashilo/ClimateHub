using ClimateHub.SharedKernel.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct MixingUnitId(Guid Value)
{
    public static MixingUnitId New() => new(Guid.NewGuid());
    public static MixingUnitId From(Guid value) => new(value);
}

public class MixingUnit : Entity<MixingUnitId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public HydraulicCircuitId? CircuitId { get; private set; }
    public LifecycleStatus LifecycleStatus { get; private set; }
    public Guid? ValveBindingId { get; private set; }
    public Guid? SupplySensorBindingId { get; private set; }
    public Guid? ReturnSensorBindingId { get; private set; }
    public double MinimumValvePositionPct { get; private set; }
    public double MaximumValvePositionPct { get; private set; }
    public double? TargetSupplyTemperatureC { get; private set; }
    public double? CurrentSupplyTemperatureC { get; private set; }
    public double? CurrentValvePositionPct { get; private set; }
    public string ControlMode { get; private set; } = "WeatherCompensated";
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private MixingUnit() { }

    public MixingUnit(EngineeringSystemId engId, string name,
        double minValvePct = 0, double maxValvePct = 100,
        Guid? valveBindingId = null, Guid? circuitId = null)
    {
        Id = MixingUnitId.New(); EngineeringSystemId = engId; Name = name;
        LifecycleStatus = LifecycleStatus.Active;
        MinimumValvePositionPct = minValvePct; MaximumValvePositionPct = maxValvePct;
        ValveBindingId = valveBindingId;
        if (circuitId.HasValue) CircuitId = HydraulicCircuitId.From(circuitId.Value);
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public void UpdateState(double? supplyTemp = null, double? valvePos = null)
    {
        if (supplyTemp.HasValue) CurrentSupplyTemperatureC = supplyTemp;
        if (valvePos.HasValue) CurrentValvePositionPct = valvePos;
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void SetTarget(double target) { TargetSupplyTemperatureC = target; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Disable() { LifecycleStatus = LifecycleStatus.Disabled; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
}