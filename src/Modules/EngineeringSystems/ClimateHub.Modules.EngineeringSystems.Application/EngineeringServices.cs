using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class CapabilityRouter
{
    private readonly IEngineeringSystemRepository _systemRepo;

    public CapabilityRouter(IEngineeringSystemRepository systemRepo)
    {
        _systemRepo = systemRepo;
    }

    public async Task<CapabilityRoutingResult> RouteAsync(RoomId roomId, string needType,
        CancellationToken ct = default)
    {
        var engCapability = EngineeringCapabilityCodes.GetEngCapability(needType);
        if (engCapability is null)
            return new CapabilityRoutingResult(null, EngineeringErrors.EngineeringCapabilityNotFound,
                $"No engineering capability mapping for need type {needType}");

        var systems = await _systemRepo.GetByCapabilityAsync(engCapability, ct);
        if (systems.Count == 0)
            return new CapabilityRoutingResult(null, EngineeringErrors.EngineeringCapabilityNotFound,
                $"No engineering system supports {engCapability}");

        var candidates = systems
            .Where(s => s.Lifecycle == LifecycleStatus.Active || s.Lifecycle == LifecycleStatus.Commissioning)
            .Where(s => s.CoversRoom(roomId) || s.Zones.Count == 0)
            .Where(s => s.ControlMode != SystemControlMode.Off)
            .Where(s => s.OperationalStatus == OperationalStatus.Available || s.OperationalStatus == OperationalStatus.Degraded)
            .OrderBy(s => s.Priority)
            .ToList();

        if (candidates.Count == 0)
        {
            var hasSystemButNotCovering = systems.Any(s => s.Lifecycle == LifecycleStatus.Active);
            return new CapabilityRoutingResult(null,
                hasSystemButNotCovering ? EngineeringErrors.RoomNotCovered : EngineeringErrors.EngineeringSystemUnavailable,
                "All capable systems are disabled or don't cover this room");
        }

        if (candidates.Count > 1)
        {
            return new CapabilityRoutingResult(null, EngineeringErrors.AmbiguousSystem,
                $"Multiple systems support {engCapability} for room {roomId}, explicit selection required");
        }

        var selected = candidates[0];

        var coveringZone = selected.Zones
            .Where(z => z.ZoneRooms.Any(zr => zr.RoomId == roomId && zr.Enabled))
            .OrderBy(z => z.Priority)
            .FirstOrDefault();

        if (coveringZone is not null && coveringZone.PreferredEngineeringSystemId.HasValue && coveringZone.PreferredEngineeringSystemId.Value != selected.Id)
        {
            var preferred = await _systemRepo.GetByIdAsync(coveringZone.PreferredEngineeringSystemId.Value, ct);
            if (preferred is not null &&
                preferred.Lifecycle == LifecycleStatus.Active &&
                preferred.OperationalStatus == OperationalStatus.Available &&
                preferred.HasCapability(engCapability))
            {
                return new CapabilityRoutingResult(preferred, engCapability, null);
            }
        }

        var resourceCode = engCapability switch
        {
            EngineeringCapabilityCodes.IncreaseTemperature => "heating_power",
            EngineeringCapabilityCodes.DecreaseTemperature => "cooling_power",
            EngineeringCapabilityCodes.IncreaseHumidity => "steam_production",
            EngineeringCapabilityCodes.DecreaseHumidity => "dehumidification_capacity",
            EngineeringCapabilityCodes.ReduceCo2 => "airflow_capacity",
            EngineeringCapabilityCodes.IncreaseAirFlow => "airflow_capacity",
            _ => null
        };

        if (resourceCode is not null)
        {
            var resource = selected.GetResource(resourceCode);
            if (resource is null || !resource.CanAllocate(1))
            {
                return new CapabilityRoutingResult(null, EngineeringErrors.InsufficientResource,
                    $"Resource {resourceCode} exhausted for system {selected.Name}");
            }
        }

        return new CapabilityRoutingResult(selected, engCapability, null);
    }
}

public record CapabilityRoutingResult(EngineeringSystem? System, string? CapabilityCode, string? FailureReason)
{
    public string? FailureCode => FailureReason;
    public bool IsSuccess => System is not null;
}

public class StrategyEngine
{
    private readonly IStrategyRepository _strategyRepo;

    public StrategyEngine(IStrategyRepository strategyRepo)
    {
        _strategyRepo = strategyRepo;
    }

    public async Task<StrategyPlanResult> PlanAsync(EngineeringSystem system, string capabilityCode,
        double currentValue, double deviation, double desiredMax, CancellationToken ct = default)
    {
        var strategies = await _strategyRepo.GetAllAsync(ct);
        var activeStrategy = strategies.FirstOrDefault(s => s.IsActive && s.Type is StrategyType.Step or StrategyType.RuleBased or StrategyType.Linear)
                            ?? strategies.FirstOrDefault(s => s.Type == StrategyType.Step);

        if (activeStrategy is null)
            return new StrategyPlanResult(null, EngineeringErrors.StrategyNotFound, "No active strategy found");

        var requestedValue = activeStrategy.Type switch
        {
            StrategyType.Step => CalculateVentilationStep(capabilityCode, deviation, desiredMax),
            StrategyType.RuleBased => CalculateRuleBased(capabilityCode, deviation, desiredMax),
            StrategyType.Linear => CalculateLinear(capabilityCode, deviation, desiredMax),
            _ => CalculateVentilationStep(capabilityCode, deviation, desiredMax)
        };

        var deviceCodes = EngineeringCapabilityCodes.GetDeviceCodes(capabilityCode);
        var steps = BuildSteps(capabilityCode, requestedValue, deviceCodes);

        return new StrategyPlanResult(
            CommandPlan.Create(system.Id, capabilityCode, capabilityCode,
                requestedValue, "percent", activeStrategy.Name, steps),
            null, null);
    }

