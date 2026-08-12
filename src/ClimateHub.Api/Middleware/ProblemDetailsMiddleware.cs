using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Api.Middleware;

public static class ProblemDetailsMiddleware
{
    private static readonly string[] SensitivePatterns =
    [
        "Authorization",
        "refresh_token",
        "password",
        "MQTT_SECRET",
        "mqtt__password",
        "ConnectionString",
        "INFLUXDB_TOKEN",
        "InfluxDb__Token",
        "SigningKey",
        "PRIVATE_KEY",
    ];

    public static IApplicationBuilder UseClimateHubProblemDetails(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;

                var (statusCode, code) = exception switch
                {
                    KeyNotFoundException => (404, context.Request.Path.StartsWithSegments("/api/v1/devices")
                        ? "DEVICE_NOT_FOUND"
                        : context.Request.Path.StartsWithSegments("/api/v1/buildings")
                            ? "BUILDING_NOT_FOUND"
                            : context.Request.Path.StartsWithSegments("/api/v1/floors")
                                ? "FLOOR_NOT_FOUND"
                                : context.Request.Path.StartsWithSegments("/api/v1/rooms")
                                    ? "ROOM_NOT_FOUND"
                                    : "NOT_FOUND"),
                    InvalidOperationException => (409, "CONCURRENCY_CONFLICT"),
                    DbUpdateConcurrencyException => (409, "CONCURRENCY_CONFLICT"),
                    ArgumentException => (400, "INVALID_ARGUMENT"),
                    _ => (500, "INTERNAL_ERROR")
                };

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                var detail = exception?.Message ?? code;
                detail = RedactSecrets(detail);

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Type = $"https://climate-hub.local/errors/{code}",
                    Title = code,
                    Status = statusCode,
                    Detail = detail,
                    Instance = context.Request.Path
                });
            });
        });

        return app;
    }

    public static string RedactSecrets(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;

        foreach (var pattern in SensitivePatterns)
        {
            var index = message.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var colonIndex = message.IndexOf(':', index);
                if (colonIndex > 0)
                {
                    var endOfValue = message.IndexOfAny(['\n', '\r', ' '], colonIndex);
                    if (endOfValue < 0) endOfValue = message.Length;
                    message = message[..(colonIndex + 1)] + " ***REDACTED***" + message[endOfValue..];
                }
            }
        }

        return message;
    }
}
