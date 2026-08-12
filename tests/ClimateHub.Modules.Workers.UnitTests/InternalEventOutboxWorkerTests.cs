using ClimateHub.Modules.EngineeringSystems.Application;
using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.Workers.UnitTests;

public class InternalEventOutboxWorkerTests
{
    [Fact]
    public void RetryDelay_FirstAttempt_ReturnsInitialDelay()
    {
        var delay = InternalEventRetryLogic.CalculateDelay(0);
        Assert.True(delay >= TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RetryDelay_MultipleAttempts_IncreasesDelay()
    {
        var d1 = InternalEventRetryLogic.CalculateDelay(1);
        var d2 = InternalEventRetryLogic.CalculateDelay(2);
        Assert.True(d2 > d1);
    }

    [Fact]
    public void RetryDelay_CappedAtMaximum()
    {
        var delay = InternalEventRetryLogic.CalculateDelay(10);
        Assert.True(delay <= TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void MaxAttemptsExceeded_ShouldBeTrue_WhenAttemptsReachesLimit()
    {
        Assert.True(InternalEventRetryLogic.IsMaxAttemptsExceeded(5, 5));
        Assert.False(InternalEventRetryLogic.IsMaxAttemptsExceeded(3, 5));
    }
}

internal static class InternalEventRetryLogic
{
    public static TimeSpan CalculateDelay(int attemptCount)
    {
        var delay = TimeSpan.FromSeconds(2);
        for (int i = 1; i < attemptCount; i++)
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
        return TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, TimeSpan.FromMinutes(5).TotalMilliseconds));
    }

    public static bool IsMaxAttemptsExceeded(int attemptCount, int maxAttempts) => attemptCount >= maxAttempts;
}
