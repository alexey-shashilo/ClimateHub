namespace ClimateHub.Modules.Needs.Domain;

public class AntiOscillationOptions
{
    public const string SectionName = "NeedEngine:AntiOscillation";

    public ParameterOptions Temperature { get; set; } = new()
    {
        DetectionDeadband = 0.3,
        ResolutionHysteresis = 0.5,
        MinimumViolationDuration = TimeSpan.FromMinutes(2),
        MinimumSatisfactionDuration = TimeSpan.FromMinutes(3),
        CommandCooldown = TimeSpan.FromMinutes(5),
        EffectEvaluationDelay = TimeSpan.FromMinutes(2)
    };

    public ParameterOptions RelativeHumidity { get; set; } = new()
    {
        DetectionDeadband = 2,
        ResolutionHysteresis = 3,
        MinimumViolationDuration = TimeSpan.FromMinutes(3),
        MinimumSatisfactionDuration = TimeSpan.FromMinutes(5),
        CommandCooldown = TimeSpan.FromMinutes(10),
        EffectEvaluationDelay = TimeSpan.FromMinutes(5)
    };

    public ParameterOptions Co2 { get; set; } = new()
    {
        DetectionDeadband = 50,
        ResolutionHysteresis = 50,
        MinimumViolationDuration = TimeSpan.FromMinutes(2),
        MinimumSatisfactionDuration = TimeSpan.FromMinutes(3),
        CommandCooldown = TimeSpan.FromMinutes(5),
        EffectEvaluationDelay = TimeSpan.FromMinutes(2)
    };

    public ParameterOptions Illuminance { get; set; } = new()
    {
        DetectionDeadband = 20,
        ResolutionHysteresis = 30,
        MinimumViolationDuration = TimeSpan.FromSeconds(30),
        MinimumSatisfactionDuration = TimeSpan.FromMinutes(1),
        CommandCooldown = TimeSpan.FromMinutes(1),
        EffectEvaluationDelay = TimeSpan.FromSeconds(30)
    };

    public ParameterOptions GetForParameter(string parameter) => parameter switch
    {
        "temperature" => Temperature,
        "humidity" => RelativeHumidity,
        "co2" => Co2,
        "illuminance" => Illuminance,
        _ => Co2
    };

    public static AntiOscillationOptions Default => new();
}

public class ParameterOptions
{
    public double DetectionDeadband { get; set; }
    public double ResolutionHysteresis { get; set; }
    public TimeSpan MinimumViolationDuration { get; set; }
    public TimeSpan MinimumSatisfactionDuration { get; set; }
    public TimeSpan CommandCooldown { get; set; }
    public TimeSpan EffectEvaluationDelay { get; set; }
}