using ClimateHub.Modules.Needs.Domain;
using ClimateHub.Modules.Needs.Domain.Repositories;
using ClimateHub.Modules.Needs.Infrastructure;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Api.Endpoints;

public static class NeedsEndpoints
{
    public static void MapNeedsEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/needs").WithTags("Needs");

        g.MapGet("/", async (INeedRepository repo, [AsParameters] NeedQueryParams qp, CancellationToken ct) =>
        {
            var needs = string.IsNullOrEmpty(qp.RoomId)
                ? await repo.GetActiveAsync(ct)
                : await repo.GetByRoomAsync(RoomId.From(Guid.Parse(qp.RoomId)), ct);
            return Results.Ok(needs);
        });

        g.MapGet("/{needId}", async (string needId, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new NeedId(gid), ct);
            return need is null ? Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND") : Results.Ok(need);
        });

        g.MapGet("/rooms/{roomId}", async (string roomId, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var gid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var needs = await repo.GetByRoomAsync(RoomId.From(gid), ct);
            return Results.Ok(needs);
        });

        g.MapPost("/rooms/{roomId}/evaluate", async (string roomId, NeedEvaluationService eval, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var gid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            await eval.EvaluateRoomAsync(RoomId.From(gid), NeedEvaluationTrigger.ManualRequest, ct: ct);
            return Results.Ok(new { status = "evaluated" });
        });

        g.MapPost("/{needId}/evaluate", async (string needId, NeedEvaluationService eval, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            await eval.PlanAndExecuteAsync(need, NeedEvaluationTrigger.ManualRequest, ct: ct);
            return Results.Ok(new { status = "planned" });
        });

        g.MapPost("/{needId}/execute", async (string needId, NeedEvaluationService eval, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            if (need.Mode is ControlMode.Disabled)
                return Results.Problem(statusCode: 400, detail: "NEED_MODE_DISABLED");
            if (need.ActiveCommandId is not null)
                return Results.Problem(statusCode: 409, detail: "ACTIVE_COMMAND_EXISTS");
            if (need.Status is NeedStatus.Satisfied or NeedStatus.Cancelled or NeedStatus.Expired)
                return Results.Problem(statusCode: 400, detail: "NEED_ALREADY_RESOLVED");
            await eval.PlanAndExecuteAsync(need, NeedEvaluationTrigger.ManualRequest, ct: ct);
            return Results.Ok(new { status = "executing" });
        });

        g.MapPost("/{needId}/cancel", async (string needId, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            if (need.Status is NeedStatus.Satisfied or NeedStatus.Cancelled or NeedStatus.Expired)
                return Results.Problem(statusCode: 400, detail: "NEED_ALREADY_RESOLVED");
            need.Cancel();
            await repo.UpdateAsync(need, ct);
            return Results.Ok(new { status = "cancelled" });
        });

        g.MapPost("/{needId}/unblock", async (string needId, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            if (need.Status != NeedStatus.Blocked)
                return Results.Problem(statusCode: 400, detail: "NEED_NOT_BLOCKED");
            need.ClearBlock();
            await repo.UpdateAsync(need, ct);
            return Results.Ok(new { status = "unblocked" });
        });

        g.MapGet("/{needId}/evaluations", async (string needId, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var evals = await repo.GetEvaluationsAsync(new NeedId(gid), ct: ct);
            return Results.Ok(evals);
        });

        g.MapGet("/buildings/{buildingId}", async (string buildingId, INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(buildingId, out _)) return Results.Problem(statusCode: 400, detail: "BUILDING_NOT_FOUND");
            var needs = await repo.GetActiveAsync(ct);
            return Results.Ok(needs);
        });
    }
}

public record NeedQueryParams(string? RoomId, string? Type, string? Status, string? Severity, string? ControlMode, bool? ActiveOnly, bool? BlockedOnly);