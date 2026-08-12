using ClimateHub.Api.Authorization;
using ClimateHub.Modules.EngineeringSystems.Application;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.IAM.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Api.Endpoints;

public static class EngineeringSystemsEndpoints
{
    public static void MapEngineeringSystemsEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/engineering-systems").WithTags("Engineering Systems");

        g.MapGet("/", async (IEngineeringSystemRepository repo, CancellationToken ct) =>
        {
            var systems = await repo.GetAllAsync(ct);
            return Results.Ok(systems.Select(MapToDto));
        }).RequirePermission("engineering_read");

        g.MapGet("/{id}", async (string id, IEngineeringSystemRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.Problem(statusCode: 400, detail: "INVALID_ID");
            var system = await repo.GetByIdAsync(EngineeringSystemId.From(guid), ct);
            return system is null ? Results.Problem(statusCode: 404) : Results.Ok(MapToDto(system));
        }).RequireEngineeringSystemAccess().RequirePermission("engineering_read");

        g.MapGet("/{id}/resources", async (string id, IEngineeringSystemRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.Problem(statusCode: 400);
            var system = await repo.GetByIdAsync(EngineeringSystemId.From(guid), ct);
            return system is null ? Results.Problem(statusCode: 404) : Results.Ok(system.Resources.Select(r => new
            {
                r.Code,
                r.Unit,
                r.Maximum,
                r.Available,
                r.Reserved,
                r.Used,
                r.Priority
            }));
        }).RequireEngineeringSystemAccess().RequirePermission("engineering_read");

        g.MapGet("/{id}/zones", async (string id, IEngineeringSystemRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.Problem(statusCode: 400);
            var system = await repo.GetByIdAsync(EngineeringSystemId.From(guid), ct);
            return system is null ? Results.Problem(statusCode: 404) : Results.Ok(system.Zones.Select(z => new
            {
                z.Id,
                z.Name,
                z.Priority,
                RoomIds = z.ZoneRooms.Select(zr => zr.RoomId.ToString())
            }));
        }).RequireEngineeringSystemAccess().RequirePermission("engineering_read");

        g.MapGet("/{id}/devices", async (string id, IEngineeringSystemRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.Problem(statusCode: 400);
            var system = await repo.GetByIdAsync(EngineeringSystemId.From(guid), ct);
            return system is null ? Results.Problem(statusCode: 404) : Results.Ok(system.DeviceBindings.Select(b => new
            {
                b.Id,
                b.DeviceId,
                b.Role,
                b.Priority,
                b.Enabled
            }));
        }).RequireEngineeringSystemAccess().RequirePermission("engineering_read");

        g.MapGet("/{id}/plans", async (string id, ICommandPlanRepository planRepo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.Problem(statusCode: 400);
            var plans = await planRepo.GetBySystemAsync(EngineeringSystemId.From(guid), ct);
            return Results.Ok(plans.Select(MapPlanToDto));
        }).RequireEngineeringSystemAccess().RequirePermission("engineering_read");

        g.MapGet("/{id}/status", async (string id, IEngineeringSystemRepository sysRepo, ICommandPlanRepository planRepo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.Problem(statusCode: 400);
            var system = await sysRepo.GetByIdAsync(EngineeringSystemId.From(guid), ct);
            if (system is null) return Results.Problem(statusCode: 404);

            var activePlans = await planRepo.GetBySystemAsync(EngineeringSystemId.From(guid), ct);
            return Results.Ok(new
            {
                system.Id,
                system.Name,
                system.SystemType,
                Lifecycle = system.Lifecycle.ToString(),
                OperationalStatus = system.OperationalStatus.ToString(),
                system.ControlMode,
                Resources = system.Resources.Select(r => new { r.Code, r.Maximum, r.Available, r.Reserved, r.Used }),
                ActivePlanCount = activePlans.Count(p => p.Status == CommandPlanStatus.Executing || p.Status == CommandPlanStatus.Reserved || p.Status == CommandPlanStatus.Allocated)
            });
        }).RequireEngineeringSystemAccess().RequirePermission("engineering_read");

        // Command Plans
        var cp = app.MapGroup("/api/v1/command-plans").WithTags("Command Plans");
        cp.MapGet("/", async (ICommandPlanRepository repo, CancellationToken ct) =>
        {
            var plans = await repo.GetActiveAsync(ct);
            return Results.Ok(plans.Select(MapPlanToDto));
        }).RequirePermission("engineering_read");

