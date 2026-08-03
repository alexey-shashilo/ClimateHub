using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain;

public enum VentilationSystemMode
{
    SupplyOnly,
    ExhaustOnly,
    BalancedSupplyExhaust
}

public class VentilationSystemConfiguration
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public VentilationSystemMode SystemMode { get; private set; }
    public double DesignSupplyAirflow { get; private set; }
    public double DesignExhaustAirflow { get; private set; }
    public double MinimumSupplyAirflow { get; private set; }
    public double MinimumExhaustAirflow { get; private set; }
    public double MaximumImbalancePct { get; private set; }
    public bool HasHeatRecovery { get; private set; }
    public bool HasSupplyHeater { get; private set; }
    public bool HasSupplyAirTemperatureSensor { get; private set; }
    public bool HasOutdoorTemperatureSensor { get; private set; }
    public bool FrostProtectionEnabled { get; private set; }
    public double FrostProtectionTemperature { get; private set; }
    public double MinimumSupplyAirTemperature { get; private set; }
    public double MaximumSupplyAirTemperature { get; private set; }
    public double DefaultHeatRecoveryEfficiency { get; private set; }
    public uint Version { get; private set; }

    private VentilationSystemConfiguration() { SystemMode = VentilationSystemMode.BalancedSupplyExhaust; }

    public VentilationSystemConfiguration(Guid engineeringSystemId,
        VentilationSystemMode systemMode = VentilationSystemMode.BalancedSupplyExhaust,
        double designSupplyAirflow = 300, double designExhaustAirflow = 300,
        double minimumSupplyAirflow = 30, double minimumExhaustAirflow = 30,
        double maximumImbalancePct = 10,
        bool hasHeatRecovery = true, bool hasSupplyHeater = true,
        bool hasSupplyAirTempSensor = true, bool hasOutdoorTempSensor = true,
        bool frostProtectionEnabled = true, double frostProtectionTemperature = -15,
        double minSupplyAirTemp = 16, double maxSupplyAirTemp = 35,
        double defaultHeatRecoveryEfficiency = 0.75)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engineeringSystemId;
        SystemMode = systemMode; DesignSupplyAirflow = designSupplyAirflow;
        DesignExhaustAirflow = designExhaustAirflow;
        MinimumSupplyAirflow = minimumSupplyAirflow;
        MinimumExhaustAirflow = minimumExhaustAirflow;
        MaximumImbalancePct = maximumImbalancePct;
        HasHeatRecovery = hasHeatRecovery; HasSupplyHeater = hasSupplyHeater;
        HasSupplyAirTemperatureSensor = hasSupplyAirTempSensor;
        HasOutdoorTemperatureSensor = hasOutdoorTempSensor;
        FrostProtectionEnabled = frostProtectionEnabled;
        FrostProtectionTemperature = frostProtectionTemperature;
        MinimumSupplyAirTemperature = minSupplyAirTemp;
        MaximumSupplyAirTemperature = maxSupplyAirTemp;
        DefaultHeatRecoveryEfficiency = defaultHeatRecoveryEfficiency;
        Version = 1;
    }

    public void SetSystemMode(VentilationSystemMode mode) { SystemMode = mode; Version++; }
    public void SetFrostProtection(bool enabled, double temperature) { FrostProtectionEnabled = enabled; FrostProtectionTemperature = temperature; Version++; }
    public void SetSupplyAirTemperatureLimits(double min, double max) { MinimumSupplyAirTemperature = min; MaximumSupplyAirTemperature = max; Version++; }
    public void SetAirflowLimits(double minSupply, double minExhaust) { MinimumSupplyAirflow = minSupply; MinimumExhaustAirflow = minExhaust; Version++; }
}

public class VentilationZoneRoomConfiguration
{
    public Guid Id { get; private set; }
    public Guid ZoneId { get; private set; }
    public RoomId? RoomId { get; private set; }
    public double DesignAirflow { get; private set; }
    public double MinimumAirflow { get; private set; }
    public double MaximumAirflow { get; private set; }
    public double CoverageWeight { get; private set; }
    public int Priority { get; private set; }
    public Guid? SupplyDamperBindingId { get; private set; }
    public Guid? ExhaustDamperBindingId { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private VentilationZoneRoomConfiguration() { }

    public VentilationZoneRoomConfiguration(Guid zoneId, RoomId roomId,
        double designAirflow = 90, double minimumAirflow = 30, double maximumAirflow = 120,
        double coverageWeight = 1.0, int priority = 100,
        Guid? supplyDamperBindingId = null, Guid? exhaustDamperBindingId = null)
    {
        Id = Guid.NewGuid(); ZoneId = zoneId; RoomId = roomId;
        DesignAirflow = designAirflow; MinimumAirflow = minimumAirflow;
        MaximumAirflow = maximumAirflow; CoverageWeight = coverageWeight;
        Priority = priority; SupplyDamperBindingId = supplyDamperBindingId;
        ExhaustDamperBindingId = exhaustDamperBindingId;
        Enabled = true; Version = 1;
    }
}