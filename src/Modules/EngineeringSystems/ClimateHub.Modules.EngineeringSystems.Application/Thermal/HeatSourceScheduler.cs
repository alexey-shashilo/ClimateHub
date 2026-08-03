using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

namespace ClimateHub.Modules.EngineeringSystems.Application.Thermal;

public class HeatSourceSchedule
{
    public List<HeatSource> SelectedSources { get; init; } = new();
    public List<double> RequestedOutputBySource { get; init; } = new();
    public double TotalAvailablePowerKw { get; set; }
    public double TotalScheduledPowerKw { get; set; }
    public double UnservedPowerKw { get; set; }
    public List<HeatSource> StartOrder { get; init; } = new();
    public List<HeatSource> StopOrder { get; set; } = new();
    public string? FailureCode { get; set; }
}

public class HeatSourceScheduler
{
    public HeatSourceSchedule Schedule(List<HeatSource> availableSources, double requiredPowerKw, double requiredSupplyTempC)
    {
        var result = new HeatSourceSchedule();
        var sortedSources = availableSources
            .Where(s => s.LifecycleStatus == LifecycleStatus.Active && s.RuntimeState != HeatSourceRuntimeState.Faulted)
            .OrderBy(s => s.Priority)
            .ThenByDescending(s => s.IsPrimary)
            .ThenByDescending(s => s.MaximumPowerKw)
            .ToList();

        if (sortedSources.Count == 0)
        {
            result.FailureCode = EngineeringErrors.HeatSourceNotConfigured;
            return result;
        }

        double remainingPower = requiredPowerKw;
        double totalAvailable = 0;

        foreach (var source in sortedSources)
        {
            if (remainingPower <= 0) break;

            if (source.RuntimeState == HeatSourceRuntimeState.Cooldown && source.CooldownUntil > DateTimeOffset.UtcNow)
                continue;

            if (source.RuntimeState == HeatSourceRuntimeState.Defrost)
                continue;

            if (source.IsMinimumOffTimeActive())
                continue;

            if (source.MaximumSupplyTemperatureC < requiredSupplyTempC)
                continue;

            totalAvailable += source.MaximumPowerKw;
            result.SelectedSources.Add(source);
            result.StartOrder.Add(source);

            var output = Math.Min(remainingPower, source.MaximumPowerKw);
            output = Math.Max(output, source.MinimumPowerKw);
            result.RequestedOutputBySource.Add(output);
            remainingPower -= output;
        }

        if (sortedSources.Any(s => s.RuntimeState == HeatSourceRuntimeState.Running && s.IsMinimumRuntimeActive()))
        {
            var runningSources = sortedSources.Where(s => s.RuntimeState == HeatSourceRuntimeState.Running).ToList();
            foreach (var source in runningSources)
            {
                if (!result.SelectedSources.Contains(source))
                {
                    result.SelectedSources.Add(source);
                    result.RequestedOutputBySource.Add(source.MinimumPowerKw);
                }
            }
        }

        result.TotalAvailablePowerKw = totalAvailable;
        result.TotalScheduledPowerKw = result.RequestedOutputBySource.Sum();
        result.UnservedPowerKw = Math.Max(0, remainingPower);
        result.StopOrder = sortedSources.OrderByDescending(s => s.Priority).ToList();

        return result;
    }
}