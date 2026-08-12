using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;
using NeedsDomain = ClimateHub.Modules.Needs.Domain;
using NeedsDomainRepos = ClimateHub.Modules.Needs.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class CommandTerminalEventHandlers
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandTerminalEventHandlers> _logger;

    public CommandTerminalEventHandlers(IServiceScopeFactory scopeFactory, ILogger<CommandTerminalEventHandlers> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleCommandTerminalEventAsync(CommandId commandId, CommandStatus terminalStatus, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var needRepo = scope.ServiceProvider.GetRequiredService<NeedsDomainRepos.INeedRepository>();
            var evalService = scope.ServiceProvider.GetRequiredService<NeedEvaluationService>();

            // Find the need that owns this command
            var activeNeeds = await needRepo.GetActiveAsync(ct);
            var needDto = activeNeeds.FirstOrDefault(n => n.ActiveCommandId == new NeedsDomain.CommandId(commandId.Value));
            if (needDto is null)
            {
                _logger.LogDebug("No active need found for command {CommandId}", commandId);
                return;
            }

            var trigger = terminalStatus switch
            {
                CommandStatus.Succeeded => NeedsDomain.NeedEvaluationTrigger.CommandSucceeded,
                CommandStatus.Failed => NeedsDomain.NeedEvaluationTrigger.CommandFailed,
                CommandStatus.TimedOut => NeedsDomain.NeedEvaluationTrigger.CommandTimedOut,
                CommandStatus.Cancelled => NeedsDomain.NeedEvaluationTrigger.CommandCancelled,
                CommandStatus.Rejected => NeedsDomain.NeedEvaluationTrigger.CommandRejected,
                _ => NeedsDomain.NeedEvaluationTrigger.PeriodicReconciliation
            };

            await evalService.HandleCommandTerminalEventAsync(needDto.Id, new NeedsDomain.CommandId(commandId.Value),
                terminalStatus.ToString(), trigger, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle command terminal event for {CommandId}", commandId);
        }
    }
}
