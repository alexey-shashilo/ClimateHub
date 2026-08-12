using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Climate.Application.Events;

public class ClimateLifecycleEventPublisher
{
    private readonly InternalEventOutboxRepository _outboxRepo;
    private readonly ILogger<ClimateLifecycleEventPublisher> _logger;

    public ClimateLifecycleEventPublisher(
        InternalEventOutboxRepository outboxRepo,
        ILogger<ClimateLifecycleEventPublisher> logger)
    {
        _outboxRepo = outboxRepo; _logger = logger;
    }

    public async Task PublishGoalEventAsync(string eventType, ClimateGoal goal, string? correlationId = null, string? causationId = null, CancellationToken ct = default)
    {
        await _outboxRepo.CreateAsync(
            eventType, "ClimateGoal", goal.Id.ToString(),
            goal.BuildingId.Value, goal.RoomId.Value,
            new
            {
                goal.Id,
                goal.RoomId,
                Status = goal.Status.ToString(),
                Profile = goal.ActiveProfile.ToString(),
                SatisfactionPct = goal.SatisfactionPct,
                ActiveClimatePlanId = goal.ActiveClimatePlanId?.ToString()
            },
            null, correlationId, causationId, null, ct);
        _logger.LogDebug("Published {EventType} for goal {GoalId}", eventType, goal.Id);
    }

    public async Task PublishPlanEventAsync(string eventType, ClimatePlan plan, string? correlationId = null, string? causationId = null, CancellationToken ct = default)
    {
        await _outboxRepo.CreateAsync(
            eventType, "ClimatePlan", plan.Id.ToString(),
            plan.BuildingId.Value, plan.RoomId.Value,
            new
            {
                plan.Id,
                plan.GoalId,
                Status = plan.Status.ToString(),
                plan.ActiveProfile,
                SubPlanCount = plan.SubPlans.Count,
                ConflictCount = plan.ResolvedConflicts.Count,
                plan.FailureCode,
                plan.FailureReason
            },
            null, correlationId, causationId, null, ct);
        _logger.LogDebug("Published {EventType} for plan {PlanId}", eventType, plan.Id);
    }

    public async Task PublishSubPlanEventAsync(string eventType, EngineeringSubPlan subPlan, Guid? buildingId, Guid? roomId, string? correlationId = null, string? causationId = null, CancellationToken ct = default)
    {
        await _outboxRepo.CreateAsync(
            eventType, "EngineeringSubPlan", subPlan.Id.ToString(),
            buildingId, roomId,
            new
            {
                subPlan.Id,
                subPlan.ClimatePlanId,
                subPlan.EngineeringCapabilityCode,
                Status = subPlan.Status.ToString(),
                subPlan.EngineeringCommandPlanId,
                subPlan.FailureCode
            },
            null, correlationId, causationId, null, ct);
    }

    public async Task PublishConflictEventAsync(ClimateConflict conflict, Guid buildingId, Guid roomId, string? correlationId = null, string? causationId = null, CancellationToken ct = default)
    {
        await _outboxRepo.CreateAsync(
            "climate.conflict.detected", "ClimateConflict", conflict.Id.ToString(),
            buildingId, roomId,
            new
            {
                conflict.Id,
                conflict.ClimatePlanId,
                conflict.ConflictType,
                conflict.FirstCapabilityCode,
                conflict.SecondCapabilityCode,
                conflict.WinnerCapabilityCode,
                conflict.LoserCapabilityCode,
                conflict.Resolution
            },
            null, correlationId, causationId, null, ct);
    }

    public async Task PublishResourceEventAsync(string eventType, ClimateResourceReservation reservation, Guid buildingId, Guid roomId, CancellationToken ct = default)
    {
        await _outboxRepo.CreateAsync(
            eventType, "ClimateResourceReservation", reservation.Id.ToString(),
            buildingId, roomId,
            new
            {
                reservation.Id,
                reservation.ClimateResourceId,
                reservation.ClimatePlanId,
                reservation.RequestedAmount,
                reservation.ReservedAmount,
                reservation.Status
            },
            null, null, null, null, ct);
    }
}
