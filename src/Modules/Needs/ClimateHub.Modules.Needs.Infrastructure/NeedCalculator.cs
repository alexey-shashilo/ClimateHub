using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class NeedCalculator
{
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly IRoomPolicyReader _policyReader;

    public NeedCalculator(IRoomEnvironmentStateReader envReader, IRoomPolicyReader policyReader)
    {
        _envReader = envReader;
        _policyReader = policyReader;
    }

    public async Task<NeedCalculationResult> CalculateAsync(RoomId roomId, BuildingId buildingId, CancellationToken ct = default)
    {
        var parameters = await _envReader.GetParametersAsync(roomId, ct);
        var policy = await _policyReader.GetByRoomAsync(roomId, ct);
        var needs = new List<Need>();

        foreach (var param in parameters)
        {
            if (param.Value is null) continue;
            var (type, desired, deviation, min, max) = ComputeNeed(param.Parameter, param.Value.Value, policy);
            if (type is null) continue;
            var severity = CalculateSeverity(deviation);
            needs.Add(Need.Create(buildingId, roomId, type.Value, severity, min, max, desired, param.Value.Value, deviation, param.Parameter));
        }
        return new NeedCalculationResult(needs, DateTimeOffset.UtcNow);
    }

    private static (NeedType? type, double desired, double deviation, double min, double max) ComputeNeed(string parameter, double current, RoomPolicyDto? policy)
    {
        return parameter switch
        {
            "temperature" => ComputeTemperature(current, policy),
            "humidity" => ComputeHumidity(current, policy),
            "co2" => ComputeCo2(current, policy),
            "illuminance" => ComputeIlluminance(current, policy),
            _ => (null, 0, 0, 0, 0)
        };
    }

    private static (NeedType? type, double desired, double deviation, double min, double max) ComputeTemperature(double current, RoomPolicyDto? policy)
    {
        var pref = policy?.TemperaturePreferred ?? 23;
        var min = policy?.TemperatureMin ?? 20;
        var max = policy?.TemperatureMax ?? 26;
        if (current >= min && current <= max) return (null, pref, 0, min, max);
        if (current < min) return (NeedType.TemperatureHeating, pref, min - current, min, max);
        return (NeedType.TemperatureCooling, pref, current - max, min, max);
    }

    private static (NeedType? type, double desired, double deviation, double min, double max) ComputeHumidity(double current, RoomPolicyDto? policy)
    {
        var pref = policy?.HumidityPreferred ?? 45;
        var min = policy?.HumidityMin ?? 30;
        var max = policy?.HumidityMax ?? 60;
        if (current >= min && current <= max) return (null, pref, 0, min, max);
        if (current < min) return (NeedType.HumidityIncrease, pref, min - current, min, max);
        return (NeedType.HumidityDecrease, pref, current - max, min, max);
    }

    private static (NeedType? type, double desired, double deviation, double min, double max) ComputeCo2(double current, RoomPolicyDto? policy)
    {
        var max = policy?.Co2Max ?? 1000;
        if (current <= max) return (null, max, 0, 0, max);
        return (NeedType.Co2Reduction, max, current - max, 0, max);
    }

    private static (NeedType? type, double desired, double deviation, double min, double max) ComputeIlluminance(double current, RoomPolicyDto? policy)
    {
        var min = policy?.IlluminanceMin ?? 300;
        var max = policy?.IlluminanceMax ?? 750;
        if (current >= min && current <= max) return (null, 0, 0, min, max);
        if (current < min) return (NeedType.IlluminanceIncrease, (min + max) / 2, min - current, min, max);
        return (NeedType.IlluminanceDecrease, (min + max) / 2, current - max, min, max);
    }

    private static NeedSeverity CalculateSeverity(double deviation)
    {
        return Math.Abs(deviation) switch
        {
            > 500 => NeedSeverity.Critical,
            > 200 => NeedSeverity.High,
            > 50 => NeedSeverity.Medium,
            _ => NeedSeverity.Low
        };
    }
}
