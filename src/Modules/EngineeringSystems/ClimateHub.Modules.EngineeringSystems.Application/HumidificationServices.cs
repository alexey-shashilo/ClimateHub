using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Humidification;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class CondensationProtectionResult
{
    public double DewPointC { get; init; }
    public double EstimatedSurfaceTemperatureC { get; init; }
    public double DewPointDeltaC { get; init; }
    public CondensationRisk Risk { get; init; }
    public double MaximumSafeRhPercent { get; init; }
    public bool BlockHumidification { get; init; }
    public string? Description { get; init; }
}

public class CondensationProtectionPlanner
{
    public CondensationProtectionResult Evaluate(
        double temperatureC, double rhPercent,
        double? surfaceTemperatureC, double? outdoorTempC,
        HumidificationSystemConfiguration config)
    {
        if (!config.CondensationProtectionEnabled)
            return new CondensationProtectionResult
            {
                DewPointC = 0,
                Risk = CondensationRisk.None,
                BlockHumidification = false,
                Description = "Condensation protection disabled"
            };

        var dewPoint = CalculateDewPoint(temperatureC, rhPercent);
        var surfaceTemp = surfaceTemperatureC ?? (outdoorTempC.HasValue
            ? EstimateSurfaceTemp(temperatureC, outdoorTempC.Value)
            : temperatureC - 2);
        var delta = Math.Abs(surfaceTemp - dewPoint);

        CondensationRisk risk;
        bool block;

        if (delta <= 1.0) { risk = CondensationRisk.Critical; block = true; }
        else if (delta <= 2.0) { risk = CondensationRisk.High; block = true; }
        else if (delta <= config.MaximumSurfaceDewPointDeltaC) { risk = CondensationRisk.Medium; block = false; }
        else if (delta <= config.MaximumSurfaceDewPointDeltaC * 2) { risk = CondensationRisk.Low; block = false; }
        else { risk = CondensationRisk.None; block = false; }

        var maxSafeRh = CalculateMaxSafeRh(temperatureC, surfaceTemp, config.MaximumSurfaceDewPointDeltaC);

        return new CondensationProtectionResult
        {
            DewPointC = Math.Round(dewPoint, 1),
            EstimatedSurfaceTemperatureC = Math.Round(surfaceTemp, 1),
            DewPointDeltaC = Math.Round(delta, 1),
            Risk = risk,
            MaximumSafeRhPercent = Math.Round(maxSafeRh, 0),
            BlockHumidification = block,
            Description = block
                ? $"Condensation risk {risk}: dew point {dewPoint:F1}°C, surface {surfaceTemp:F1}°C, delta {delta:F1}°C"
                : $"Safe: delta {delta:F1}°C, max safe RH {maxSafeRh:F0}%"
        };
    }

    public static double CalculateDewPoint(double tempC, double rhPercent)
    {
        if (rhPercent <= 0 || rhPercent > 100) return double.NaN;
        if (tempC < -40 || tempC > 50) return double.NaN;
        var a = 17.27; var b = 237.7;
        var gamma = (a * tempC) / (b + tempC) + Math.Log(rhPercent / 100.0);
        return (b * gamma) / (a - gamma);
    }

    public static double CalculateMaxSafeRh(double tempC, double surfaceTempC, double minDelta)
    {
        var safeDewPoint = surfaceTempC - minDelta;
        var a = 17.27; var b = 237.7;
        var gamma = (a * safeDewPoint) / (b + safeDewPoint);
        return Math.Exp(gamma - (a * tempC) / (b + tempC)) * 100;
    }

    private static double EstimateSurfaceTemp(double indoorTemp, double outdoorTemp)
    {
        var delta = indoorTemp - outdoorTemp;
        return indoorTemp - delta * 0.15;
    }
}

public class HumidityCalculationEngine
{
    public double CalculateAbsoluteHumidity(double tempC, double rhPercent)
    {
        var e = 6.112 * Math.Exp((17.67 * tempC) / (tempC + 243.5));
        var actualVaporPressure = e * (rhPercent / 100.0);
        return actualVaporPressure * 2.1674 / (273.15 + tempC);
    }

    public double CalculateRequiredMoisture(double supplyAirflowM3h,
        double currentRh, double targetRh, double supplyTempC, double roomTempC)
    {
        var supplyAh = CalculateAbsoluteHumidity(supplyTempC, currentRh);
        var targetAh = CalculateAbsoluteHumidity(roomTempC, targetRh);
        var deficit = targetAh - supplyAh;
        if (deficit <= 0) return 0;
        return deficit * supplyAirflowM3h / 1000.0;
    }

    public double SteamToPower(double steamKgH) => steamKgH * 0.75;
    public double WaterToSteam(double waterLh) => waterLh * 0.95;
}

public class HumidificationStrategyResult
{
    public double RequiredCapacityKgH { get; init; }
    public double SteamOutputPct { get; init; }
    public double NozzlePumpSpeedPct { get; init; }
    public CondensationProtectionResult? CondensationResult { get; init; }
    public List<CommandPlanStep> Steps { get; init; } = new();
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public bool IsSuccess => FailureCode is null;
}

public class HumidificationStrategyEngine
{
    private readonly CondensationProtectionPlanner _condensationPlanner;
    private readonly HumidityCalculationEngine _calcEngine;

    public HumidificationStrategyEngine(
        CondensationProtectionPlanner condensationPlanner,
        HumidityCalculationEngine calcEngine)
    {
        _condensationPlanner = condensationPlanner;
        _calcEngine = calcEngine;
    }

