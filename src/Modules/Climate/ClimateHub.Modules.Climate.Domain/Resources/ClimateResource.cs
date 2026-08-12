using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Domain.Resources;

public readonly record struct ClimateResourceId(Guid Value)
{
    public static ClimateResourceId New() => new(Guid.NewGuid());
    public static ClimateResourceId From(Guid value) => new(value);
}

public class ClimateResource : Entity<ClimateResourceId>, IAggregateRoot
{
    public BuildingId BuildingId { get; private init; }
    public string ResourceCode { get; private init; }
    public string Unit { get; private init; }
    public double MaximumCapacity { get; private set; }
    public double AvailableCapacity { get; private set; }
    public double ReservedCapacity { get; private set; }
    public uint Version { get; private set; }

    private ClimateResource() { ResourceCode = string.Empty; Unit = string.Empty; }

    public ClimateResource(BuildingId buildingId, string resourceCode, string unit, double maximumCapacity)
    {
        Id = ClimateResourceId.New(); BuildingId = buildingId;
        ResourceCode = resourceCode; Unit = unit;
        MaximumCapacity = maximumCapacity; AvailableCapacity = maximumCapacity;
        Version = 1;
    }

    public bool CanReserve(double amount) => ReservedCapacity + amount <= MaximumCapacity;
    public void Reserve(double amount) { ReservedCapacity += amount; AvailableCapacity = MaximumCapacity - ReservedCapacity; Version++; }
    public void Release(double amount) { ReservedCapacity = Math.Max(0, ReservedCapacity - amount); AvailableCapacity = MaximumCapacity - ReservedCapacity; Version++; }
}

public static class ClimateResourceCodes
{
    public const string ElectricalCapacity = "electrical_capacity";
    public const string HeatingCapacity = "heating_capacity";
    public const string CoolingCapacity = "cooling_capacity";
    public const string FreshAirCapacity = "fresh_air_capacity";
    public const string WaterCapacity = "water_capacity";
}
