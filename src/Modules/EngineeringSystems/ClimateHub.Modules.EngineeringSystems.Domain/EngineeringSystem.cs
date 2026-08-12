using ClimateHub.Modules.EngineeringSystems.Domain.Humidification;
using ClimateHub.Modules.EngineeringSystems.Domain.Lighting;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain;

public readonly record struct EngineeringSystemId(Guid Value)
{
    public static EngineeringSystemId New() => new(Guid.NewGuid());
    public static EngineeringSystemId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum LifecycleStatus
{
    Draft,
    Commissioning,
    Active,
    Maintenance,
    Disabled,
    Decommissioned
}

public enum OperationalStatus
{
    Unknown,
    Available,
    Degraded,
    Unavailable,
    Faulted
}

public enum SystemType
{
    SupplyVentilation,
    ExhaustVentilation,
    Ventilation,
    Heating,
    Cooling,
    Humidification,
    Dehumidification,
    Lighting,
    AirQuality,
    Curtains,
    Irrigation
}

[Obsolete("Use LifecycleStatus instead")]
public enum SystemStatus { Enabled, Disabled, Faulted, Maintenance, Offline }

public enum SystemControlMode { MonitorOnly, Automatic, Manual, Scheduled, Off }

public static class DeviceRole
{
    public const string SupplyFan = "SupplyFan";
    public const string ExhaustFan = "ExhaustFan";
    public const string SupplyDamper = "SupplyDamper";
    public const string ExhaustDamper = "ExhaustDamper";
    public const string BypassDamper = "BypassDamper";
    public const string Recuperator = "Recuperator";
    public const string SupplyHeater = "SupplyHeater";
    public const string CoolingCoil = "CoolingCoil";
    public const string OutdoorAirTemperatureSensor = "OutdoorAirTemperatureSensor";
    public const string SupplyAirTemperatureSensor = "SupplyAirTemperatureSensor";
    public const string ExhaustAirTemperatureSensor = "ExhaustAirTemperatureSensor";
    public const string AirflowSensor = "AirflowSensor";
    public const string DifferentialPressureSensor = "DifferentialPressureSensor";
    public const string FilterPressureSensor = "FilterPressureSensor";
    public const string FrostProtectionSensor = "FrostProtectionSensor";
    public const string AirQualitySensor = "AirQualitySensor";
    public const string TemperatureSensor = "TemperatureSensor";
    public const string HumiditySensor = "HumiditySensor";
    public const string LightActuator = "LightActuator";
    public const string ShadeActuator = "ShadeActuator";
    public const string HeatSourceBoiler = "HeatSourceBoiler";
    public const string HeatPump = "HeatPump";
    public const string CirculationPump = "CirculationPump";
    public const string MixingValve = "MixingValve";
    public const string RadiatorActuator = "RadiatorActuator";
    public const string FloorHeatingActuator = "FloorHeatingActuator";
    public const string FanCoilUnit = "FanCoilUnit";
    public const string SupplyTemperatureSensor = "SupplyTemperatureSensor";
    public const string ReturnTemperatureSensor = "ReturnTemperatureSensor";
    public const string OutdoorTemperatureSensor = "OutdoorTemperatureSensor";
    public const string BufferTank = "BufferTank";
    public const string ExpansionVessel = "ExpansionVessel";
    public const string DHWCirculationPump = "DHWCirculationPump";
    public const string HeatMeter = "HeatMeter";
    public const string FlowMeter = "FlowMeter";
    public const string SteamGenerator = "SteamGenerator";
    public const string SteamValve = "SteamValve";
    public const string SteamInjector = "SteamInjector";
    public const string HumidifierFan = "HumidifierFan";
    public const string NozzlePump = "NozzlePump";
    public const string NozzleValve = "NozzleValve";
    public const string WaterPump = "WaterPump";
    public const string ROSystem = "ROSystem";
    public const string UVSterilizer = "UVSterilizer";
    public const string ConductivitySensor = "ConductivitySensor";
    public const string WaterTemperatureSensor = "WaterTemperatureSensor";
    public const string WaterPressureSensor = "WaterPressureSensor";
    public const string SteamPressureSensor = "SteamPressureSensor";
    public const string DrainValve = "DrainValve";
    public const string FlushValve = "FlushValve";
    public const string WaterLevelSensor = "WaterLevelSensor";
    public const string HumiditySensorReference = "HumiditySensorReference";
    public const string SupplyAirHumiditySensor = "SupplyAirHumiditySensor";
    public const string ReturnAirHumiditySensor = "ReturnAirHumiditySensor";
    public const string DuctHumiditySensor = "DuctHumiditySensor";
    public const string CondensationSensor = "CondensationSensor";
    public const string DaliGateway = "DaliGateway";
    public const string LightingController = "LightingController";
    public const string Dimmer = "Dimmer";
    public const string RelayModule = "RelayModule";
    public const string RgbController = "RgbController";
    public const string CctController = "CctController";
    public const string OccupancySensor = "OccupancySensor";
    public const string PresenceSensor = "PresenceSensor";
    public const string LuxSensor = "LuxSensor";
    public const string WindowSensor = "WindowSensor";
    public const string BlindMotor = "BlindMotor";
    public const string CurtainMotor = "CurtainMotor";
    public const string RollerMotor = "RollerMotor";
    public const string FacadeScreenMotor = "FacadeScreenMotor";
    public const string RoofWindowMotor = "RoofWindowMotor";
    public const string SunSensor = "SunSensor";
    public const string FacadeTemperatureSensor = "FacadeTemperatureSensor";
    public const string GlassTemperatureSensor = "GlassTemperatureSensor";
    public const string OutdoorBrightnessSensor = "OutdoorBrightnessSensor";
    public const string IndoorBrightnessSensor = "IndoorBrightnessSensor";
    public const string SkyConditionSensor = "SkyConditionSensor";
}

public class EngineeringSystem : Entity<EngineeringSystemId>, IAggregateRoot
{
    public BuildingId BuildingId { get; private init; }
    public string Name { get; private set; }
    public SystemType SystemType { get; private init; }
    [Obsolete("Use Lifecycle instead")]
    public SystemStatus Status { get; private set; }
    public LifecycleStatus Lifecycle { get; private set; }
    public OperationalStatus OperationalStatus { get; private set; }
#pragma warning disable CS0618
    public SystemStatus LegacyStatus { get; private set; }
#pragma warning restore CS0618
    public SystemControlMode ControlMode { get; private set; }
    public int Priority { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private readonly List<SystemCapability> _capabilities = new();
    public IReadOnlyCollection<SystemCapability> Capabilities => _capabilities.AsReadOnly();

    private readonly List<EngineeringResource> _resources = new();
    public IReadOnlyCollection<EngineeringResource> Resources => _resources.AsReadOnly();

    private readonly List<SystemZone> _zones = new();
    public IReadOnlyCollection<SystemZone> Zones => _zones.AsReadOnly();

    private readonly List<EngineeringSystemDeviceBinding> _deviceBindings = new();
    public IReadOnlyCollection<EngineeringSystemDeviceBinding> DeviceBindings => _deviceBindings.AsReadOnly();

    public VentilationSystemConfiguration? VentilationConfiguration { get; private set; }
    public ThermalSystemConfiguration? ThermalConfiguration { get; private set; }
    public HumidificationSystemConfiguration? HumidificationConfiguration { get; private set; }
    // [PROTOTYPE] Lighting is a prototype feature — not yet ready for production.
    // Excluded from production claims, audits, and compliance checks.
    public LightingSystemConfiguration? LightingConfiguration { get; private set; }

    public void SetHumidificationConfiguration(HumidificationSystemConfiguration config)
    {
        HumidificationConfiguration = config;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void SetLightingConfiguration(LightingSystemConfiguration config)
    {
        LightingConfiguration = config;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    private readonly List<HeatSourceConfiguration> _heatSources = new();
    public IReadOnlyCollection<HeatSourceConfiguration> HeatSources => _heatSources.AsReadOnly();

    private readonly List<HydraulicCircuitConfiguration> _hydraulicCircuits = new();
    public IReadOnlyCollection<HydraulicCircuitConfiguration> HydraulicCircuits => _hydraulicCircuits.AsReadOnly();

    [Obsolete("Use DeviceBindings instead")]
    private readonly List<Guid> _deviceIds = new();
    [Obsolete("Use DeviceBindings instead")]
    public IReadOnlyCollection<Guid> DeviceIds => _deviceIds.AsReadOnly();

    private EngineeringSystem()
    {
        Name = string.Empty;
    }

    private EngineeringSystem(EngineeringSystemId id, BuildingId buildingId, string name, SystemType systemType, int priority, string? description)
    {
        Id = id; BuildingId = buildingId; Name = name; SystemType = systemType;
        Priority = priority; Description = description;
        Lifecycle = LifecycleStatus.Active;
        OperationalStatus = OperationalStatus.Unknown;
#pragma warning disable CS0618
        Status = SystemStatus.Enabled;
        LegacyStatus = SystemStatus.Enabled;
#pragma warning restore CS0618
        ControlMode = SystemControlMode.Automatic;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version = 1;
    }

    public static EngineeringSystem Create(BuildingId buildingId, string name, SystemType systemType,
        int priority = 100, string? description = null) =>
        new(EngineeringSystemId.New(), buildingId, name, systemType, priority, description);

    public void AddCapability(SystemCapability capability)
    {
        if (_capabilities.Any(c => c.Code == capability.Code))
            throw new InvalidOperationException($"Capability {capability.Code} already exists");
        _capabilities.Add(capability);
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void RemoveCapability(string code)
    {
        _capabilities.RemoveAll(c => c.Code == code);
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public bool HasCapability(string code) => _capabilities.Any(c => c.Code == code);

    public void AddResource(EngineeringResource resource)
    {
        if (_resources.Any(r => r.Code == resource.Code))
            throw new InvalidOperationException($"Resource {resource.Code} already exists");
        _resources.Add(resource);
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public EngineeringResource? GetResource(string code) =>
        _resources.FirstOrDefault(r => r.Code == code);

    public void AddZone(SystemZone zone)
    {
        _zones.Add(zone);
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public bool CoversRoom(RoomId roomId) => _zones.Any(z => z.CachedRoomIds.Contains(roomId));

    [Obsolete("Use AssignDeviceBinding instead")]
    public void AssignDevice(Guid deviceId)
    {
        if (!_deviceIds.Contains(deviceId))
            _deviceIds.Add(deviceId);
    }

    [Obsolete("Use RemoveDeviceBinding instead")]
    public void RemoveDevice(Guid deviceId) => _deviceIds.Remove(deviceId);

    public void AssignDeviceBinding(EngineeringSystemDeviceBinding binding)
    {
        if (_deviceBindings.Any(b => b.DeviceId == binding.DeviceId && b.Role == binding.Role))
            throw new InvalidOperationException($"Device {binding.DeviceId} already bound with role {binding.Role}");
        _deviceBindings.Add(binding);
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void RemoveDeviceBinding(Guid bindingId)
    {
        _deviceBindings.RemoveAll(b => b.Id == bindingId);
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void SetLifecycleStatus(LifecycleStatus status) { Lifecycle = status; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetOperationalStatus(OperationalStatus status) { OperationalStatus = status; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    [Obsolete("Use SetLifecycleStatus instead")]
    public void SetStatus(SystemStatus status)
    {
#pragma warning disable CS0618
        Status = status; LegacyStatus = status;
#pragma warning restore CS0618
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }
    public void SetControlMode(SystemControlMode mode) { ControlMode = mode; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Rename(string name) { Name = name; UpdatedAt = DateTimeOffset.UtcNow; Version++; }

    public void SetVentilationConfiguration(VentilationSystemConfiguration config)
    {
        VentilationConfiguration = config;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void SetThermalConfiguration(ThermalSystemConfiguration config)
    {
        ThermalConfiguration = config;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    public void AddHeatSource(HeatSourceConfiguration source)
    {
        if (_heatSources.Any(h => h.SourceType == source.SourceType && h.IsPrimary == source.IsPrimary))
            throw new InvalidOperationException($"Heat source {source.SourceType} already configured");
        _heatSources.Add(source);
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void AddHydraulicCircuit(HydraulicCircuitConfiguration circuit)
    {
        if (_hydraulicCircuits.Any(c => c.CircuitType == circuit.CircuitType))
            throw new InvalidOperationException($"Circuit {circuit.CircuitType} already configured");
        _hydraulicCircuits.Add(circuit);
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }
}

public class SystemCapability
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; set; }
    public string Code { get; private set; } = string.Empty;
    public string? DataType { get; private set; }
    public string? Unit { get; private set; }
    public double? Minimum { get; private set; }
    public double? Maximum { get; private set; }
    public bool SupportsModulation { get; private set; }
    public uint Version { get; private set; }

    private SystemCapability() { }

    public SystemCapability(string code, string? dataType = null, string? unit = null,
        double? minimum = null, double? maximum = null, bool supportsModulation = true)
    {
        Id = Guid.NewGuid(); Code = code;
        DataType = dataType; Unit = unit;
        Minimum = minimum; Maximum = maximum;
        SupportsModulation = supportsModulation;
        Version = 1;
    }
}

public class EngineeringResource
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; set; }
    public string Code { get; private set; } = string.Empty;
    public string? Unit { get; private set; }
    public double Maximum { get; private set; }
    public double Available { get; private set; }
    public double Reserved { get; private set; }
    public double Used { get; private set; }
    public int Priority { get; private set; }
    public EngineeringResourceAllocation? CurrentAllocation { get; private set; }
    public uint Version { get; private set; }

    private EngineeringResource() { }

    public EngineeringResource(string code, double maximum, string? unit = null, int priority = 100)
    {
        Id = Guid.NewGuid(); Code = code;
        Maximum = maximum; Available = maximum;
        Unit = unit; Priority = priority;
        Version = 1;
    }

    public bool CanAllocate(double amount) => Reserved + amount <= Maximum;
    public void Reserve(double amount) { Reserved += amount; Available = Maximum - Reserved - Used; Version++; }
    public void Allocate(double amount) { Used += amount; Reserved -= amount; Available = Maximum - Reserved - Used; Version++; }
    public void Release(double amount) { Used = Math.Max(0, Used - amount); Available = Maximum - Reserved - Used; Version++; }
    public void SetCurrentAllocation(EngineeringResourceAllocation? allocation) { CurrentAllocation = allocation; Version++; }
}

public enum AllocationStatus { Requested, Reserved, Allocated, Released, Rejected, Expired }

public class EngineeringResourceAllocation
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid CommandPlanId { get; private set; }
    public double RequestedAmount { get; private set; }
    public double ReservedAmount { get; private set; }
    public AllocationStatus Status { get; private set; }
    public uint Version { get; private set; }

    private EngineeringResourceAllocation() { }

    public EngineeringResourceAllocation(Guid resourceId, Guid commandPlanId, double requestedAmount)
    {
        Id = Guid.NewGuid(); ResourceId = resourceId;
        CommandPlanId = commandPlanId; RequestedAmount = requestedAmount;
        ReservedAmount = 0; Status = AllocationStatus.Requested;
        Version = 1;
    }

    public void Reserve(double amount) { ReservedAmount = amount; Status = AllocationStatus.Reserved; Version++; }
    public void Allocate() { Status = AllocationStatus.Allocated; Version++; }
    public void Release() { Status = AllocationStatus.Released; Version++; }
    public void Reject() { Status = AllocationStatus.Rejected; Version++; }
    public void Expire() { Status = AllocationStatus.Expired; Version++; }
}

public class EngineeringSystemDeviceBinding
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; set; }
    public Guid DeviceId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public bool Enabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private EngineeringSystemDeviceBinding() { }

    public EngineeringSystemDeviceBinding(Guid engineeringSystemId, Guid deviceId, string role, int priority = 100, bool enabled = true)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engineeringSystemId;
        DeviceId = deviceId; Role = role; Priority = priority;
        Enabled = enabled; CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetRole(string role) { Role = role; UpdatedAt = DateTimeOffset.UtcNow; }
    public void SetPriority(int priority) { Priority = priority; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Enable() { Enabled = true; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Disable() { Enabled = false; UpdatedAt = DateTimeOffset.UtcNow; }
}

public class SystemZoneRoom
{
    public Guid Id { get; private set; }
    public Guid ZoneId { get; private set; }
    public Guid ZoneRoomId { get; private set; }
    public RoomId RoomId { get; private set; }
    public double CoverageWeight { get; private set; }
    public int Priority { get; private set; }
    public bool Enabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private SystemZoneRoom() { }

    public SystemZoneRoom(Guid zoneId, RoomId roomId, double coverageWeight = 1.0, int priority = 100, bool enabled = true)
    {
        Id = Guid.NewGuid(); ZoneId = zoneId;
        ZoneRoomId = Guid.NewGuid(); RoomId = roomId;
        CoverageWeight = coverageWeight; Priority = priority;
        Enabled = enabled; CreatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCoverageWeight(double weight) { CoverageWeight = weight; UpdatedAt = DateTimeOffset.UtcNow; }
    public void SetPriority(int priority) { Priority = priority; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Enable() { Enabled = true; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Disable() { Enabled = false; UpdatedAt = DateTimeOffset.UtcNow; }
}

public class SystemZone
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public List<SystemZoneRoom> ZoneRooms { get; private set; } = new();
    [Obsolete("Use ZoneRooms instead")]
    public List<RoomId> RoomIds { get; private set; } = new();
    public IReadOnlyCollection<RoomId> CachedRoomIds => ZoneRooms.Select(zr => zr.RoomId).ToList().AsReadOnly();
    public int Priority { get; private set; }
    public EngineeringSystemId? PreferredEngineeringSystemId { get; private set; }

    private SystemZone()
    {
        Name = string.Empty;
    }

    public SystemZone(string name, IEnumerable<RoomId> roomIds, int priority = 100, EngineeringSystemId? preferredSystemId = null)
    {
        Id = Guid.NewGuid(); Name = name;
        Priority = priority;
        PreferredEngineeringSystemId = preferredSystemId;
#pragma warning disable CS0618
        RoomIds = roomIds.ToList();
        ZoneRooms = RoomIds.Select(r => new SystemZoneRoom(Id, r)).ToList();
#pragma warning restore CS0618
    }

    public void SetPreferredSystem(EngineeringSystemId? systemId) { PreferredEngineeringSystemId = systemId; }
    public void AddZoneRoom(SystemZoneRoom room)
    {
        ZoneRooms.Add(room);
#pragma warning disable CS0618
        RoomIds.Add(room.RoomId);
#pragma warning restore CS0618
    }

    public void RemoveZoneRoom(Guid zoneRoomId)
    {
        var removed = ZoneRooms.FirstOrDefault(zr => zr.ZoneRoomId == zoneRoomId);
        if (removed != null)
        {
            ZoneRooms.Remove(removed);
#pragma warning disable CS0618
            RoomIds.RemoveAll(r => r == removed.RoomId);
#pragma warning restore CS0618
        }
    }
}
