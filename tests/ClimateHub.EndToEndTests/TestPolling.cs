#nullable disable
using System.Text.Json;

namespace ClimateHub.EndToEndTests;

public static class TestPolling
{
#nullable enable
    public static async Task<T?> EventuallyAsync<T>(Func<Task<T?>> poll, Func<T?, bool> condition,
        TimeSpan timeout, TimeSpan? interval = null) where T : class
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var pollInterval = interval ?? TimeSpan.FromSeconds(1);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = await poll();
            if (condition(result))
                return result;
            await Task.Delay(pollInterval);
        }

        return await poll();
    }

    public static async Task<bool> EventuallyTrueAsync(Func<Task<bool>> poll,
        TimeSpan timeout, TimeSpan? interval = null)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var pollInterval = interval ?? TimeSpan.FromSeconds(1);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await poll())
                return true;
            await Task.Delay(pollInterval);
        }

        return await poll();
    }
#nullable disable
}
