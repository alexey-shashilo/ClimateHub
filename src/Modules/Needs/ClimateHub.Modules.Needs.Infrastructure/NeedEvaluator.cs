using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Needs.Domain;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class NeedEvaluator
{
    private readonly AntiOscillationOptions _antiOscillation;

    public NeedEvaluator(AntiOscillationOptions antiOscillation)
    {
        _antiOscillation = antiOscillation;
    }

    public NeedEvaluationResult Evaluate(string parameter, double current, RoomPolicyDto? policy)
    {
        var opts = _antiOscillation.GetForParameter(parameter);

        if (string.IsNullOrEmpty(parameter)) parameter = "co2";

        var (needType, severity, deviation, min, max, pref) = parameter switch
        {
            "temperature" => ComputeTemperature(current, policy),
            "humidity" => ComputeHumidity(current, policy),
            "co2" => ComputeCo2(current, policy),
            "illuminance" => ComputeIlluminance(current, policy),
            _ => ((NeedType?)null, NeedSeverity.Low, 0.0, 0.0, 0.0, 0.0)
        };

        if (needType is null)
            return new NeedEvaluationResult(null, NeedSeverity.Low, 0, min, max, pref, opts, true);

        return new NeedEvaluationResult(needType, severity, deviation, min, max, pref, opts, false);
    }

    public bool ShouldCreateNeed(double deviation, ParameterOptions opts) =>
        deviation >= opts.DetectionDeadband;

    public bool IsSatisfied(double current, double desiredMax, double hysteresisThreshold) =>
        current <= desiredMax - hysteresisThreshold;

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeTemperature(
        double current, RoomPolicyDto? p)
    {
        var (min, max, pref) = (p?.TemperatureMin ?? 20, p?.TemperatureMax ?? 26, p?.TemperaturePreferred ?? 23);
        if (current >= min && current <= max) return (null, NeedSeverity.Low, 0, min, max, pref);
        var dev = current < min ? min - current : current - max;
        var sev = dev switch { > 8 => NeedSeverity.Critical, > 4 => NeedSeverity.High, > 2 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (current < min ? NeedType.TemperatureHeating : NeedType.TemperatureCooling, sev, dev, min, max, pref);
    }

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeHumidity(
        double current, RoomPolicyDto? p)
    {
        var (min, max, pref) = (p?.HumidityMin ?? 30, p?.HumidityMax ?? 60, p?.HumidityPreferred ?? 45);
        if (current >= min && current <= max) return (null, NeedSeverity.Low, 0, min, max, pref);
        var dev = current < min ? min - current : current - max;
        var sev = dev switch { > 20 => NeedSeverity.Critical, > 10 => NeedSeverity.High, > 5 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (current < min ? NeedType.HumidityIncrease : NeedType.HumidityDecrease, sev, dev, min, max, pref);
    }

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeCo2(
        double current, RoomPolicyDto? p)
    {
        var max = p?.Co2Max ?? 1000;
        if (current <= max) return (null, NeedSeverity.Low, 0, 0, max, p?.Co2Preferred ?? 600);
        var dev = current - max;
        var sev = dev switch { > 1000 => NeedSeverity.Critical, > 500 => NeedSeverity.High, > 200 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (NeedType.Co2Reduction, sev, dev, 0, max, p?.Co2Preferred ?? 600);
    }

    private static (NeedType? type, NeedSeverity severity, double deviation, double min, double max, double pref) ComputeIlluminance(
        double current, RoomPolicyDto? p)
    {
        var (min, max, pref) = (p?.IlluminanceMin ?? 300, p?.IlluminanceMax ?? 750, p?.IlluminancePreferred ?? 500);
        if (current >= min && current <= max) return (null, NeedSeverity.Low, 0, min, max, pref);
        var dev = current < min ? min - current : current - max;
        var sev = dev switch { > 300 => NeedSeverity.Critical, > 150 => NeedSeverity.High, > 50 => NeedSeverity.Medium, _ => NeedSeverity.Low };
        return (current < min ? NeedType.IlluminanceIncrease : NeedType.IlluminanceDecrease, sev, dev, min, max, pref);
    }
}

public record NeedEvaluationResult(
    NeedType? Type, NeedSeverity Severity, double Deviation,
    double Min, double Max, double Preferred,
    ParameterOptions AntiOscillationOptions, bool IsInRange);