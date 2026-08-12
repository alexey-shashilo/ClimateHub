using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Contracts;

public enum NeedStatus { Detected, Planning, Planned, Executing, WaitingForEffect, Satisfied, Blocked }
public enum NeedEvaluationTrigger { EnvironmentStateChanged, CommandSucceeded, CommandFailed, CommandTimedOut, CommandCancelled, CommandRejected, RoomPolicyChanged, DeviceConnectivityChanged, PeriodicReconciliation, ManualRequest, StartupRecovery }

public interface INeedModule
{
    Task EvaluateRoomAsync(RoomId roomId, string trigger, string? correlationId = null, CancellationToken ct = default);
    Task HandleCommandTerminalEventAsync(CommandIdDto commandId, string terminalStatus, string trigger, string? correlationId = null, CancellationToken ct = default);
}

public readonly record struct CommandIdDto(Guid Value)
{
    public static CommandIdDto From(Guid value) => new(value);
}
