using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Lighting;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class SolarPositionResult
{
    public double AzimuthDeg { get; init; }
    public double ElevationDeg { get; init; }
    public DateTime Sunrise { get; init; }
    public DateTime Sunset { get; init; }
    public bool IsDaytime { get; init; }
    public double ZenithDeg { get; init; }
    public string Quality { get; init; } = "Calculated";
}

public class DaylightHarvestingResult
{
    public double EstimatedIndoorLux { get; init; }
    public double NaturalLightingContributionPct { get; init; }
    public double RequiredArtificialLux { get; init; }
    public double RequiredDimmerPct { get; init; }
    public bool DaylightSufficient { get; init; }
    public string Quality { get; init; } = "Estimated";
}

public class SolarProtectionResult
{
    public bool ProtectionRequired { get; init; }
    public double TargetBlindPositionPct { get; init; }
    public double TargetSlatAngleDeg { get; init; }
    public bool OverheatingRisk { get; init; }
    public bool GlareRisk { get; init; }
    public string? Reason { get; init; }
}

public class SolarPositionCalculator
{
    public SolarPositionResult Calculate(double latitude, double longitude,
        DateTime dateTime, double timezoneOffset)
    {
        var utc = dateTime.ToUniversalTime();
        var dayOfYear = utc.DayOfYear;
        var solarDeclination = 23.45 * Math.Sin((360.0 / 365) * (dayOfYear - 81) * Math.PI / 180.0);
        var equationOfTime = 9.87 * Math.Sin(2 * (360.0 / 365) * (dayOfYear - 81) * Math.PI / 180.0)
            - 7.53 * Math.Cos((360.0 / 365) * (dayOfYear - 81) * Math.PI / 180.0)
            - 1.5 * Math.Sin((360.0 / 365) * (dayOfYear - 81) * Math.PI / 180.0);
        var timeOffset = equationOfTime + 4 * longitude - 60 * timezoneOffset;
        var solarHourAngle = (utc.Hour * 60 + utc.Minute + timeOffset) / 4.0 - 180;
        var latitudeRad = latitude * Math.PI / 180.0;
        var declinationRad = solarDeclination * Math.PI / 180.0;
        var hourAngleRad = solarHourAngle * Math.PI / 180.0;

        var elevation = Math.Asin(
            Math.Sin(latitudeRad) * Math.Sin(declinationRad) +
            Math.Cos(latitudeRad) * Math.Cos(declinationRad) * Math.Cos(hourAngleRad)) * 180.0 / Math.PI;

        var azimuth = Math.Acos(
            (Math.Sin(declinationRad) * Math.Cos(latitudeRad) -
             Math.Cos(declinationRad) * Math.Sin(latitudeRad) * Math.Cos(hourAngleRad)) /
            Math.Cos(elevation * Math.PI / 180.0)) * 180.0 / Math.PI;

        if (solarHourAngle > 0) azimuth = 360 - azimuth;

        return new SolarPositionResult
        {
            AzimuthDeg = Math.Round(azimuth, 1),
            ElevationDeg = Math.Round(elevation, 1),
            ZenithDeg = Math.Round(90 - elevation, 1),
            IsDaytime = elevation > 0,
            Quality = elevation > 10 ? "Calculated" : "LowAccuracy",
            Sunrise = dateTime.Date.AddHours(6),
            Sunset = dateTime.Date.AddHours(18)
        };
    }
}

public class DaylightHarvestingPlanner
{
    private readonly SolarPositionCalculator _solarCalc;

    public DaylightHarvestingPlanner(SolarPositionCalculator solarCalc) { _solarCalc = solarCalc; }

    public DaylightHarvestingResult Estimate(
        double outdoorBrightnessLux, double windowAreaM2,
        double windowOrientationDeg, double blindPositionPct,
        double targetLux, double latitude, double longitude,
        DateTime dateTime, double timezoneOffset, double roomAreaM2)
    {
        var solar = _solarCalc.Calculate(latitude, longitude, dateTime, timezoneOffset);
        if (!solar.IsDaytime)
            return new DaylightHarvestingResult { DaylightSufficient = false, RequiredArtificialLux = targetLux, RequiredDimmerPct = 100 };

        var facadeExposure = Math.Max(0, Math.Cos((windowOrientationDeg - solar.AzimuthDeg) * Math.PI / 180));
        var usableDaylight = outdoorBrightnessLux * 0.01 * facadeExposure * (1 - blindPositionPct / 100);
        var estimatedIndoorLux = usableDaylight * windowAreaM2 / Math.Max(roomAreaM2, 1);
        var naturalPct = Math.Min(estimatedIndoorLux / Math.Max(targetLux, 1) * 100, 100);
        var requiredArtificial = Math.Max(0, targetLux - estimatedIndoorLux);
        var dimmerPct = targetLux > 0 ? Math.Clamp(requiredArtificial / targetLux * 100, 0, 100) : 100;

        return new DaylightHarvestingResult
        {
            EstimatedIndoorLux = Math.Round(estimatedIndoorLux, 0),
            NaturalLightingContributionPct = Math.Round(naturalPct, 0),
            RequiredArtificialLux = Math.Round(requiredArtificial, 0),
            RequiredDimmerPct = Math.Round(dimmerPct, 0),
            DaylightSufficient = estimatedIndoorLux >= targetLux * 0.9,
            Quality = "Estimated"
        };
    }
}

