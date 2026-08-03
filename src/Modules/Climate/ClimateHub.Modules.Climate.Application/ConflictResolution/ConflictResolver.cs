using ClimateHub.Modules.Climate.Application.EffectModel;
using ClimateHub.Modules.Climate.Application.Prioritization;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;

namespace ClimateHub.Modules.Climate.Application.ConflictResolution;

public record ConflictResolutionResult(
    List<string> ResolvedCapabilities,
    List<string> BlockedCapabilities,
    List<ClimateConflict> Conflicts);

public class ConflictResolver
{
    private readonly CrossSystemEffectModel _effectModel;
    private readonly PriorityEngine _priorityEngine;

    public ConflictResolver(CrossSystemEffectModel effectModel, PriorityEngine priorityEngine)
    {
        _effectModel = effectModel;
        _priorityEngine = priorityEngine;
    }

    public ConflictResolutionResult Resolve(List<string> requestedCapabilities, StrategyProfile profile)
    {
        var conflicts = new List<ClimateConflict>();
        var resolvedCapabilities = new List<string>(requestedCapabilities);
        var blocked = new List<string>();

        // Check for direct conflicts: heating + cooling, humidification + dehumidification
        var directPairs = new List<(string, string, string)>
        {
            ("eng.temperature.increase", "eng.temperature.decrease", "Heating and Cooling conflict"),
            ("eng.humidity.increase", "eng.humidity.decrease", "Humidification and Dehumidification conflict"),
        };

        foreach (var (a, b, description) in directPairs)
        {
            if (resolvedCapabilities.Contains(a) && resolvedCapabilities.Contains(b))
            {
                var winner = _priorityEngine.IsHigherPriorityThan(a, b, profile) ? a : b;
                var loser = winner == a ? b : a;

                conflicts.Add(new ClimateConflict(Guid.Empty, ConflictType.DirectOpposition,
                    a, b, winner, loser, $"{winner} blocked {loser}", description));
                resolvedCapabilities.Remove(loser);
                blocked.Add(loser);
            }
        }

        // Check for cross-effects: capability that hurts another's goal
        foreach (var capability in requestedCapabilities)
        {
            foreach (var other in requestedCapabilities)
            {
                if (capability == other) continue;
                if (resolvedCapabilities.Contains(capability) && resolvedCapabilities.Contains(other))
                {
                    if (_effectModel.HasNegativeEffect(capability, other))
                    {
                        var winner = _priorityEngine.IsHigherPriorityThan(other, capability, profile) ? other : capability;
                        var loser = winner == other ? capability : other;

                        conflicts.Add(new ClimateConflict(Guid.Empty, ConflictType.CrossSystemNegativeEffect,
                            capability, other, winner, loser, $"{winner} prioritized over {loser}", 
                            $"{capability} negatively affects {other}"));

                        if (loser == other)
                            resolvedCapabilities.Remove(other);
                        else
                            resolvedCapabilities.Remove(capability);
                    }
                }
            }
        }

        return new ConflictResolutionResult(resolvedCapabilities, blocked, conflicts);
    }
}