    public HumidificationStrategyResult PlanHumidification(
        HumidificationSystemConfiguration config,
        double currentRh, double targetRh, double roomTempC,
        double supplyAirTempC, double supplyAirflowM3h,
        double? outdoorTempC, double? surfaceTempC,
        HumidifierType humidifierType = HumidifierType.SteamElectrode)
    {
        if (config.Mode == HumidificationMode.Steam && !config.HasSteamGenerator)
            return Fail(EngineeringErrors.SteamGeneratorNotAvailable, "Steam generator not available");

        var condensation = _condensationPlanner.Evaluate(
            roomTempC, targetRh, surfaceTempC, outdoorTempC, config);

        if (condensation.BlockHumidification)
            return Fail(EngineeringErrors.CondensationRiskDetected,
                $"Condensation risk: {condensation.Description}");

        var requiredKgH = _calcEngine.CalculateRequiredMoisture(
            supplyAirflowM3h, currentRh, targetRh, supplyAirTempC, roomTempC);

        if (requiredKgH <= 0)
            return new HumidificationStrategyResult { RequiredCapacityKgH = 0, Steps = new List<CommandPlanStep>() };

        requiredKgH = Math.Clamp(requiredKgH, config.MinimumCapacityKgH, config.MaximumCapacityKgH);

        var steps = new List<CommandPlanStep>();
        var seq = 0;

        if (config.Mode == HumidificationMode.Steam || humidifierType == HumidifierType.SteamElectrode)
        {
            var steamPct = (requiredKgH / config.DesignCapacityKgH) * 100;
            steamPct = Math.Clamp(steamPct, 0, 100);

            steps.Add(new CommandPlanStep("control.steam-generator", "set",
                (int)steamPct, "percent",
                deviceRole: DeviceRole.SteamGenerator, sequence: seq++, required: true));

            steps.Add(new CommandPlanStep("control.steam-valve", "set",
                (int)steamPct, "percent",
                deviceRole: DeviceRole.SteamValve, sequence: seq++, required: true));

            if (config.HasWaterTreatment)
            {
                steps.Add(new CommandPlanStep("control.ro-system", "set", 100, "percent",
                    deviceRole: DeviceRole.ROSystem, sequence: seq++, required: false));
                steps.Add(new CommandPlanStep("control.uv-sterilizer", "set", 100, "percent",
                    deviceRole: DeviceRole.UVSterilizer, sequence: seq++, required: false));
            }

            return new HumidificationStrategyResult
            {
                RequiredCapacityKgH = requiredKgH,
                SteamOutputPct = steamPct,
                CondensationResult = condensation,
                Steps = steps
            };
        }
        else
        {
            var pumpSpeed = (requiredKgH / config.DesignCapacityKgH) * 100;
            pumpSpeed = Math.Clamp(pumpSpeed, 0, 100);

            steps.Add(new CommandPlanStep("control.nozzle-pump", "set",
                (int)pumpSpeed, "percent",
                deviceRole: DeviceRole.NozzlePump, sequence: seq++, required: true));

            steps.Add(new CommandPlanStep("control.nozzle-valve", "set",
                (int)pumpSpeed, "percent",
                deviceRole: DeviceRole.NozzleValve, sequence: seq++, required: true));

            return new HumidificationStrategyResult
            {
                RequiredCapacityKgH = requiredKgH,
                NozzlePumpSpeedPct = pumpSpeed,
                CondensationResult = condensation,
                Steps = steps
            };
        }
    }

    public List<CommandPlanStep> BuildSanitaryCycleSteps(
        HumidificationSystemConfiguration config, bool isFlush, bool isSterilization)
    {
        var steps = new List<CommandPlanStep>();
        var seq = 0;

        if (isFlush)
        {
            steps.Add(new CommandPlanStep("control.flush-valve", "set", 100, "percent",
                deviceRole: DeviceRole.FlushValve, sequence: seq++, required: true));
            steps.Add(new CommandPlanStep("control.drain-valve", "set", 100, "percent",
                deviceRole: DeviceRole.DrainValve, sequence: seq++, required: false));
        }

        if (isSterilization)
        {
            if (config.HasUVSterilization)
            {
                steps.Add(new CommandPlanStep("control.uv-sterilizer", "set", 100, "percent",
                    deviceRole: DeviceRole.UVSterilizer, sequence: seq++, required: true));
            }
            if (config.LegionellaProtectionTemperatureC > 0)
            {
                steps.Add(new CommandPlanStep("control.water-heater", "set",
                    (int)config.LegionellaProtectionTemperatureC, "celsius",
                    sequence: seq++, required: false));
            }
        }

        return steps;
    }

    private static HumidificationStrategyResult Fail(string code, string reason) =>
        new() { FailureCode = code, FailureReason = reason };
}

public class HumidificationDemandAggregator
{
    public HumidificationZone? ResolveZone(List<HumidificationZone> zones, RoomId roomId)
    {
        return zones.FirstOrDefault(z =>
            z.Enabled && z.Rooms.Any(r => r.RoomId == roomId && r.Enabled));
    }

    public double AggregateDemand(List<HumidificationDemand> demands,
        HumidificationSystemConfiguration config)
    {
        var sorted = demands
            .Where(d => d.Status == "Active")
            .OrderBy(d => d.Severity switch { "Critical" => 0, "High" => 1, "Medium" => 2, "Low" => 3, _ => 4 })
            .ThenBy(d => d.Priority)
            .ToList();

        var total = sorted.Sum(d => d.RequiredCapacityKgH);
        return Math.Min(total, config.MaximumCapacityKgH);
    }
}
