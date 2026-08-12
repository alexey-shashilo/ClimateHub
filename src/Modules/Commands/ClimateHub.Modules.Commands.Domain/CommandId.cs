using System.Globalization;

namespace ClimateHub.Modules.Commands.Domain;

public readonly record struct CommandId(Guid Value) : IParsable<CommandId>
{
    public static CommandId New() => new(Guid.NewGuid());
    public static CommandId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
    public static CommandId Parse(string s, IFormatProvider? provider = null) => new(Guid.Parse(s));
    public static bool TryParse(string? s, IFormatProvider? provider, out CommandId result)
    {
        if (Guid.TryParse(s, out var g)) { result = new CommandId(g); return true; }
        result = default; return false;
    }
}

public enum CommandStatus
{
    Created, Validated, Queued, Published,
    Acknowledged, Executing,
    Succeeded, Failed,
    Expired, Cancelled, Rejected, Superseded, TimedOut,
    CancellationRequested
}

public enum CommandPriority { Low, Normal, High, Critical }

public static class CommandTransitions
{
    private static readonly HashSet<(CommandStatus, CommandStatus)> Valid = new()
    {
        (CommandStatus.Created, CommandStatus.Validated),
        (CommandStatus.Created, CommandStatus.Rejected),
        (CommandStatus.Validated, CommandStatus.Queued),
        (CommandStatus.Validated, CommandStatus.Rejected),
        (CommandStatus.Queued, CommandStatus.Published),
        (CommandStatus.Queued, CommandStatus.Cancelled),
        (CommandStatus.Published, CommandStatus.Acknowledged),
        (CommandStatus.Published, CommandStatus.TimedOut),
        (CommandStatus.Published, CommandStatus.Expired),
        (CommandStatus.Acknowledged, CommandStatus.Executing),
        (CommandStatus.Acknowledged, CommandStatus.Failed),
        (CommandStatus.Acknowledged, CommandStatus.TimedOut),
        (CommandStatus.Executing, CommandStatus.Succeeded),
        (CommandStatus.Executing, CommandStatus.Failed),
        (CommandStatus.Executing, CommandStatus.TimedOut),
        (CommandStatus.Created, CommandStatus.Cancelled),
        (CommandStatus.Validated, CommandStatus.Cancelled),
        (CommandStatus.Published, CommandStatus.CancellationRequested),
        (CommandStatus.Acknowledged, CommandStatus.CancellationRequested),
        (CommandStatus.Executing, CommandStatus.CancellationRequested),
        (CommandStatus.CancellationRequested, CommandStatus.Cancelled),
        (CommandStatus.CancellationRequested, CommandStatus.Failed),
        (CommandStatus.CancellationRequested, CommandStatus.Succeeded),
    };

    public static bool IsValid(CommandStatus from, CommandStatus to) => Valid.Contains((from, to));
    public static bool IsTerminal(CommandStatus s) => s is CommandStatus.Succeeded or CommandStatus.Failed
        or CommandStatus.Expired or CommandStatus.Cancelled or CommandStatus.Rejected
        or CommandStatus.Superseded or CommandStatus.TimedOut;
}
