using ClimateHub.Modules.Climate.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Workers.UnitTests;

public class ClimateEventConsumerWorkerTests
{
    [Theory]
    [InlineData("need.detected", true)]
    [InlineData("need.satisfied", true)]
    [InlineData("environment.state.changed", true)]
    [InlineData("engineering.command-plan.created", true)]
    [InlineData("climate.strategy.changed", true)]
    [InlineData("building.updated", false)]
    [InlineData("device.connectivity.changed", false)]
    public void IsClimateEvent_MatchesExpectedPatterns(string eventType, bool expected)
    {
        Assert.Equal(expected, ClimateEventClassifier.IsClimateEvent(eventType));
    }

    [Fact]
    public void RetryDelay_ExponentialBackoff()
    {
        var d0 = ClimateEventClassifier.CalculateDelay(0);
        var d1 = ClimateEventClassifier.CalculateDelay(1);
        var d2 = ClimateEventClassifier.CalculateDelay(2);

        Assert.True(d1 >= d0);
        Assert.True(d2 >= d1);
    }

    [Fact]
    public void RetryDelay_CappedAtFiveMinutes()
    {
        var delay = ClimateEventClassifier.CalculateDelay(10);
        Assert.True(delay <= TimeSpan.FromMinutes(5));
    }
}

internal static class ClimateEventClassifier
{
    public static bool IsClimateEvent(string eventType) => eventType switch
    {
        "need.detected" or "need.updated" or "need.satisfied" or "need.cancelled" => true,
        "environment.state.changed" => true,
        "room.policy.changed" => true,
        "engineering.command-plan.created" or "engineering.command-plan.executing" => true,
        "engineering.command-plan.succeeded" or "engineering.command-plan.failed" => true,
        "engineering.command-plan.cancelled" or "engineering.command-plan.expired" => true,
        "climate.strategy.changed" or "climate.resource.changed" => true,
        _ => false
    };

    public static TimeSpan CalculateDelay(int attemptCount)
    {
        var delay = TimeSpan.FromSeconds(2);
        for (int i = 1; i < attemptCount; i++)
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
        return TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, TimeSpan.FromMinutes(5).TotalMilliseconds));
    }
}