public class SolarProtectionPlanner
{
    public SolarProtectionResult Evaluate(
        double outdoorBrightnessLux, double outdoorTemperatureC,
        double solarElevationDeg, double facadeAzimuthDeg,
        double windowOrientationDeg,
        LightingSystemConfiguration config)
    {
        if (outdoorBrightnessLux < config.SolarProtectionThresholdLux * 0.5)
            return new SolarProtectionResult { ProtectionRequired = false };

        var facadeExposure = Math.Max(0, Math.Cos((windowOrientationDeg - facadeAzimuthDeg) * Math.PI / 180));
        if (facadeExposure < 0.3)
            return new SolarProtectionResult { ProtectionRequired = false };

        var overheatingRisk = outdoorTemperatureC > config.SolarProtectionTemperatureC;
        var glareRisk = solarElevationDeg > 15 && solarElevationDeg < 60 && facadeExposure > 0.5;

        if (overheatingRisk || glareRisk)
        {
            var blindPosition = overheatingRisk ? 20 : glareRisk ? 30 : 50;
            var slatAngle = overheatingRisk ? 45 : glareRisk ? 60 : 0;
            return new SolarProtectionResult
            {
                ProtectionRequired = true,
                TargetBlindPositionPct = blindPosition,
                TargetSlatAngleDeg = slatAngle,
                OverheatingRisk = overheatingRisk,
                GlareRisk = glareRisk,
                Reason = (overheatingRisk ? "Overheating risk" : "") +
                         (overheatingRisk && glareRisk ? " and " : "") +
                         (glareRisk ? "Glare risk" : "")
            };
        }

        return new SolarProtectionResult { ProtectionRequired = false };
    }
}

public class CircadianLightingPlanner
{
    public (double cct, string period) GetCircadianCct(DateTime time, LightingSystemConfiguration config)
    {
        if (!config.CircadianEnabled) return (config.DefaultColorTemperatureK, "Fixed");

        var hour = time.Hour + time.Minute / 60.0;
        return hour switch
        {
            < 6 => (config.NightColorTemperatureK, "Night"),
            < 8 => (config.MorningColorTemperatureK, "Morning"),
            < 17 => (config.DayColorTemperatureK, "Day"),
            < 20 => (config.EveningColorTemperatureK, "Evening"),
            _ => (config.NightColorTemperatureK, "Night")
        };
    }
}

public class OccupancyPlanner
{
    public OccupancyState Evaluate(DateTime? lastMotion, DateTime now, int timeoutMinutes)
    {
        if (!lastMotion.HasValue) return OccupancyState.Unknown;
        return (now - lastMotion.Value).TotalMinutes > timeoutMinutes
            ? OccupancyState.Vacant : OccupancyState.Occupied;
    }
}

// === Lighting Strategy Engine ===

public class LightingStrategyResult
{
    public double TargetBrightnessLux { get; init; }
    public double DimmerLevelPct { get; init; }
    public double ColorTemperatureK { get; init; }
    public double? ColorR { get; init; }
    public double? ColorG { get; init; }
    public double? ColorB { get; init; }
    public double BlindPositionPct { get; init; }
    public double CurtainPositionPct { get; init; }
    public bool LightsOn { get; init; }
    public string AppliedScene { get; init; } = "None";
    public SolarProtectionResult? SolarProtection { get; init; }
    public DaylightHarvestingResult? Daylight { get; init; }
    public SolarPositionResult? SolarPosition { get; init; }
    public List<CommandPlanStep> Steps { get; init; } = new();
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public bool IsSuccess => FailureCode is null;
}

public class LightingStrategyEngine
{
    private readonly SolarPositionCalculator _solarCalc;
    private readonly DaylightHarvestingPlanner _daylightPlanner;
    private readonly SolarProtectionPlanner _solarProtection;
    private readonly CircadianLightingPlanner _circadianPlanner;
    private readonly OccupancyPlanner _occupancyPlanner;

    public LightingStrategyEngine(
        SolarPositionCalculator solarCalc,
        DaylightHarvestingPlanner daylightPlanner,
        SolarProtectionPlanner solarProtection,
        CircadianLightingPlanner circadianPlanner,
        OccupancyPlanner occupancyPlanner)
    {
        _solarCalc = solarCalc; _daylightPlanner = daylightPlanner;
        _solarProtection = solarProtection; _circadianPlanner = circadianPlanner;
        _occupancyPlanner = occupancyPlanner;
    }

