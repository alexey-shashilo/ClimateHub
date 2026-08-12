namespace ClimateHub.Modules.Climate.Application.EffectModel;

public enum EffectDirection { Increase, Decrease, NoEffect }

public record CrossEffect(string SourceAction, string TargetParameter, EffectDirection Direction, double Magnitude);

public class CrossSystemEffectModel
{
    private static readonly List<CrossEffect> Effects = new()
    {
        // Ventilation affects temperature and humidity
        new("eng.airflow.increase", "temperature", EffectDirection.Decrease, 0.3),
        new("eng.airflow.increase", "humidity", EffectDirection.Decrease, 0.2),
        new("eng.co2.reduce", "temperature", EffectDirection.Decrease, 0.3),
        new("eng.co2.reduce", "humidity", EffectDirection.Decrease, 0.2),

        // Heating affects humidity
        new("eng.temperature.increase", "humidity", EffectDirection.Decrease, 0.4),
        new("eng.temperature.increase", "co2", EffectDirection.NoEffect, 0),

        // Cooling affects humidity
        new("eng.temperature.decrease", "humidity", EffectDirection.Increase, 0.2),

        // Humidification affects temperature
        new("eng.humidity.increase", "temperature", EffectDirection.Increase, 0.1),
        new("eng.humidity.decrease", "temperature", EffectDirection.Decrease, 0.1),

        // Lighting affects temperature
        new("eng.illuminance.increase", "temperature", EffectDirection.Increase, 0.05),
        new("eng.illuminance.decrease", "temperature", EffectDirection.Decrease, 0.03),
    };

    public IReadOnlyCollection<CrossEffect> GetEffects(string capabilityCode) =>
        Effects.Where(e => e.SourceAction == capabilityCode).ToList().AsReadOnly();

    public bool HasNegativeEffect(string sourceCapability, string targetCapability)
    {
        var targetParam = targetCapability switch
        {
            "eng.temperature.increase" or "eng.temperature.decrease" => "temperature",
            "eng.humidity.increase" or "eng.humidity.decrease" => "humidity",
            "eng.co2.reduce" => "co2",
            _ => null
        };

        if (targetParam is null) return false;

        var effect = Effects.FirstOrDefault(e =>
            e.SourceAction == sourceCapability && e.TargetParameter == targetParam);
        return effect?.Direction switch
        {
            EffectDirection.Decrease when targetCapability.Contains("increase") => true,
            EffectDirection.Increase when targetCapability.Contains("decrease") => true,
            _ => false
        };
    }
}
