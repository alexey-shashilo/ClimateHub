using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct ThermalZoneId(Guid Value)
{
    public static ThermalZoneId New() => new(Guid.NewGuid());
    public static ThermalZoneId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum ThermalZoneStatus { Draft, Active, Disabled, Maintenance, Faulted }
public enum ThermalDemandStatus { None, HeatingRequested, HeatingActive, WaitingForEffect, Satisfied, Blocked }

public class ThermalZone : Entity<ThermalZoneId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public BuildingId BuildingId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public ThermalZoneStatus Status { get; private set; }
    public int Priority { get; private set; }
    public double DesignHeatLoadKw { get; private set; }
    public double ThermalMassKjPerK { get; private set; }
    public double HeatLossCoefficientWPerK { get; private set; }
    public double TargetTemperatureC { get; private set; }
    public double? EstimatedTemperatureC { get; private set; }
    public ThermalDemandStatus DemandStatus { get; private set; }
    public Guid? ActiveThermalPlanId { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private readonly List<ThermalZoneRoom> _rooms = new();
    public IReadOnlyCollection<ThermalZoneRoom> Rooms => _rooms.AsReadOnly();

    private ThermalZone() { }

    public ThermalZone(EngineeringSystemId engId, BuildingId buildingId, string name,
        double designHeatLoadKw = 5, double thermalMassKwMin = 2000, double heatLossWPerK = 50,
        int priority = 100)
    {
        Id = ThermalZoneId.New(); EngineeringSystemId = engId; BuildingId = buildingId;
        Name = name; Status = ThermalZoneStatus.Active; Priority = priority;
        DesignHeatLoadKw = designHeatLoadKw; ThermalMassKjPerK = thermalMassKwMin;
        HeatLossCoefficientWPerK = heatLossWPerK; TargetTemperatureC = 22;
        DemandStatus = ThermalDemandStatus.None;
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public void AddRoom(ThermalZoneRoom room) { _rooms.Add(room); UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void UpdateCurrentTemperature(double? temp) { EstimatedTemperatureC = temp; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void UpdateTarget(double target) { TargetTemperatureC = target; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void SetDemandStatus(ThermalDemandStatus status) { DemandStatus = status; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void AttachPlan(Guid planId) { ActiveThermalPlanId = planId; DemandStatus = ThermalDemandStatus.HeatingActive; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void ClearPlan() { ActiveThermalPlanId = null; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Disable() { Status = ThermalZoneStatus.Disabled; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
    public void Activate() { Status = ThermalZoneStatus.Active; UpdatedAt = DateTimeOffset.UtcNow; Version++; }
}

public class ThermalZoneRoom
{
    public Guid Id { get; private set; }
    public ThermalZoneId ZoneId { get; set; }
    public RoomId RoomId { get; private init; }
    public double Weight { get; private set; }
    public double DesignHeatLoadKw { get; private set; }
    public double MinimumTemperatureC { get; private set; }
    public double MaximumTemperatureC { get; private set; }
    public int Priority { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private ThermalZoneRoom() { RoomId = RoomId.From(Guid.Empty); }

    public ThermalZoneRoom(RoomId roomId, double designHeatLoadKw = 1.5, double weight = 1.0,
        double minTemp = 18, double maxTemp = 28, int priority = 100)
    {
        Id = Guid.NewGuid(); RoomId = roomId; Weight = weight;
        DesignHeatLoadKw = designHeatLoadKw; MinimumTemperatureC = minTemp;
        MaximumTemperatureC = maxTemp; Priority = priority; Enabled = true; Version = 1;
    }

    public void SetEnabled(bool enabled) { Enabled = enabled; Version++; }
}