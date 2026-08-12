using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Lighting;

public enum BlindType { Venetian, Roller, Roman, Pleated, Vertical, Panel }
public enum CurtainType { Curtain, Drape, Sheer, Blackout }
public enum SceneType { Morning, Day, Evening, Night, Cinema, Reading, Working, Relax, Away, Cleaning, Emergency }
public enum OccupancyState { Occupied, Vacant, Unknown }

public class LightingSystemConfiguration
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public double DefaultBrightnessLux { get; private set; }
    public double MinimumBrightnessLux { get; private set; }
    public double MaximumBrightnessLux { get; private set; }
    public double DefaultColorTemperatureK { get; private set; }
    public double MorningColorTemperatureK { get; private set; }
    public double DayColorTemperatureK { get; private set; }
    public double EveningColorTemperatureK { get; private set; }
    public double NightColorTemperatureK { get; private set; }
    public bool CircadianEnabled { get; private set; }
    public bool DaylightHarvestingEnabled { get; private set; }
    public bool OccupancyControlEnabled { get; private set; }
    public int OccupancyTimeoutMinutes { get; private set; }
    public double SolarProtectionThresholdLux { get; private set; }
    public double SolarProtectionTemperatureC { get; private set; }
    public bool WindowOpenProtectionEnabled { get; private set; }
    public bool EmergencyLightingEnabled { get; private set; }
    public uint Version { get; private set; }

    private LightingSystemConfiguration() { }

    public LightingSystemConfiguration(Guid engineeringSystemId,
        double defaultBrightness = 500, double minBrightness = 50, double maxBrightness = 2000,
        double defaultCct = 4000, double morningCct = 2700, double dayCct = 6500,
        double eveningCct = 3500, double nightCct = 3000,
        bool circadian = true, bool daylightHarvesting = true, bool occupancyControl = true,
        int occupancyTimeout = 15, double solarProtectLux = 30000,
        double solarProtectTemp = 28, bool windowProtect = true, bool emergency = true)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engineeringSystemId;
        DefaultBrightnessLux = defaultBrightness; MinimumBrightnessLux = minBrightness;
        MaximumBrightnessLux = maxBrightness; DefaultColorTemperatureK = defaultCct;
        MorningColorTemperatureK = morningCct; DayColorTemperatureK = dayCct;
        EveningColorTemperatureK = eveningCct; NightColorTemperatureK = nightCct;
        CircadianEnabled = circadian; DaylightHarvestingEnabled = daylightHarvesting;
        OccupancyControlEnabled = occupancyControl;
        OccupancyTimeoutMinutes = occupancyTimeout;
        SolarProtectionThresholdLux = solarProtectLux;
        SolarProtectionTemperatureC = solarProtectTemp;
        WindowOpenProtectionEnabled = windowProtect;
        EmergencyLightingEnabled = emergency; Version = 1;
    }
}

public class LightingScene
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public SceneType SceneType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public double BrightnessLux { get; private set; }
    public double ColorTemperatureK { get; private set; }
    public double? ColorR { get; private set; }
    public double? ColorG { get; private set; }
    public double? ColorB { get; private set; }
    public double BlindPositionPct { get; private set; }
    public double CurtainPositionPct { get; private set; }
    public uint Version { get; private set; }

    private LightingScene() { }

    public LightingScene(Guid engId, SceneType sceneType,
        double brightnessLux = 500, double colorTempK = 4000,
        double blindPct = 0, double curtainPct = 100)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engId; SceneType = sceneType;
        Name = sceneType.ToString(); BrightnessLux = brightnessLux;
        ColorTemperatureK = colorTempK; BlindPositionPct = blindPct;
        CurtainPositionPct = curtainPct; Version = 1;
    }
}

public class BlindConfiguration
{
    public Guid Id { get; private set; }
    public Guid EngineeringSystemId { get; private set; }
    public BlindType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? MotorBindingId { get; private set; }
    public double MinimumPositionPct { get; private set; }
    public double MaximumPositionPct { get; private set; }
    public double CurrentPositionPct { get; private set; }
    public double SlatAngleDeg { get; private set; }
    public bool Enabled { get; private set; }
    public uint Version { get; private set; }

    private BlindConfiguration() { }

    public BlindConfiguration(Guid engId, string name, BlindType type,
        double minPct = 0, double maxPct = 100)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = engId; Name = name;
        Type = type; MinimumPositionPct = minPct; MaximumPositionPct = maxPct;
        CurrentPositionPct = 100; Enabled = true; Version = 1;
    }

    public void SetPosition(double pct, double slatAngle = 0)
    {
        CurrentPositionPct = Math.Clamp(pct, MinimumPositionPct, MaximumPositionPct);
        SlatAngleDeg = slatAngle; Version++;
    }
}
