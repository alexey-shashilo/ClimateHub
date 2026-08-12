namespace ClimateHub.Modules.Workers.UnitTests;

public class TelemetryOutboxWorkerTests
{
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 30)]
    [InlineData(4, 60)]
    public void CalculateRetryDelay_ReturnsExpectedBaseSeconds(int attempt, int expectedSeconds)
    {
        var delay = TelemetryRetryLogic.CalculateDelay(attempt);
        Assert.True(delay >= TimeSpan.FromSeconds(expectedSeconds * 0.85));
        Assert.True(delay <= TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData("connection refused", true)]
    [InlineData("operation timeout error", true)]
    [InlineData("invalid data", false)]
    [InlineData("some other error", false)]
    public void IsRetryable_MatchesExpected(string message, bool expected)
    {
        Assert.Equal(expected, TelemetryRetryLogic.IsRetryable(new InvalidOperationException(message)));
    }
}

internal static class TelemetryRetryLogic
{
    public static TimeSpan CalculateDelay(int attemptCount)
    {
        var baseDelay = attemptCount switch
        {
            0 => TimeSpan.FromSeconds(2),
            1 => TimeSpan.FromSeconds(5),
            2 => TimeSpan.FromSeconds(10),
            3 => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromMinutes(1),
        };
        return baseDelay > TimeSpan.FromMinutes(5) ? TimeSpan.FromMinutes(5) : baseDelay;
    }

    public static bool IsRetryable(Exception ex)
    {
        var msg = ex.Message;
        return msg.Contains("connect", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("refused", StringComparison.OrdinalIgnoreCase);
    }
}
