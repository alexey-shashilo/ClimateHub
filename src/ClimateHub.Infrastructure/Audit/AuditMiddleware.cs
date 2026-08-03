using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ClimateHub.Infrastructure.Audit;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogRepository auditLogRepository)
    {
        var stopwatch = Stopwatch.StartNew();

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sessionId = context.User.FindFirst("session_id")?.Value;
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        var buildingId = context.Request.RouteValues.TryGetValue("buildingId", out var bId) ? bId?.ToString() : null;

        await _next(context);

        stopwatch.Stop();

        var statusCode = context.Response.StatusCode;
        var outcome = statusCode switch
        {
            >= 200 and < 300 => "success",
            >= 400 and < 500 => "client_error",
            >= 500 => "server_error",
            _ => "unknown"
        };

        var auditEvent = new AuditEvent(
            auditEventId: Guid.NewGuid(),
            actorType: userId is not null ? "user" : "anonymous",
            actorId: userId ?? "anonymous",
            action: $"{context.Request.Method} {context.Request.Path}",
            outcome: outcome,
            sessionId: sessionId,
            buildingId: buildingId,
            resourceType: context.Request.RouteValues.TryGetValue("controller", out var ctrl) ? ctrl?.ToString() : null,
            resourceId: context.Request.RouteValues.TryGetValue("id", out var id) ? id?.ToString() : null,
            reasonCode: statusCode >= 400 ? statusCode.ToString() : null,
            ipAddress: ipAddress,
            userAgent: userAgent,
            correlationId: correlationId,
            traceId: traceId);

        await auditLogRepository.AppendAsync(auditEvent, context.RequestAborted);
    }
}