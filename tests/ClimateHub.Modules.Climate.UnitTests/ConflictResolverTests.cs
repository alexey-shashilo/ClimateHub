using ClimateHub.Modules.Climate.Application.ConflictResolution;
using ClimateHub.Modules.Climate.Application.EffectModel;
using ClimateHub.Modules.Climate.Application.Prioritization;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;

namespace ClimateHub.Modules.Climate.UnitTests;

public class ConflictResolverTests
{
    private readonly ConflictResolver _resolver;

    public ConflictResolverTests()
    {
        var model = new CrossSystemEffectModel();
        var priority = new PriorityEngine();
        _resolver = new ConflictResolver(model, priority);
    }

    [Fact]
    public void HeatingAndCooling_ShouldResolveToWinner()
    {
        var result = _resolver.Resolve(
            new List<string> { "eng.temperature.increase", "eng.temperature.decrease" },
            StrategyProfile.Balanced);
        Assert.Contains("eng.temperature.increase", result.ResolvedCapabilities);
        Assert.Contains("eng.temperature.decrease", result.BlockedCapabilities);
        Assert.Single(result.Conflicts);
    }

    [Fact]
    public void HumidificationAndDehumidification_ShouldResolveToOne()
    {
        var result = _resolver.Resolve(
            new List<string> { "eng.humidity.increase", "eng.humidity.decrease" },
            StrategyProfile.Balanced);
        Assert.Single(result.ResolvedCapabilities);
        Assert.Single(result.BlockedCapabilities);
    }

    [Fact]
    public void SingleCapability_ShouldReturnUnchanged()
    {
        var result = _resolver.Resolve(
            new List<string> { "eng.co2.reduce" },
            StrategyProfile.Balanced);
        Assert.Single(result.ResolvedCapabilities);
        Assert.Empty(result.BlockedCapabilities);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void EmptyList_ShouldReturnEmpty()
    {
        var result = _resolver.Resolve(new List<string>(), StrategyProfile.Comfort);
        Assert.Empty(result.ResolvedCapabilities);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void EqualPriorityCapabilities_ShouldResolveDeterministically()
    {
        var result1 = _resolver.Resolve(
            new List<string> { "eng.temperature.increase", "eng.temperature.decrease" },
            StrategyProfile.Balanced);
        var result2 = _resolver.Resolve(
            new List<string> { "eng.temperature.increase", "eng.temperature.decrease" },
            StrategyProfile.Balanced);
        Assert.Equal(result1.ResolvedCapabilities[0], result2.ResolvedCapabilities[0]);
    }

    [Fact]
    public void CrossEffect_IlluminanceDecreaseOnTemperatureIncrease_ShouldDetectConflict()
    {
        var result = _resolver.Resolve(
            new List<string> { "eng.temperature.increase", "eng.illuminance.decrease" },
            StrategyProfile.Balanced);
        Assert.Single(result.ResolvedCapabilities);
        Assert.NotEmpty(result.Conflicts);
    }

    [Fact]
    public void CrossEffect_VentilationAndHeating_ShouldResolve()
    {
        var result = _resolver.Resolve(
            new List<string> { "eng.airflow.increase", "eng.temperature.increase" },
            StrategyProfile.Balanced);
        Assert.NotEmpty(result.ResolvedCapabilities);
    }

    [Fact]
    public void ThreeWayConflict_HeatingBoth_ShouldResolveDirect()
    {
        var result = _resolver.Resolve(
            new List<string> { "eng.temperature.increase", "eng.temperature.decrease", "eng.illuminance.increase" },
            StrategyProfile.Balanced);
        Assert.True(result.ResolvedCapabilities.Count >= 2);
    }

    [Fact]
    public void ThreeWayConflict_HeatingAndHumidity_ShouldResolve()
    {
        var result = _resolver.Resolve(
            new List<string> { "eng.temperature.increase", "eng.humidity.increase", "eng.airflow.increase" },
            StrategyProfile.Balanced);
        Assert.True(result.ResolvedCapabilities.Count >= 1);
    }
}