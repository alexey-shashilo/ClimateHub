using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain;

public enum HeatSourceType
{
    ElectricBoiler,
    GasBoiler,
    PelletBoiler,
    HeatPump,
    DistrictHeating,
    SolarThermal
}

public enum CoolingSourceType
{
    Chiller,
    HeatPumpCooling,
    FanCoilCooling,
    DryCooler,
    FreeCooling
}

public enum HydraulicCircuitType
{
    Radiators,
    FloorHeating,
    FanCoil,
    DHW,
    BufferTank
}

public enum MixingUnitMode
{
    Fixed,
    WeatherCompensated,
    RoomCompensated,
    Cascade
}

public enum PumpStatus
{
    Off,
    Running,
    Standby,
    Faulted
}

public enum HeatSourceStatus
{
    Off,
    Starting,
    Running,
    Modulating,
    Cooldown,
    Faulted,
    Defrost,
    MaintenanceRequired
}

public class ThermalSystemConfiguration
{
    public Guid Id { get; private set; }
    public EngineeringSystemId EngineeringSystemId { get; private set; }

    // Heating
    public double DesignSupplyTemperature { get; private set; }
    public double DesignReturnTemperature { get; private set; }
    public double MinimumSupplyTemperature { get; private set; }
    public double MaximumSupplyTemperature { get; private set; }
    public double DesignHeatLoad { get; private set; }
    public double SystemThermalMass { get; private set; }

    // Weather compensation
    public bool WeatherCompensationEnabled { get; private set; }
    public double WeatherCompensationMinOutdoor { get; private set; }
    public double WeatherCompensationMaxOutdoor { get; private set; }
    public double WeatherCompensationMinSupply { get; private set; }
    public double WeatherCompensationMaxSupply { get; private set; }
    public double WeatherCompensationSlope { get; private set; }
    public double WeatherCompensationParallelShift { get; private set; }

    // Freeze protection
    public bool FreezeProtectionEnabled { get; private set; }
    public double FreezeProtectionTemperature { get; private set; }
    public double FreezeProtectionSupplyTemperature { get; private set; }

    // Safety limits
    public double MaximumReturnTemperature { get; private set; }
    public double MaximumTempRiseRate { get; private set; }
    public bool HasBufferTank { get; private set; }
    public double BufferTankVolume { get; private set; }
    public bool HasDHW { get; private set; }

    public uint Version { get; private set; }

    private ThermalSystemConfiguration() { }

    public ThermalSystemConfiguration(Guid engineeringSystemId,
        double designSupplyTemp = 55, double designReturnTemp = 45,
        double minSupplyTemp = 20, double maxSupplyTemp = 90,
        double designHeatLoad = 15000, double systemThermalMass = 5000,
        bool weatherCompensationEnabled = true,
        double minOutdoor = -30, double maxOutdoor = 20,
        double minSupply = 20, double maxSupply = 60,
        double slope = 1.2, double parallelShift = 0,
        bool freezeProtection = true, double freezeTemp = 5,
        double freezeSupplyTemp = 10,
        double maxReturnTemp = 65, double maxTempRiseRate = 5,
        bool hasBufferTank = false, double bufferTankVolume = 0,
        bool hasDHW = false)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = EngineeringSystemId.From(engineeringSystemId);
        DesignSupplyTemperature = designSupplyTemp;
        DesignReturnTemperature = designReturnTemp;
        MinimumSupplyTemperature = minSupplyTemp;
        MaximumSupplyTemperature = maxSupplyTemp;
        DesignHeatLoad = designHeatLoad;
        SystemThermalMass = systemThermalMass;
        WeatherCompensationEnabled = weatherCompensationEnabled;
        WeatherCompensationMinOutdoor = minOutdoor;
        WeatherCompensationMaxOutdoor = maxOutdoor;
        WeatherCompensationMinSupply = minSupply;
        WeatherCompensationMaxSupply = maxSupply;
        WeatherCompensationSlope = slope;
        WeatherCompensationParallelShift = parallelShift;
        FreezeProtectionEnabled = freezeProtection;
        FreezeProtectionTemperature = freezeTemp;
        FreezeProtectionSupplyTemperature = freezeSupplyTemp;
        MaximumReturnTemperature = maxReturnTemp;
        MaximumTempRiseRate = maxTempRiseRate;
        HasBufferTank = hasBufferTank;
        BufferTankVolume = bufferTankVolume;
        HasDHW = hasDHW;
        Version = 1;
    }

    public void UpdateWeatherCompensation(double slope, double parallelShift,
        double minSupply, double maxSupply)
    {
        WeatherCompensationSlope = slope;
        WeatherCompensationParallelShift = parallelShift;
        WeatherCompensationMinSupply = minSupply;
        WeatherCompensationMaxSupply = maxSupply;
        Version++;
    }
}

