using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Humidification;

public enum HumidifierType
{
    SteamElectrode, SteamResistive, SteamGas,
    Ultrasonic, HighPressureNozzle, EvaporativeMedia,
    RotaryDisc, CompressedAirNozzle, CentralSteamGenerator
}

public enum HumidificationMode { Steam, Adiabatic, Hybrid }
public enum CondensationRisk { None, Low, Medium, High, Critical }
public enum SanitaryStatus { Ok, FlushDue, DrainDue, SterilizationDue, Overdue }

public class HumidificationSystemConfiguration
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public HumidificationMode Mode { get; private set; }
    public double DesignCapacityKgH { get; private set; }
    public double MaximumCapacityKgH { get; private set; }
    public double MinimumCapacityKgH { get; private set; }
    public double TargetRhPercent { get; private set; }
    public double MaximumRhPercent { get; private set; }
    public double MinimumSupplyAirTemperatureC { get; private set; }
    public double MaximumSupplyAirTemperatureC { get; private set; }
    public bool HasWaterTreatment { get; private set; }
    public bool HasSteamGenerator { get; private set; }
    public bool HasUVSterilization { get; private set; }
    public bool CondensationProtectionEnabled { get; private set; }
    public double MaximumSurfaceDewPointDeltaC { get; private set; }
    public bool SanitaryCycleEnabled { get; private set; }
    public int FlushIntervalHours { get; private set; }
    public int DrainIntervalHours { get; private set; }
    public int SterilizationIntervalDays { get; private set; }
    public double StandingWaterTimeoutMinutes { get; private set; }
    public double LegionellaProtectionTemperatureC { get; private set; }
    public uint Version { get; private set; }

    private HumidificationSystemConfiguration() { }

    public HumidificationSystemConfiguration(Guid engineeringSystemId,
        HumidificationMode mode = HumidificationMode.Steam,
        double designCapacity = 50, double maxCapacity = 100, double minCapacity = 5,
        double targetRh = 50, double maxRh = 60,
        double minSupplyAirTemp = 12, double maxSupplyAirTemp = 35,
        bool hasWaterTreatment = true, bool hasSteamGen = true,
        bool hasUV = true, bool condensationProtection = true,
        double dewPointDelta = 3, bool sanitaryEnabled = true,
        int flushHours = 24, int drainHours = 48, int sterilDays = 30,
        double standingWaterTimeout = 72, double legionellaTemp = 65)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engineeringSystemId;
        Mode = mode; DesignCapacityKgH = designCapacity;
        MaximumCapacityKgH = maxCapacity; MinimumCapacityKgH = minCapacity;
        TargetRhPercent = targetRh; MaximumRhPercent = maxRh;
        MinimumSupplyAirTemperatureC = minSupplyAirTemp;
        MaximumSupplyAirTemperatureC = maxSupplyAirTemp;
        HasWaterTreatment = hasWaterTreatment; HasSteamGenerator = hasSteamGen;
        HasUVSterilization = hasUV;
        CondensationProtectionEnabled = condensationProtection;
        MaximumSurfaceDewPointDeltaC = dewPointDelta;
        SanitaryCycleEnabled = sanitaryEnabled;
        FlushIntervalHours = flushHours; DrainIntervalHours = drainHours;
        SterilizationIntervalDays = sterilDays;
        StandingWaterTimeoutMinutes = standingWaterTimeout;
        LegionellaProtectionTemperatureC = legionellaTemp;
        Version = 1;
    }
}

public class HumidificationZone
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public double DesignCapacityKgH { get; private set; }
    public double MinimumRhPercent { get; private set; }
    public double MaximumRhPercent { get; private set; }
    public int Priority { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public uint Version { get; private set; }

    private readonly List<HumidificationZoneRoom> _rooms = new();
    public IReadOnlyCollection<HumidificationZoneRoom> Rooms => _rooms.AsReadOnly();

    private HumidificationZone() { }

    public HumidificationZone(Guid engId, string name, double designCapacity = 20,
        double minRh = 30, double maxRh = 60, int priority = 100)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engId; Name = name;
        DesignCapacityKgH = designCapacity; MinimumRhPercent = minRh;
        MaximumRhPercent = maxRh; Priority = priority; Enabled = true;
        CreatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public void AddRoom(HumidificationZoneRoom room) { _rooms.Add(room); Version++; }
    public void SetEnabled(bool enabled) { Enabled = enabled; Version++; }
}

public class HumidificationZoneRoom
{
    public Guid Id { get; private set; }
    public Guid ZoneId { get; set; }
    public RoomId RoomId { get; private init; }
    public double Weight { get; private set; }
    public double DesignCapacityKgH { get; private set; }
    public double MinimumRhPercent { get; private set; }
    public double MaximumRhPercent { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private HumidificationZoneRoom() { RoomId = RoomId.From(Guid.Empty); }

    public HumidificationZoneRoom(RoomId roomId, double weight = 1.0,
        double designCapacity = 5, double minRh = 30, double maxRh = 60)
    {
        Id = Guid.NewGuid(); RoomId = roomId; Weight = weight;
        DesignCapacityKgH = designCapacity; MinimumRhPercent = minRh;
        MaximumRhPercent = maxRh; Enabled = true; Version = 1;
    }

    public void SetEnabled(bool enabled) { Enabled = enabled; Version++; }
}

public class HumidificationDemand
{
    public Guid Id { get; private set; }
    public string? ClimatePlanId { get; private set; }
    public Guid EngineeringSystemId { get; private init; }
    public RoomId RoomId { get; private init; }
    public double CurrentRhPercent { get; private set; }
    public double TargetRhPercent { get; private set; }
    public double DewPointC { get; private set; }
    public double RequiredCapacityKgH { get; private set; }
    public string Severity { get; private set; } = "Medium";
    public string Status { get; private set; } = "Active";
    public int Priority { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private HumidificationDemand() { RoomId = RoomId.From(Guid.Empty); }

    public HumidificationDemand(Guid engId, RoomId roomId,
        double currentRh, double targetRh, double dewPointC,
        double requiredCapacity, string severity = "Medium", int priority = 100)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engId; RoomId = roomId;
        CurrentRhPercent = currentRh; TargetRhPercent = targetRh;
        DewPointC = dewPointC; RequiredCapacityKgH = requiredCapacity;
        Severity = severity; Status = "Active"; Priority = priority;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Satisfy() { Status = "Satisfied"; }
    public void Block() { Status = "Blocked"; }
}