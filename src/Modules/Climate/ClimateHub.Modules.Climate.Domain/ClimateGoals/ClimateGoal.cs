using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Domain.ClimateGoals;

public readonly record struct ClimateGoalId(Guid Value)
{
    public static ClimateGoalId New() => new(Guid.NewGuid());
    public static ClimateGoalId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum GoalStatus
{
    Active,
    Planning,
    Executing,
    WaitingForEffect,
    Satisfied,
    Blocked,
    Failed,
    Disabled
}

public enum StrategyProfile
{
    Comfort,
    Balanced,
    EnergySaving,
    Night,
    Away,
    MaximumAirQuality,
    Sleep,
    Vacation
}

public class ClimateGoal : Entity<ClimateGoalId>, IAggregateRoot
{
    public BuildingId BuildingId { get; private init; }
    public RoomId RoomId { get; private init; }
    public GoalStatus Status { get; private set; }
    public StrategyProfile ActiveProfile { get; private set; }

    public double TargetTemperature { get; private set; }
    public double TargetTemperatureMin { get; private set; }
    public double TargetTemperatureMax { get; private set; }
    public double TargetHumidity { get; private set; }
    public double TargetHumidityMin { get; private set; }
    public double TargetHumidityMax { get; private set; }
    public double TargetCo2 { get; private set; }
    public double TargetCo2Max { get; private set; }
    public double TargetIlluminance { get; private set; }

    public double? CurrentTemperature { get; private set; }
    public double? CurrentHumidity { get; private set; }
    public double? CurrentCo2 { get; private set; }
    public double? CurrentIlluminance { get; private set; }

    public double SatisfactionPct { get; private set; }
    public ClimatePlanId? ActiveClimatePlanId { get; private set; }
    public string? PolicyVersion { get; private set; }
    public string? EnvironmentStateVersion { get; private set; }
    public DateTimeOffset? LastEvaluatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? SatisfiedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public DateTimeOffset? BlockedAt { get; private set; }
    public uint Version { get; private set; }

    private ClimateGoal() { ActiveProfile = StrategyProfile.Comfort; }

    private ClimateGoal(ClimateGoalId id, BuildingId buildingId, RoomId roomId,
        double targetTemp, double targetTempMin, double targetTempMax,
        double targetHum, double targetHumMin, double targetHumMax,
        double targetCo2, double targetCo2Max,
        double targetIlluminance,
        StrategyProfile profile = StrategyProfile.Comfort,
        string? policyVersion = null)
    {
        Id = id; BuildingId = buildingId; RoomId = roomId;
        Status = GoalStatus.Active; ActiveProfile = profile;
        TargetTemperature = targetTemp; TargetTemperatureMin = targetTempMin;
        TargetTemperatureMax = targetTempMax; TargetHumidity = targetHum;
        TargetHumidityMin = targetHumMin; TargetHumidityMax = targetHumMax;
        TargetCo2 = targetCo2; TargetCo2Max = targetCo2Max;
        TargetIlluminance = targetIlluminance;
        PolicyVersion = policyVersion;
        CreatedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow;
        Version = 1;
    }

    public static ClimateGoal Create(BuildingId buildingId, RoomId roomId,
        double targetTemp = 22, double targetTempMin = 20, double targetTempMax = 26,
        double targetHum = 45, double targetHumMin = 30, double targetHumMax = 60,
        double targetCo2 = 600, double targetCo2Max = 1000,
        double targetIlluminance = 500,
        StrategyProfile profile = StrategyProfile.Comfort,
        string? policyVersion = null) =>
        new(ClimateGoalId.New(), buildingId, roomId,
            targetTemp, targetTempMin, targetTempMax,
            targetHum, targetHumMin, targetHumMax,
            targetCo2, targetCo2Max, targetIlluminance,
            profile, policyVersion);

    public void UpdateTargets(double tempMin, double tempMax, double humMin, double humMax,
        double co2Max, StrategyProfile profile, string? policyVersion = null)
    {
        TargetTemperatureMin = tempMin; TargetTemperatureMax = tempMax;
        TargetHumidityMin = humMin; TargetHumidityMax = humMax;
        TargetCo2Max = co2Max;
        ActiveProfile = profile;
        if (policyVersion is not null) PolicyVersion = policyVersion;
        RecalculateSatisfaction();
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void UpdateCurrentConditions(double? temperature, double? humidity, double? co2, double? illuminance,
        string? environmentStateVersion = null)
    {
        CurrentTemperature = temperature; CurrentHumidity = humidity;
        CurrentCo2 = co2; CurrentIlluminance = illuminance;
        if (environmentStateVersion is not null) EnvironmentStateVersion = environmentStateVersion;
        RecalculateSatisfaction();
        LastEvaluatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void SetProfile(StrategyProfile profile)
    {
        ActiveProfile = profile; UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void BeginPlanning()
    {
        Transition(GoalStatus.Planning);
    }

    public void AttachPlan(ClimatePlanId planId)
    {
        ActiveClimatePlanId = planId;
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    public void MarkExecuting()
    {
        Transition(GoalStatus.Executing);
    }

    public void WaitForEffect()
    {
        Transition(GoalStatus.WaitingForEffect);
    }

    public void MarkSatisfied()
    {
        Transition(GoalStatus.Satisfied);
        SatisfiedAt = DateTimeOffset.UtcNow;
        ActiveClimatePlanId = null;
    }

    public void Block()
    {
        Transition(GoalStatus.Blocked);
        BlockedAt = DateTimeOffset.UtcNow;
    }

    public void Fail()
    {
        Transition(GoalStatus.Failed);
        FailedAt = DateTimeOffset.UtcNow;
        ActiveClimatePlanId = null;
    }

    public void Disable()
    {
        Transition(GoalStatus.Disabled);
    }

    public void Reactivate()
    {
        if (Status == GoalStatus.Disabled || Status == GoalStatus.Satisfied || Status == GoalStatus.Failed)
        {
            Status = GoalStatus.Active;
            SatisfiedAt = null; FailedAt = null; BlockedAt = null;
            UpdatedAt = DateTimeOffset.UtcNow; Version++;
        }
    }

    public void ClearPlanId()
    {
        ActiveClimatePlanId = null;
        UpdatedAt = DateTimeOffset.UtcNow; Version++;
    }

    private void Transition(GoalStatus to)
    {
        if (!IsValid(Status, to))
            throw new InvalidOperationException($"Invalid ClimateGoal transition: {Status} → {to}");
        Status = to;
        Version++;
    }

    private static bool IsValid(GoalStatus from, GoalStatus to) => (from, to) switch
    {
        (GoalStatus.Active, GoalStatus.Planning) => true,
        (GoalStatus.Active, GoalStatus.Disabled) => true,
        (GoalStatus.Planning, GoalStatus.Executing) => true,
        (GoalStatus.Planning, GoalStatus.Blocked) => true,
        (GoalStatus.Planning, GoalStatus.Active) => true,
        (GoalStatus.Executing, GoalStatus.WaitingForEffect) => true,
        (GoalStatus.Executing, GoalStatus.Blocked) => true,
        (GoalStatus.Executing, GoalStatus.Failed) => true,
        (GoalStatus.WaitingForEffect, GoalStatus.Satisfied) => true,
        (GoalStatus.WaitingForEffect, GoalStatus.Failed) => true,
        (GoalStatus.WaitingForEffect, GoalStatus.Blocked) => true,
        (GoalStatus.Satisfied, GoalStatus.Active) => true,
        (GoalStatus.Satisfied, GoalStatus.Disabled) => true,
        (GoalStatus.Blocked, GoalStatus.Active) => true,
        (GoalStatus.Failed, GoalStatus.Active) => true,
        (GoalStatus.Disabled, GoalStatus.Active) => true,
        _ => false
    };

    private void RecalculateSatisfaction()
    {
        var metrics = 0;
        var satisfied = 0;

        if (CurrentTemperature.HasValue)
        {
            metrics++;
            if (CurrentTemperature.Value >= TargetTemperatureMin && CurrentTemperature.Value <= TargetTemperatureMax)
                satisfied++;
        }
        if (CurrentHumidity.HasValue)
        {
            metrics++;
            if (CurrentHumidity.Value >= TargetHumidityMin && CurrentHumidity.Value <= TargetHumidityMax)
                satisfied++;
        }
        if (CurrentCo2.HasValue)
        {
            metrics++;
            if (CurrentCo2.Value <= TargetCo2Max)
                satisfied++;
        }
        if (CurrentIlluminance.HasValue)
        {
            metrics++;
            if (CurrentIlluminance.Value >= 300)
                satisfied++;
        }

        SatisfactionPct = metrics > 0 ? (double)satisfied / metrics * 100 : 0;
    }
}