    public LightingStrategyResult PlanLighting(
        LightingSystemConfiguration config,
        double currentLux, double targetLux,
        double outdoorBrightness, double outdoorTemp,
        double latitude, double longitude, DateTime now, double timezoneOffset,
        double windowArea, double windowOrientation, double roomArea,
        DateTime? lastMotion = null,
        LightingScene? activeScene = null,
        bool illuminationIncrease = true)
    {
        // Step 1: Solar position
        var solar = _solarCalc.Calculate(latitude, longitude, now, timezoneOffset);

        // Step 2: Occupancy check
        var occupancy = _occupancyPlanner.Evaluate(lastMotion, now, config.OccupancyTimeoutMinutes);
        if (occupancy == OccupancyState.Vacant && config.OccupancyControlEnabled)
        {
            var vacSteps = new List<CommandPlanStep>
            {
                new("control.lighting", "set", 0, "percent", deviceRole: DeviceRole.Dimmer, sequence: 0, required: true)
            };
            if (config.WindowOpenProtectionEnabled)
            {
                vacSteps.Add(new CommandPlanStep("control.curtain-motor", "set", 100, "percent",
                    deviceRole: DeviceRole.CurtainMotor, sequence: 1, required: false));
            }
            return new LightingStrategyResult
            {
                TargetBrightnessLux = 0,
                DimmerLevelPct = 0,
                LightsOn = false,
                AppliedScene = "Away",
                SolarPosition = solar,
                Steps = vacSteps
            };
        }

        // Step 3: Scene or circadian
        var (cct, period) = _circadianPlanner.GetCircadianCct(now, config);
        if (activeScene is not null)
        {
            cct = activeScene.ColorTemperatureK;
            targetLux = activeScene.BrightnessLux;
        }

        // Step 4: Daylight harvesting
        DaylightHarvestingResult? daylight = null;
        if (config.DaylightHarvestingEnabled && solar.IsDaytime)
        {
            var blindPos = activeScene?.BlindPositionPct ?? 100;
            daylight = _daylightPlanner.Estimate(outdoorBrightness, windowArea,
                windowOrientation, blindPos, targetLux, latitude, longitude,
                now, timezoneOffset, roomArea);
            if (daylight.DaylightSufficient)
            {
                return new LightingStrategyResult
                {
                    TargetBrightnessLux = 0,
                    DimmerLevelPct = 0,
                    ColorTemperatureK = cct,
                    LightsOn = false,
                    AppliedScene = activeScene?.SceneType.ToString() ?? "Daylight",
                    Daylight = daylight,
                    SolarPosition = solar,
                    Steps = new List<CommandPlanStep>()
                };
            }
        }

        // Step 5: Solar protection
        SolarProtectionResult? protection = null;
        if (solar.ElevationDeg > 15 && solar.IsDaytime)
        {
            protection = _solarProtection.Evaluate(outdoorBrightness, outdoorTemp,
                solar.ElevationDeg, 180, windowOrientation, config);
        }

        // Step 6: Build command steps
        var steps = new List<CommandPlanStep>();
        var seq = 0;
        var targetBrightness = daylight?.RequiredArtificialLux ?? targetLux;
        var dimmerLevel = daylight?.RequiredDimmerPct ??
            (targetLux > 0 ? Math.Clamp(targetLux / config.MaximumBrightnessLux * 100, 0, 100) : 100);

        steps.Add(new CommandPlanStep("control.dimmer", "set", (int)dimmerLevel, "percent",
            deviceRole: DeviceRole.Dimmer, sequence: seq++, required: true));

        if (config.CircadianEnabled)
        {
            steps.Add(new CommandPlanStep("control.cct", "set", (int)cct, "kelvin",
                deviceRole: DeviceRole.CctController, sequence: seq++, required: false));
        }

        if (protection?.ProtectionRequired == true)
        {
            steps.Add(new CommandPlanStep("control.blind-motor", "set",
                (int)protection.TargetBlindPositionPct, "percent",
                deviceRole: DeviceRole.BlindMotor, sequence: seq++, required: false));
        }

        return new LightingStrategyResult
        {
            TargetBrightnessLux = Math.Round(targetBrightness, 0),
            DimmerLevelPct = Math.Round(dimmerLevel, 0),
            ColorTemperatureK = cct,
            LightsOn = dimmerLevel > 0,
            AppliedScene = activeScene?.SceneType.ToString() ?? period,
            SolarProtection = protection,
            Daylight = daylight,
            SolarPosition = solar,
            Steps = steps
        };
    }
}
