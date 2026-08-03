namespace ClimateHub.Modules.Climate.Domain;

public static class ClimateErrors
{
    public const string GoalNotFound = "CLIMATE_GOAL_NOT_FOUND";
    public const string GoalAlreadySatisfied = "CLIMATE_GOAL_ALREADY_SATISFIED";
    public const string PlanNotFound = "CLIMATE_PLAN_NOT_FOUND";
    public const string PlanAlreadyTerminal = "CLIMATE_PLAN_ALREADY_TERMINAL";
    public const string PlanCannotBeCancelled = "CLIMATE_PLAN_CANNOT_BE_CANCELLED";
    public const string PlanDependencyCycle = "CLIMATE_PLAN_DEPENDENCY_CYCLE";
    public const string PlanDependencyNotSatisfied = "CLIMATE_PLAN_DEPENDENCY_NOT_SATISFIED";
    public const string ConflictUnresolved = "CLIMATE_CONFLICT_UNRESOLVED";
    public const string ResourceNotFound = "CLIMATE_RESOURCE_NOT_FOUND";
    public const string ResourceInsufficient = "CLIMATE_RESOURCE_INSUFFICIENT";
    public const string ResourceReservationConflict = "CLIMATE_RESOURCE_RESERVATION_CONFLICT";
    public const string EffectNotObserved = "CLIMATE_EFFECT_NOT_OBSERVED";
    public const string EnvironmentDataStale = "CLIMATE_ENVIRONMENT_DATA_STALE";
    public const string PolicyNotFound = "CLIMATE_POLICY_NOT_FOUND";
    public const string ProfileInvalid = "CLIMATE_PROFILE_INVALID";
    public const string EvaluationBusy = "CLIMATE_EVALUATION_BUSY";
    public const string ConcurrencyConflict = "CLIMATE_CONCURRENCY_CONFLICT";
    public const string EngineeringSubPlanFailed = "ENGINEERING_SUB_PLAN_FAILED";
    public const string EngineeringSubPlanUnavailable = "ENGINEERING_SUB_PLAN_UNAVAILABLE";
}