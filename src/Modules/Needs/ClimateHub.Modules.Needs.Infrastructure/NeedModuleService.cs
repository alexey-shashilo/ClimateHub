using ClimateHub.Modules.Needs.Contracts;
using ClimateHub.Modules.Needs.Domain;
using ClimateHub.Modules.Needs.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using NeedEvalTrigger = ClimateHub.Modules.Needs.Domain.NeedEvaluationTrigger;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class NeedModuleService(
    NeedEvaluationService evaluationService,
    INeedRepository needRepo) : INeedModule
{
    public async Task EvaluateRoomAsync(RoomId roomId, string trigger, string? correlationId = null, CancellationToken ct = default)
    {
        var parsedTrigger = Enum.TryParse<NeedEvalTrigger>(trigger, true, out var t)
            ? t : NeedEvalTrigger.ManualRequest;
        await evaluationService.EvaluateRoomAsync(roomId, parsedTrigger, correlationId, ct: ct);
    }

    public async Task HandleCommandTerminalEventAsync(CommandIdDto commandId, string terminalStatus, string trigger,
        string? correlationId = null, CancellationToken ct = default)
    {
        var parsedTrigger = Enum.TryParse<NeedEvalTrigger>(trigger, true, out var t)
            ? t : NeedEvalTrigger.PeriodicReconciliation;

        var needs = await needRepo.GetActiveAsync(ct);
        var needDto = needs.FirstOrDefault(n => n.ActiveCommandId is not null && n.ActiveCommandId.Value.Value == commandId.Value);
        if (needDto is null) return;

        await evaluationService.HandleCommandTerminalEventAsync(needDto.Id,
            new CommandId(commandId.Value), commandStatus: terminalStatus, trigger: parsedTrigger, correlationId, ct: ct);
    }
}
