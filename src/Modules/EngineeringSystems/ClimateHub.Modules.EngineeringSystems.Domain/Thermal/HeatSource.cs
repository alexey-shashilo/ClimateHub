using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct HeatSourceId(Guid Value)
{
    public static HeatSourceId New() => new(Guid.NewGuid());
    public static HeatSourceId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum HeatSourceRuntimeState { Off, Starting, Running, Stopping, Cooldown, Defrost, Faulted, Unavailable }

public class HeatSource : Entity<HeatSourceId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public HeatSourceType SourceType { get; private init; }
    public LifecycleStatus LifecycleStatus { get; private set; }
    public bool IsPrimary { get; private set; }
    public int Priority { get; private set; }
    public double MinimumPowerKw { get; private set; }
    public double MaximumPowerKw { get; private set; }
    public int MinimumRuntimeMinutes { get; private set; }
    public int MinimumOffTimeMinutes { get; private set; }
    public int StartupDurationSeconds { get; private set; }
    public int ShutdownDurationSeconds { get; private set; }
    public double Efficiency { get; private set; }
    public double NominalCop { get; private set; }
    public double MaximumSupplyTemperatureC { get; private set; }
    public double? CurrentOutputKw { get; private set; }
    public double? CurrentOutputPct { get; private set; }
    public HeatSourceRuntimeState RuntimeState { get; private set; }
    public DateTimeOffset? LastStartedAt { get; private set; }
    public DateTimeOffset? LastStoppedAt { get; private set; }
    public DateTimeOffset? CooldownUntil { get; private set; }
    public string? DefrostState { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private HeatSource() { }

    public HeatSource(EngineeringSystemId engId, string name, HeatSourceType sourceType,
        double maxPowerKw, double minPowerKw = 0, bool isPrimary = false, int priority = 100,
        int startupSec = 30, int shutdownSec = 30, int minRuntimeMin = 5, int minOffMin = 3,
        double efficiency = 1, double cop = 1, double maxSupplyTemp = 90)
    {
        Id = HeatSourceId.New(); EngineeringSystemId = engId; Name = name;
        SourceType = sourceType; LifecycleStatus = LifecycleStatus.Active;
        IsPrimary = isPrimary; Priority = priority;
        MaximumPowerKw = maxPowerKw; MinimumPowerKw = minPowerKw;
        StartupDurationSeconds = startupSec; ShutdownDurationSeconds = shutdownSec;
        MinimumRuntimeMinutes = minRuntimeMin; MinimumOffTimeMinutes = minOffMin;
        Efficiency = efficiency; NominalCop = cop; MaximumSupplyTemperatureC = maxSupplyTemp;
        RuntimeState = HeatSourceRuntimeState.Off;
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public void RequestStart() => Transition(HeatSourceRuntimeState.Starting);
    public void MarkRunning() { Transition(HeatSourceRuntimeState.Running); LastStartedAt = DateTimeOffset.UtcNow; }
    public void RequestStop() => Transition(HeatSourceRuntimeState.Stopping);
    public void MarkStopped() { Transition(HeatSourceRuntimeState.Off); LastStoppedAt = DateTimeOffset.UtcNow; CooldownUntil = DateTimeOffset.UtcNow.AddMinutes(CooldownRemainingMinutes()); }
    public void EnterCooldown() { Transition(HeatSourceRuntimeState.Cooldown); CooldownUntil = DateTimeOffset.UtcNow.AddMinutes(CooldownRemainingMinutes()); }
    public void ExitCooldown() { Transition(HeatSourceRuntimeState.Off); CooldownUntil = null; }
    public void BeginDefrost() { DefrostState = "Active"; Transition(HeatSourceRuntimeState.Defrost); }
    public void CompleteDefrost() { DefrostState = "Completed"; Transition(HeatSourceRuntimeState.Running); }
    public void Fail(string? code = null) { FailureCode = code; Transition(HeatSourceRuntimeState.Faulted); }
    public void Recover() { FailureCode = null; Transition(HeatSourceRuntimeState.Off); }
    public void Disable() { LifecycleStatus = LifecycleStatus.Disabled; UpdatedAt = DateTimeOffset.UtcNow; Version++; }

    public bool CanStart() => RuntimeState == HeatSourceRuntimeState.Off
        && (CooldownUntil is null || CooldownUntil <= DateTimeOffset.UtcNow)
        && LifecycleStatus == LifecycleStatus.Active;
    public bool IsMinimumRuntimeActive() => LastStartedAt.HasValue
        && DateTimeOffset.UtcNow < LastStartedAt.Value.AddMinutes(MinimumRuntimeMinutes);
    public bool IsMinimumOffTimeActive() => LastStoppedAt.HasValue
        && DateTimeOffset.UtcNow < LastStoppedAt.Value.AddMinutes(MinimumOffTimeMinutes);

    private double CooldownRemainingMinutes() => SourceType switch
    {
        HeatSourceType.GasBoiler => 3,
        HeatSourceType.PelletBoiler => 10,
        HeatSourceType.HeatPump => 5,
        _ => 2
    };

    private void Transition(HeatSourceRuntimeState to)
    {
        if (!IsValid(RuntimeState, to))
            throw new InvalidOperationException($"Invalid HeatSource transition: {RuntimeState} \u2192 {to}");
        RuntimeState = to; UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    private static bool IsValid(HeatSourceRuntimeState from, HeatSourceRuntimeState to) => (from, to) switch
    {
        (HeatSourceRuntimeState.Off, HeatSourceRuntimeState.Starting) => true,
        (HeatSourceRuntimeState.Starting, HeatSourceRuntimeState.Running) => true,
        (HeatSourceRuntimeState.Starting, HeatSourceRuntimeState.Faulted) => true,
        (HeatSourceRuntimeState.Running, HeatSourceRuntimeState.Stopping) => true,
        (HeatSourceRuntimeState.Running, HeatSourceRuntimeState.Defrost) => true,
        (HeatSourceRuntimeState.Running, HeatSourceRuntimeState.Faulted) => true,
        (HeatSourceRuntimeState.Stopping, HeatSourceRuntimeState.Off) => true,
        (HeatSourceRuntimeState.Stopping, HeatSourceRuntimeState.Cooldown) => true,
        (HeatSourceRuntimeState.Stopping, HeatSourceRuntimeState.Faulted) => true,
        (HeatSourceRuntimeState.Cooldown, HeatSourceRuntimeState.Off) => true,
        (HeatSourceRuntimeState.Defrost, HeatSourceRuntimeState.Running) => true,
        (HeatSourceRuntimeState.Defrost, HeatSourceRuntimeState.Faulted) => true,
        (HeatSourceRuntimeState.Faulted, HeatSourceRuntimeState.Off) => true,
        _ => false
    };
}