public class ThermalZoneConfiguration
{
    public Guid Id { get; private set; }
    public Guid ZoneId { get; private set; }
    public RoomId RoomId { get; private set; }
    public double DesignHeatLoad { get; private set; }
    public double ThermalMass { get; private set; }
    public double HeatLossCoefficient { get; private set; }
    public double MinimumSupplyTemperature { get; private set; }
    public double MaximumSupplyTemperature { get; private set; }
    public Guid? ActuatorBindingId { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private ThermalZoneConfiguration() { RoomId = RoomId.From(Guid.Empty); }

    public ThermalZoneConfiguration(Guid zoneId, RoomId roomId,
        double designHeatLoad = 1500, double thermalMass = 2000,
        double heatLossCoefficient = 50,
        double minSupplyTemp = 25, double maxSupplyTemp = 55,
        Guid? actuatorBindingId = null)
    {
        Id = Guid.NewGuid(); ZoneId = zoneId; RoomId = roomId;
        DesignHeatLoad = designHeatLoad; ThermalMass = thermalMass;
        HeatLossCoefficient = heatLossCoefficient;
        MinimumSupplyTemperature = minSupplyTemp;
        MaximumSupplyTemperature = maxSupplyTemp;
        ActuatorBindingId = actuatorBindingId;
        Enabled = true; Version = 1;
    }
}

public class HeatSourceConfiguration
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public HeatSourceType SourceType { get; private set; }
    public double MaximumPower { get; private set; }
    public double MinimumPower { get; private set; }
    public double CopAtDesign { get; private set; }
    public double Efficiency { get; private set; }
    public int StartupTimeSeconds { get; private set; }
    public int MinimumRuntimeMinutes { get; private set; }
    public int CooldownMinutes { get; private set; }
    public double MaximumSupplyTemperature { get; private set; }
    public bool IsPrimary { get; private set; }
    public int Priority { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private HeatSourceConfiguration() { }

    public HeatSourceConfiguration(Guid engineeringSystemId, HeatSourceType sourceType,
        double maxPower, double minPower = 0,
        double copAtDesign = 1, double efficiency = 1,
        int startupTimeSec = 30, int minRuntimeMin = 5, int cooldownMin = 3,
        double maxSupplyTemp = 90, bool isPrimary = false, int priority = 100)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engineeringSystemId;
        SourceType = sourceType; MaximumPower = maxPower;
        MinimumPower = minPower; CopAtDesign = copAtDesign;
        Efficiency = efficiency; StartupTimeSeconds = startupTimeSec;
        MinimumRuntimeMinutes = minRuntimeMin; CooldownMinutes = cooldownMin;
        MaximumSupplyTemperature = maxSupplyTemp;
        IsPrimary = isPrimary; Priority = priority; Enabled = true;
        Version = 1;
    }

    public void SetEnabled(bool enabled) { Enabled = enabled; Version++; }
    public void SetPriority(int priority) { Priority = priority; Version++; }
}

public class HydraulicCircuitConfiguration
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public HydraulicCircuitType CircuitType { get; private set; }
    public double DesignSupplyTemperature { get; private set; }
    public double DesignReturnTemperature { get; private set; }
    public double DesignFlow { get; private set; }
    public double DesignHeatOutput { get; private set; }
    public Guid? PumpBindingId { get; private set; }
    public Guid? MixingValveBindingId { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private HydraulicCircuitConfiguration() { }

    public HydraulicCircuitConfiguration(Guid engineeringSystemId, HydraulicCircuitType circuitType,
        double designSupplyTemp, double designReturnTemp,
        double designFlow, double designHeatOutput,
        Guid? pumpBindingId = null, Guid? mixingValveBindingId = null)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engineeringSystemId;
        CircuitType = circuitType;
        DesignSupplyTemperature = designSupplyTemp;
        DesignReturnTemperature = designReturnTemp;
        DesignFlow = designFlow; DesignHeatOutput = designHeatOutput;
        PumpBindingId = pumpBindingId;
        MixingValveBindingId = mixingValveBindingId;
        Enabled = true; Version = 1;
    }
}
