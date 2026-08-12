namespace ClimateHub.Modules.Needs.Domain;

public enum NeedEvaluationTrigger
{
    EnvironmentStateChanged,
    RoomPolicyChanged,
    DeviceAssignmentChanged,
    DeviceCapabilityChanged,
    DeviceConnectivityChanged,
    CommandSucceeded,
    CommandFailed,
    CommandTimedOut,
    CommandCancelled,
    CommandRejected,
    CooldownExpired,
    EffectEvaluationDue,
    PeriodicReconciliation,
    ManualRequest,
    StartupRecovery
}
