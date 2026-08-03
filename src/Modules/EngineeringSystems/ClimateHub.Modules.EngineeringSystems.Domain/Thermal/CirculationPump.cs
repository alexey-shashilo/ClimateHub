using ClimateHub.SharedKernel.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct CirculationPumpId(Guid Value)
{
    public static CirculationPumpId New() => new(Guid.NewGuid());
    public static CirculationPumpId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public class CirculationPump : Entity<CirculationPumpId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public HydraulicCircuitId? CircuitId { get; private set; }
    public Guid? DeviceBindingId { get; private set; }
    public PumpStatus Status { get; private set; }
    public double MinimumSpeedPct { get; private set; }
    public double MaximumSpeedPct { get; private set; }
    public double? CurrentSpeedPct { get; private set; }
    public double? TargetSpeedPct { get; private set; }
    public double? CurrentFlowM3h { get; private set; }
    public bool DryRunProtectionEnabled { get; private set; }
    public DateTimeOffset? LastStartedAt { get; private set; }
    public DateTimeOffset? LastStoppedAt { get; private set; }
    public double RuntimeHours { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private CirculationPump() { }

    public CirculationPump(EngineeringSystemId engId, string name,
        double minSpeedPct = 20, double maxSpeedPct = 100,
        bool dryRunProtection = true, Guid? circuitId = null, Guid? deviceBindingId = null)
    {
        Id = CirculationPumpId.New(); EngineeringSystemId = engId;
        if (circuitId.HasValue) CircuitId = HydraulicCircuitId.From(circuitId.Value);
        DeviceBindingId = deviceBindingId;
        Status = PumpStatus.Off;
        MinimumSpeedPct = minSpeedPct; MaximumSpeedPct = maxSpeedPct;
        DryRunProtectionEnabled = dryRunProtection;
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Clarity")]
    private string _name = string.Empty;
    public string Name { get => _name; private init => _name = value; }

    public void Start() { Status = PumpStatus.Running; LastStartedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Stop() { Status = PumpStatus.Off; LastStoppedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetStandby() { Status = PumpStatus.Standby; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Fail() { Status = PumpStatus.Faulted; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Recover() { Status = PumpStatus.Off; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetTargetSpeed(double pct) { TargetSpeedPct = Math.Clamp(pct, MinimumSpeedPct, MaximumSpeedPct); UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void UpdateCurrentState(double? speedPct = null, double? flowM3h = null)
    {
        if (speedPct.HasValue) CurrentSpeedPct = speedPct;
        if (flowM3h.HasValue) CurrentFlowM3h = flowM3h;
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }
    public void AddRuntimeHours(double hours) { RuntimeHours += hours; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
}