        cp.MapGet("/{id}", async (Guid id, ICommandPlanRepository repo, CancellationToken ct) =>
        {
            var plan = await repo.GetByIdAsync(id, ct);
            return plan is null ? Results.Problem(statusCode: 404) : Results.Ok(MapPlanToDto(plan));
        }).RequirePermission("engineering_read");

        // Resources
        var res = app.MapGroup("/api/v1/resources").WithTags("Resources");
        res.MapGet("/", async (IEngineeringSystemRepository repo, CancellationToken ct) =>
        {
            var systems = await repo.GetAllAsync(ct);
            return Results.Ok(systems.SelectMany(s => s.Resources.Select(r => new
            {
                SystemId = s.Id.ToString(),
                SystemName = s.Name,
                r.Code,
                r.Unit,
                r.Maximum,
                r.Available,
                r.Reserved,
                r.Used,
                r.Priority
            })));
        }).RequirePermission("engineering_read");

        // POST Engineering System
        g.MapPost("/", async (CreateEngineeringSystemRequest req, IEngineeringSystemRepository repo, IBuildingAccessGrantRepository grantRepo, CancellationToken ct, HttpContext httpCtx) =>
        {
            if (!Guid.TryParse(req.BuildingId, out var bg))
                return Results.Problem(statusCode: 400, detail: "INVALID_BUILDING_ID");

            var userIdClaim = httpCtx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var actorUserId))
                return Results.Problem(statusCode: 401, detail: "Not authenticated");

            var hasAccess = await grantRepo.HasAccessAsync(actorUserId, bg, ct);
            if (!hasAccess)
                return Results.Problem(statusCode: 403, detail: "Access denied to this building");

            if (!Enum.TryParse<SystemType>(req.SystemType, true, out var systemType))
                return Results.Problem(statusCode: 400, detail: "INVALID_SYSTEM_TYPE");

            var system = EngineeringSystem.Create(BuildingId.From(bg), req.Name, systemType, req.Priority, req.Description);

            if (req.Capabilities is not null)
            {
                foreach (var cap in req.Capabilities)
                    system.AddCapability(new SystemCapability(cap.Code, cap.DataType, cap.Unit, cap.Minimum, cap.Maximum));
            }

            if (req.Resources is not null)
            {
                foreach (var res in req.Resources)
                    system.AddResource(new EngineeringResource(res.Code, res.Maximum, res.Unit, res.Priority));
            }

            await repo.AddAsync(system, ct);
            return Results.Created($"/api/v1/engineering-systems/{system.Id}", MapToDto(system));
        }).RequirePermission("engineering_configure");
    }

    public static object MapToDto(EngineeringSystem s) => new
    {
        s.Id,
        s.BuildingId,
        s.Name,
        SystemType = s.SystemType.ToString(),
        Lifecycle = s.Lifecycle.ToString(),
        OperationalStatus = s.OperationalStatus.ToString(),
        ControlMode = s.ControlMode.ToString(),
        s.Priority,
        s.Description,
        Capabilities = s.Capabilities.Select(c => new { c.Code, c.DataType, c.Unit, c.Minimum, c.Maximum, c.SupportsModulation }),
        Resources = s.Resources.Select(r => new { r.Code, r.Unit, r.Maximum, r.Available, r.Reserved, r.Used, r.Priority }),
        ZoneCount = s.Zones.Count,
        DeviceCount = s.DeviceBindings.Count(b => b.Enabled),
        s.Version
    };

    public static object MapPlanToDto(CommandPlan p) => new
    {
        p.Id,
        EngineeringSystemId = p.EngineeringSystemId.ToString(),
        p.NeedType,
        p.CapabilityCode,
        Status = p.Status.ToString(),
        p.RequestedValue,
        p.ValueUnit,
        p.StrategyName,
        Steps = p.Steps.Select(s => new { s.CapabilityCode, s.Operation, s.RequestedValue, s.ValueUnit, DeviceId = s.DeviceId?.ToString(), s.Sequence, s.DeviceRole, s.Status, StatusStr = s.Status.ToString() }),
        ResourceAllocations = p.ResourceAllocations.Select(a => new { a.ResourceCode, a.Amount }),
        p.CreatedAt,
        p.CompletedAt,
        p.FailureCode,
        p.Version
    };
}

public record CreateEngineeringSystemRequest(
    string BuildingId, string Name, string SystemType,
    int Priority = 100, string? Description = null,
    List<CreateCapabilityRequest>? Capabilities = null,
    List<CreateResourceRequest>? Resources = null);

public record CreateCapabilityRequest(string Code, string? DataType, string? Unit, double? Minimum, double? Maximum);
public record CreateResourceRequest(string Code, double Maximum, string? Unit, int Priority = 100);
