using ClimateHub.Api.Authorization;

namespace ClimateHub.Api.Endpoints;

public static class NeedsEndpoints
{
    public static void MapNeedsEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/needs").WithTags("Needs");

        g.MapGet("/", async (ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, [AsParameters] NeedQueryParams qp, CancellationToken ct) =>
        {
            var needs = string.IsNullOrEmpty(qp.RoomId)
                ? await repo.GetActiveAsync(ct)
                : await repo.GetByRoomAsync(ClimateHub.SharedKernel.Primitives.RoomId.From(Guid.Parse(qp.RoomId)), ct);
            return Results.Ok(needs);
        }).RequirePermission("need_read");

        g.MapGet("/{needId}", async (string needId, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new ClimateHub.Modules.Needs.Domain.NeedId(gid), ct);
            return need is null ? Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND") : Results.Ok(need);
        }).RequirePermission("need_read");

        g.MapGet("/rooms/{roomId}", async (string roomId, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var gid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var needs = await repo.GetByRoomAsync(ClimateHub.SharedKernel.Primitives.RoomId.From(gid), ct);
            return Results.Ok(needs);
        }).RequirePermission("need_read");

        g.MapPost("/rooms/{roomId}/evaluate", async (string roomId, ClimateHub.Modules.Needs.Infrastructure.NeedEvaluationService eval, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var gid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            await eval.EvaluateRoomAsync(ClimateHub.SharedKernel.Primitives.RoomId.From(gid), ClimateHub.Modules.Needs.Domain.NeedEvaluationTrigger.ManualRequest, ct: ct);
            return Results.Ok(new { status = "evaluated" });
        }).RequirePermission("need_configure");

        g.MapPost("/{needId}/evaluate", async (string needId, ClimateHub.Modules.Needs.Infrastructure.NeedEvaluationService eval, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new ClimateHub.Modules.Needs.Domain.NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            await eval.PlanAndExecuteAsync(need, ClimateHub.Modules.Needs.Domain.NeedEvaluationTrigger.ManualRequest, ct: ct);
            return Results.Ok(new { status = "planned" });
        }).RequirePermission("need_configure");

        g.MapPost("/{needId}/execute", async (string needId, ClimateHub.Modules.Needs.Infrastructure.NeedEvaluationService eval, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new ClimateHub.Modules.Needs.Domain.NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            if (need.Mode is ClimateHub.Modules.Needs.Domain.ControlMode.Disabled)
                return Results.Problem(statusCode: 400, detail: "NEED_MODE_DISABLED");
            if (need.ActiveCommandId is not null)
                return Results.Problem(statusCode: 409, detail: "ACTIVE_COMMAND_EXISTS");
            if (need.Status is ClimateHub.Modules.Needs.Domain.NeedStatus.Satisfied or ClimateHub.Modules.Needs.Domain.NeedStatus.Cancelled or ClimateHub.Modules.Needs.Domain.NeedStatus.Expired)
                return Results.Problem(statusCode: 400, detail: "NEED_ALREADY_RESOLVED");
            await eval.PlanAndExecuteAsync(need, ClimateHub.Modules.Needs.Domain.NeedEvaluationTrigger.ManualRequest, ct: ct);
            return Results.Ok(new { status = "executing" });
        }).RequirePermission("need_execute");

        g.MapPost("/{needId}/cancel", async (string needId, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new ClimateHub.Modules.Needs.Domain.NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            if (need.Status is ClimateHub.Modules.Needs.Domain.NeedStatus.Satisfied or ClimateHub.Modules.Needs.Domain.NeedStatus.Cancelled or ClimateHub.Modules.Needs.Domain.NeedStatus.Expired)
                return Results.Problem(statusCode: 400, detail: "NEED_ALREADY_RESOLVED");
            need.Cancel();
            await repo.UpdateAsync(need, ct);
            return Results.Ok(new { status = "cancelled" });
        }).RequirePermission("need_configure");

        g.MapPost("/{needId}/unblock", async (string needId, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var need = await repo.GetByIdAsync(new ClimateHub.Modules.Needs.Domain.NeedId(gid), ct);
            if (need is null) return Results.Problem(statusCode: 404, detail: "NEED_NOT_FOUND");
            if (need.Status != ClimateHub.Modules.Needs.Domain.NeedStatus.Blocked)
                return Results.Problem(statusCode: 400, detail: "NEED_NOT_BLOCKED");
            need.ClearBlock();
            await repo.UpdateAsync(need, ct);
            return Results.Ok(new { status = "unblocked" });
        }).RequirePermission("need_configure");

        g.MapGet("/{needId}/evaluations", async (string needId, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(needId, out var gid)) return Results.Problem(statusCode: 400, detail: "NEED_NOT_FOUND");
            var evals = await repo.GetEvaluationsAsync(new ClimateHub.Modules.Needs.Domain.NeedId(gid), ct: ct);
            return Results.Ok(evals);
        }).RequirePermission("need_read");

        g.MapGet("/buildings/{buildingId}", async (string buildingId, ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(buildingId, out _)) return Results.Problem(statusCode: 400, detail: "BUILDING_NOT_FOUND");
            var needs = await repo.GetActiveAsync(ct);
            return Results.Ok(needs);
        }).RequireBuildingAccess();
    }
}

public record NeedQueryParams(string? RoomId, string? Type, string? Status, string? Severity, string? ControlMode, bool? ActiveOnly, bool? BlockedOnly);