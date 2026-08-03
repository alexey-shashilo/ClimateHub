using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace ClimateHub.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();

    private static readonly HashSet<string> SensitivePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login",
        "/api/v1/auth/refresh",
    };

    private static readonly HashSet<string> ProtectedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/commands/",
        "/api/v1/devices/register",
        "/api/v1/sse/",
    };

    private const int PermitLimit = 10;
    private const int WindowSeconds = 60;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var isSensitive = SensitivePaths.Contains(path);
        var isProtected = ProtectedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isSensitive && !isProtected)
        {
            await _next(context);
            return;
        }

        var key = GetClientKey(context);
        var limiter = _limiters.GetOrAdd(key, _ =>
            new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = isSensitive ? 5 : PermitLimit,
                Window = TimeSpan.FromSeconds(WindowSeconds),
                SegmentsPerWindow = isSensitive ? 1 : 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

        using var lease = await limiter.AcquireAsync(permitCount: 1);

        if (!lease.IsAcquired)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = WindowSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests",
                message = "Rate limit exceeded. Please try again later.",
                retryAfterSeconds = WindowSeconds
            });
            return;
        }

        await _next(context);
    }

    private static string GetClientKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var auth = context.Request.Headers.Authorization.FirstOrDefault() ?? "";
        return $"{ip}:{auth.GetHashCode()}";
    }
}