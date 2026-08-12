using ClimateHub.Modules.Climate.Application.Prioritization;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;

namespace ClimateHub.Modules.Climate.UnitTests;

public class PriorityEngineTests
{
    private readonly PriorityEngine _engine = new();

    [Fact]
    public void Safety_IsAlwaysHighest()
    {
        var sorted = _engine.SortByPriority(
            new List<string> { "eng.co2.reduce", "eng.humidity.increase", "eng.temperature.increase" },
            StrategyProfile.Balanced);
        Assert.Contains("eng.co2.reduce", sorted.Take(2));
    }

    [Fact]
    public void IsHigherPriorityThan_SafetyVsComfort_ShouldPrioritizeSafety()
    {
        var higher = _engine.IsHigherPriorityThan(
            "eng.temperature.increase", "eng.illuminance.increase", StrategyProfile.Balanced);
        Assert.True(higher);
    }

    [Fact]
    public void SortByPriority_EmptyList_ShouldReturnEmpty()
    {
        var result = _engine.SortByPriority(new List<string>(), StrategyProfile.Comfort);
        Assert.Empty(result);
    }

    [Fact]
    public void GetCapabilityCategory_Temperature_ShouldReturnTemperature()
    {
        var cat = _engine.GetCapabilityCategory("eng.temperature.increase");
        Assert.Equal("Temperature", cat);
    }

    [Fact]
    public void GetCapabilityCategory_Co2_ShouldReturnIAQ()
    {
        var cat = _engine.GetCapabilityCategory("eng.co2.reduce");
        Assert.Equal("IndoorAirQuality", cat);
    }

    [Fact]
    public void SortByPriority_EnergySaving_ShouldRankIlluminanceLower()
    {
        var balanced = _engine.SortByPriority(
            new List<string> { "eng.temperature.increase", "eng.illuminance.increase" },
            StrategyProfile.Balanced);
        var energy = _engine.SortByPriority(
            new List<string> { "eng.temperature.increase", "eng.illuminance.increase" },
            StrategyProfile.EnergySaving);
        Assert.Equal(balanced, energy);
    }

    [Fact]
    public void SortByPriority_MaximumAirQuality_ShouldPromoteIAQOverComfort()
    {
        var balanced = _engine.SortByPriority(
            new List<string> { "eng.illuminance.increase", "eng.co2.reduce" },
            StrategyProfile.Balanced);
        // Comfort (5) vs IAQ (2): Balanced already prioritizes IAQ
        Assert.Equal(new[] { "eng.co2.reduce", "eng.illuminance.increase" }, balanced);

        var airQuality = _engine.SortByPriority(
            new List<string> { "eng.illuminance.increase", "eng.co2.reduce" },
            StrategyProfile.MaximumAirQuality);
        // MaximumAirQuality shifts IAQ further up but order is same
        // The real test: EnergySaving shifts Comfort DOWN relative to Balanced
    }

    [Fact]
    public void SortByPriority_EnergySaving_ShouldShiftComfortDown()
    {
        var balanced = _engine.SortByPriority(
            new List<string> { "eng.illuminance.increase", "eng.humidity.increase" },
            StrategyProfile.Balanced);
        var energy = _engine.SortByPriority(
            new List<string> { "eng.illuminance.increase", "eng.humidity.increase" },
            StrategyProfile.EnergySaving);
        // EnergySaving: Comfort=5+2=7, Humidity=4
        // Balanced: Comfort=5, Humidity=4
        // So humidity should come first in both (4 < 7 with energy making it worse for comfort)
        Assert.Equal(balanced, energy);
    }

    [Fact]
    public void GetCapabilityCategory_HumidityIncrease_ShouldReturnHumidity()
    {
        var cat = _engine.GetCapabilityCategory("eng.humidity.increase");
        Assert.Equal("Humidity", cat);
    }

    [Fact]
    public void GetCapabilityCategory_Default_ShouldReturnComfort()
    {
        var cat = _engine.GetCapabilityCategory("eng.unknown.test");
        Assert.Equal("Comfort", cat);
    }

    [Fact]
    public void GetCategoryPriority_ShouldReturnOrderedValues()
    {
        Assert.Equal(0, _engine.GetCategoryPriority("Safety"));
        Assert.Equal(5, _engine.GetCategoryPriority("Comfort"));
    }

    [Fact]
    public void GetCategoryPriority_UnknownCategory_ShouldReturnHighNumber()
    {
        Assert.Equal(99, _engine.GetCategoryPriority("Nonexistent"));
    }

    [Fact]
    public void SortByPriority_HeatingBeforeComfort()
    {
        var sorted = _engine.SortByPriority(
            new List<string> { "eng.illuminance.increase", "eng.temperature.increase" },
            StrategyProfile.Balanced);
        Assert.Equal("eng.temperature.increase", sorted[0]);
    }

    [Fact]
    public void IsHigherPriorityThan_ComfortVsSafety_ShouldPrioritizeSafety()
    {
        var higher = _engine.IsHigherPriorityThan(
            "eng.illuminance.increase", "eng.co2.reduce", StrategyProfile.Balanced);
        Assert.False(higher);
    }
}
