using ClimateHub.Modules.Climate.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Api.Endpoints;

public static class ClimateEndpoints
{
    public static void MapClimateEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/climate").WithTags("Climate");

        g.MapGet("/goals", async (IClimateModule climate, string? roomId, CancellationToken ct) =>
        {
            if (string.IsNullOrEmpty(roomId) || !Guid.TryParse(roomId, out var guid))
                return Results.Problem(statusCode: 400, detail: "ROOM_ID_REQUIRED");

            var goal = await climate.GetGoalForRoomAsync(RoomId.From(guid), ct);
            return Results.Ok(goal);
        });

        g.MapGet("/goals/building", async (IClimateModule climate, string? roomIds, CancellationToken ct) =>
        {
            if (string.IsNullOrEmpty(roomIds))
                return Results.Problem(statusCode: 400, detail: "ROOM_IDS_REQUIRED");

            var ids = roomIds.Split(',')
                .Select(r => r.Trim())
                .Where(r => Guid.TryParse(r, out _))
                .Select(r => RoomId.From(Guid.Parse(r)))
                .ToList();

            var results = new List<object>();
            foreach (var roomId in ids)
            {
                var goal = await climate.GetGoalForRoomAsync(roomId, ct);
                results.Add(new
                {
                    RoomId = roomId.ToString(),
                    Status = goal.Status,
                    SatisfactionPct = goal.SatisfactionPct,
                    NeedsAttention = goal.SatisfactionPct < 90
                });
            }
            return Results.Ok(results);
        });

        g.MapPost("/plan", async (IClimateModule climate, ClimatePlanRequest req, CancellationToken ct) =>
        {
            if (!Guid.TryParse(req.RoomId, out var guid))
                return Results.Problem(statusCode: 400, detail: "INVALID_ROOM_ID");

            var result = await climate.PlanRoomAsync(RoomId.From(guid), req.Profile ?? "Comfort", ct);
            if (result.Plan is null)
                return Results.Problem(statusCode: 500, detail: "Plan creation failed");

            return Results.Ok(new
            {
                PlanId = result.Plan.PlanId,
                Status = result.Plan.Status,
                Profile = result.Plan.Profile,
                SubPlanCount = result.Plan.SubPlans.Count,
                ConflictCount = result.Plan.Conflicts.Count,
                Conflicts = result.Plan.Conflicts.Select(c => new
                {
                    c.Reason, c.ConflictType, c.Resolution,
                    c.WinnerCapability, c.LoserCapability
                }),
                SubPlans = result.Plan.SubPlans.Select(sp => new
                {
                    CapabilityCode = sp.CapabilityCode,
                    Status = sp.Status,
                    Priority = sp.Priority,
                    ExecutionOrder = sp.ExecutionOrder
                })
            });
        });

        g.MapGet("/strategies", (IClimateModule climate) =>
        {
            var profiles = climate.GetStrategyProfiles();
            return Results.Ok(profiles);
        });

        g.MapPost("/check-conflicts", (IClimateModule climate, ConflictCheckRequest req) =>
        {
            var hasConflicts = climate.ConflictsExist(req.Capabilities ?? new List<string>());
            return Results.Ok(new { HasConflicts = hasConflicts });
        });

        g.MapPost("/priority", (IClimateModule climate, PriorityRequest req) =>
        {
            var higher = climate.IsHigherPriority(req.CapabilityA ?? "", req.CapabilityB ?? "", req.Profile ?? "Balanced");
            return Results.Ok(new { HigherPriority = higher ? req.CapabilityA : req.CapabilityB });
        });
    }
}

public record ClimatePlanRequest(string RoomId, string? Profile);
public record ConflictCheckRequest(List<string>? Capabilities);
public record PriorityRequest(string? CapabilityA, string? CapabilityB, string? Profile);