using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class WeatherCompensationResult
{
    public double TargetSupplyTemperature { get; init; }
    public double? ExpectedReturnTemperature { get; init; }
    public string Quality { get; init; } = "Estimated";
    public string? Description { get; init; }
}

public class WeatherCompensationPlanner
{
    public WeatherCompensationResult Calculate(ThermalSystemConfiguration config, double outdoorTemp, double indoorTemp)
    {
        if (!config.WeatherCompensationEnabled)
            return new WeatherCompensationResult
            {
                TargetSupplyTemperature = config.DesignSupplyTemperature,
                Description = "Weather compensation disabled, using design temperature"
            };

        var clampedOutdoor = Math.Clamp(outdoorTemp,
            config.WeatherCompensationMinOutdoor,
            config.WeatherCompensationMaxOutdoor);

        var referenceOutdoor = config.WeatherCompensationMaxOutdoor;
        var deltaT = referenceOutdoor - clampedOutdoor;
        var supplyTemp = config.WeatherCompensationMinSupply +
            config.WeatherCompensationSlope * deltaT +
            config.WeatherCompensationParallelShift;

        var roomAdjustment = (20 - indoorTemp) * 0.5;
        supplyTemp += Math.Max(0, roomAdjustment);

        supplyTemp = Math.Clamp(supplyTemp,
            config.WeatherCompensationMinSupply,
            config.WeatherCompensationMaxSupply);

        var returnTemp = supplyTemp - (config.DesignSupplyTemperature - config.DesignReturnTemperature);

        return new WeatherCompensationResult
        {
            TargetSupplyTemperature = Math.Round(supplyTemp, 1),
            ExpectedReturnTemperature = Math.Round(Math.Max(returnTemp, 15), 1),
            Quality = "Estimated",
            Description = $"Outdoor {outdoorTemp:F1}°C → supply {supplyTemp:F1}°C"
        };
    }
}

public class HeatLossEstimator
{
    public HeatLossResult Estimate(ThermalSystemConfiguration config,
        double outdoorTemp, double indoorTemp, double windSpeed,
        double? previousHeatOutput = null)
    {
        var deltaT = indoorTemp - outdoorTemp;
        if (deltaT <= 0)
            return new HeatLossResult { RequiredPower = 0, EstimatedDeltaT = deltaT, Quality = "Minimal" };

        var designDeltaT = 20 - (-26);
        var baseHeatLoss = config.DesignHeatLoad * (deltaT / designDeltaT);

        var windFactor = windSpeed > 2 ? 1.0 + (windSpeed - 2) * 0.01 : 1.0;
        var adjustedHeatLoss = baseHeatLoss * windFactor;

        return new HeatLossResult
        {
            RequiredPower = Math.Round(adjustedHeatLoss, 0),
            EstimatedDeltaT = deltaT,
            Quality = windFactor > 1.1 ? "EstimatedWithWindAdjustment" : "Estimated"
        };
    }

    public double EstimateThermalInertia(ThermalZoneConfiguration zone, double outdoorTemp, double indoorTemp)
    {
        var deltaT = indoorTemp - outdoorTemp;
        if (deltaT <= 0) return 0;
        return zone.ThermalMass * 0.001;
    }
}

public record HeatLossResult
{
    public double RequiredPower { get; init; }
    public double EstimatedDeltaT { get; init; }
    public string Quality { get; init; } = "Unknown";
}