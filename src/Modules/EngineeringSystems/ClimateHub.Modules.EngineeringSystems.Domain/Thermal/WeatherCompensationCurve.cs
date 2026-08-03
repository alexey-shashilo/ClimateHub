using ClimateHub.SharedKernel.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public readonly record struct WeatherCompensationCurveId(Guid Value)
{
    public static WeatherCompensationCurveId New() => new(Guid.NewGuid());
    public static WeatherCompensationCurveId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public class WeatherCompensationCurve : Entity<WeatherCompensationCurveId>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public double Slope { get; private set; }
    public double ParallelShiftC { get; private set; }
    public double ReferenceOutdoorTemperatureC { get; private set; }
    public double MinimumSupplyTemperatureC { get; private set; }
    public double MaximumSupplyTemperatureC { get; private set; }
    public double RoomInfluenceFactor { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private WeatherCompensationCurve() { }

    public WeatherCompensationCurve(EngineeringSystemId engId, string name,
        double slope = 1.2, double parallelShift = 0,
        double referenceOutdoor = 20, double minSupply = 20, double maxSupply = 60,
        double roomInfluence = 0.5)
    {
        Id = WeatherCompensationCurveId.New(); EngineeringSystemId = engId; Name = name;
        Slope = slope; ParallelShiftC = parallelShift;
        ReferenceOutdoorTemperatureC = referenceOutdoor;
        MinimumSupplyTemperatureC = minSupply; MaximumSupplyTemperatureC = maxSupply;
        RoomInfluenceFactor = roomInfluence; Enabled = true; Version = 1;
    }

    public void Update(double slope, double shift) { Slope = slope; ParallelShiftC = shift; Version++; }
    public void SetEnabled(bool enabled) { Enabled = enabled; Version++; }
}