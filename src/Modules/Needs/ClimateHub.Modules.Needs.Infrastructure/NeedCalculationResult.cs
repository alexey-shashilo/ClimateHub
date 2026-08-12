using ClimateHub.Modules.Needs.Domain;

namespace ClimateHub.Modules.Needs.Infrastructure;

public record NeedCalculationResult(IReadOnlyCollection<Need> Needs, DateTimeOffset CalculatedAt);
