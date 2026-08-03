namespace ClimateHub.DeviceGateway.Models;

public static class QualityClassifier
{
    public static string Classify(Contracts.EnvironmentTelemetryPayload payload)
    {
        if (payload.TemperatureC is null && payload.RelativeHumidityPct is null && payload.Co2Ppm is null)
            return "invalid";

        return "valid";
    }
}