    private static double CalculateVentilationStep(string capabilityCode, double deviation, double desiredMax)
    {
        var target = desiredMax > 0 ? desiredMax : 1000;
        var ratio = Math.Clamp(deviation / target, 0, 1);
        return ratio switch
        {
            > 0.75 => 300,
            > 0.5 => 225,
            > 0.25 => 150,
            > 0.1 => 90,
            _ => 45
        };
    }

    private static double CalculateRuleBased(string capabilityCode, double deviation, double desiredMax)
    {
        var ratio = Math.Clamp(deviation / Math.Max(desiredMax, 1), 0, 1);
        return ratio switch
        {
            > 0.5 => 80,
            > 0.25 => 50,
            _ => 30
        };
    }

    private static double CalculateLinear(string capabilityCode, double deviation, double desiredMax)
    {
        var ratio = Math.Clamp(deviation / Math.Max(desiredMax, 1), 0, 1);
        return Math.Round(ratio * 100, 0);
    }

    private static List<CommandPlanStep> BuildSteps(string capabilityCode, double requestedValue, string[] deviceCodes)
    {
        if (capabilityCode == EngineeringCapabilityCodes.ReduceCo2 || capabilityCode == EngineeringCapabilityCodes.IncreaseAirFlow)
        {
            var fanPct = MapAirflowToFanPercent(requestedValue);
            var damperPct = MapAirflowToDamperPercent(requestedValue);

            return new List<CommandPlanStep>
            {
                new("control.damper-position", "set", damperPct, "percent", deviceRole: DeviceRole.SupplyDamper, sequence: 0),
                new("control.fan-speed", "set", fanPct, "percent", deviceRole: DeviceRole.SupplyFan, sequence: 1)
            };
        }

        return deviceCodes.Select((code, i) => new CommandPlanStep(
            code, "set", requestedValue, "percent", sequence: i)).ToList();
    }

    private static double MapAirflowToFanPercent(double airflow)
    {
        return airflow switch
        {
            >= 300 => 100,
            >= 225 => 75,
            >= 150 => 50,
            >= 90 => 30,
            _ => 15
        };
    }

    private static double MapAirflowToDamperPercent(double airflow)
    {
        return airflow switch
        {
            >= 300 => 100,
            >= 225 => 80,
            >= 150 => 55,
            >= 90 => 35,
            _ => 20
        };
    }
}

public record StrategyPlanResult(CommandPlan? Plan, string? FailureCode, string? FailureReason)
{
    public bool IsSuccess => Plan is not null;
}

public class ResourceManager
{
    private readonly IEngineeringSystemRepository _systemRepo;
    private readonly ICommandPlanRepository _planRepo;

    public ResourceManager(IEngineeringSystemRepository systemRepo, ICommandPlanRepository planRepo)
    {
        _systemRepo = systemRepo; _planRepo = planRepo;
    }

    public async Task<ResourceReservationResult> ReserveAsync(CommandPlan plan, CancellationToken ct = default)
    {
        var system = await _systemRepo.GetByIdAsync(plan.EngineeringSystemId, ct);
        if (system is null)
            return new ResourceReservationResult(false, EngineeringErrors.EngineeringSystemNotFound);

        var resourceAllocations = plan.CapabilityCode switch
        {
            EngineeringCapabilityCodes.ReduceCo2 or EngineeringCapabilityCodes.IncreaseAirFlow
                when system.GetResource("airflow_capacity") is not null =>
                new[] { ("airflow_capacity", plan.RequestedValue) },
            EngineeringCapabilityCodes.IncreaseHumidity or EngineeringCapabilityCodes.DecreaseHumidity
                when system.GetResource("steam_production") is not null =>
                new[] { ("steam_production", plan.RequestedValue / 100.0 * 10.0) },
            EngineeringCapabilityCodes.IncreaseTemperature
                when system.GetResource("heating_power") is not null =>
                new[] { ("heating_power", plan.RequestedValue / 100.0 * 5.0) },
            EngineeringCapabilityCodes.DecreaseTemperature
                when system.GetResource("cooling_power") is not null =>
                new[] { ("cooling_power", plan.RequestedValue / 100.0 * 5.0) },
            _ => Array.Empty<(string, double)>()
        };

        foreach (var (code, amount) in resourceAllocations)
        {
            var resource = system.GetResource(code);
            if (resource is null || !resource.CanAllocate(amount))
                return new ResourceReservationResult(false, EngineeringErrors.InsufficientResource);
        }

        foreach (var (code, amount) in resourceAllocations)
        {
            var resource = system.GetResource(code)!;
            resource.Reserve(amount);
            plan.AddResourceAllocation(code, amount);
        }

        plan.Reserve();
        await _systemRepo.UpdateAsync(system, ct);
        await _planRepo.UpdateAsync(plan, ct);
        return new ResourceReservationResult(true, null);
    }

    public async Task ReleaseAsync(CommandPlan plan, CancellationToken ct = default)
    {
        var system = await _systemRepo.GetByIdAsync(plan.EngineeringSystemId, ct);
        if (system is null) return;

        foreach (var allocation in plan.ResourceAllocations)
        {
            var resource = system.GetResource(allocation.ResourceCode);
            resource?.Release(allocation.Amount);
        }

        await _systemRepo.UpdateAsync(system, ct);
    }
}

public record ResourceReservationResult(bool Success, string? FailureCode);
