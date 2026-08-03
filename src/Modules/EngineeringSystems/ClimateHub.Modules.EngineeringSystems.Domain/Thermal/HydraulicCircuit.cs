using ClimateHub.SharedKernel.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct HydraulicCircuitId(Guid Value)
{
    public static HydraulicCircuitId New() => new(Guid.NewGuid());
    public static HydraulicCircuitId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public class HydraulicCircuit : Entity<HydraulicCircuitId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public HydraulicCircuitType CircuitType { get; private init; }
    public LifecycleStatus LifecycleStatus { get; private set; }
    public int Priority { get; private set; }
    public double DesignFlowM3h { get; private set; }
    public double MinimumFlowM3h { get; private set; }
    public double MaximumFlowM3h { get; private set; }
    public double MinimumSupplyTemperatureC { get; private set; }
    public double MaximumSupplyTemperatureC { get; private set; }
    public double? CurrentSupplyTemperatureC { get; private set; }
    public double? CurrentReturnTemperatureC { get; private set; }
    public double? CurrentFlowM3h { get; private set; }
    public double? TargetSupplyTemperatureC { get; private set; }
    public Guid? PumpBindingId { get; private set; }
    public Guid? MixingUnitId { get; private set; }
    public string? ResourceCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private HydraulicCircuit() { }

    public HydraulicCircuit(EngineeringSystemId engId, string name, HydraulicCircuitType circuitType,
        double designFlow = 1.5, double minFlow = 0.3, double maxFlow = 2.0,
        double minSupplyTemp = 25, double maxSupplyTemp = 60, int priority = 100)
    {
        Id = HydraulicCircuitId.New(); EngineeringSystemId = engId; Name = name;
        CircuitType = circuitType; LifecycleStatus = LifecycleStatus.Active; Priority = priority;
        DesignFlowM3h = designFlow; MinimumFlowM3h = minFlow; MaximumFlowM3h = maxFlow;
        MinimumSupplyTemperatureC = minSupplyTemp; MaximumSupplyTemperatureC = maxSupplyTemp;
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public void UpdateCurrentState(double? supplyTemp = null, double? returnTemp = null, double? flow = null)
    {
        if (supplyTemp.HasValue) CurrentSupplyTemperatureC = supplyTemp;
        if (returnTemp.HasValue) CurrentReturnTemperatureC = returnTemp;
        if (flow.HasValue) CurrentFlowM3h = flow;
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void SetTargetSupply(double target) { TargetSupplyTemperatureC = target; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetPumpBinding(Guid bindingId) { PumpBindingId = bindingId; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetMixingUnit(Guid unitId) { MixingUnitId = unitId; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Disable() { LifecycleStatus = LifecycleStatus.Disabled; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Activate() { LifecycleStatus = LifecycleStatus.Active; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
}