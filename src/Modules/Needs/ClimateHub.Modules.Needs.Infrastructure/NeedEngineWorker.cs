using ClimateHub.Modules.Needs.Domain;
using ClimateHub.Modules.Needs.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class NeedEngineWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NeedEngineWorker> _logger;

    public NeedEngineWorker(IServiceScopeFactory scopeFactory, ILogger<NeedEngineWorker> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NeedEngineWorker started");
        await Task.Delay(5000, stoppingToken);

        // Startup recovery: scan all rooms with environment state
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var evalService = scope.ServiceProvider.GetRequiredService<NeedEvaluationService>();
            var needRepo = scope.ServiceProvider.GetRequiredService<INeedRepository>();

            var activeNeeds = await needRepo.GetActiveAsync(stoppingToken);
            var roomsFromNeeds = activeNeeds.Select(n => n.RoomId).Distinct().ToList();

            foreach (var roomId in roomsFromNeeds)
            {
                if (stoppingToken.IsCancellationRequested) break;
                try
                {
                    await evalService.EvaluateRoomAsync(roomId,
                        NeedEvaluationTrigger.StartupRecovery, ct: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Startup recovery evaluation failed for room {Room}", roomId);
                }
            }
            _logger.LogInformation("NeedEngineWorker startup recovery completed for {Count} rooms", roomsFromNeeds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Startup recovery failed");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var needRepo = scope.ServiceProvider.GetRequiredService<INeedRepository>();
                var evalService = scope.ServiceProvider.GetRequiredService<NeedEvaluationService>();

                // Reconciliation: scan all rooms with active needs that require processing
                var pendingNeeds = await needRepo.GetNeedsPendingReconciliationAsync(stoppingToken);
                var roomsToEvaluate = pendingNeeds.Select(n => n.RoomId).Distinct().ToList();

                foreach (var roomId in roomsToEvaluate)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    try
                    {
                        await evalService.EvaluateRoomAsync(roomId,
                            NeedEvaluationTrigger.PeriodicReconciliation, ct: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Need evaluation failed for room {Room}", roomId);
                    }
                }

                // Handle cooldown expiry
                var now = DateTimeOffset.UtcNow;
                var activeNeeds = await needRepo.GetActiveAsync(stoppingToken);
                foreach (var dto in activeNeeds)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    if (dto.CooldownUntil.HasValue && dto.CooldownUntil.Value <= now && dto.Status == "Detected")
                    {
                        var need = await needRepo.GetByIdAsync(dto.Id, stoppingToken);
                        if (need is null) continue;
                        need.ClearCooldown();
                        await needRepo.UpdateAsync(need, stoppingToken);
                    }

                    if (dto.EffectEvaluationDueAt.HasValue && dto.EffectEvaluationDueAt.Value <= now && dto.Status == "WaitingForEffect")
                    {
                        var need = await needRepo.GetByIdAsync(dto.Id, stoppingToken);
                        if (need is null) continue;
                        // Re-evaluate room to check if effect occurred
                        await evalService.EvaluateRoomAsync(dto.RoomId,
                            NeedEvaluationTrigger.EffectEvaluationDue, ct: stoppingToken);
                    }

                    // Handle stale data -> blocked
                    if (dto.Status is "Detected" or "Planning" or "Planned" or "Executing" or "WaitingForEffect")
                    {
                        if (dto.LastEvaluationAt.HasValue && now - dto.LastEvaluationAt.Value > TimeSpan.FromMinutes(10))
                        {
                            var need = await needRepo.GetByIdAsync(dto.Id, stoppingToken);
                            if (need is null || need.Status == NeedStatus.Blocked) continue;
                            need.Block("ENVIRONMENT_DATA_STALE");
                            need.ClearActiveCommand();
                            await needRepo.UpdateAsync(need, stoppingToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "NeedEngineWorker cycle error"); }

            await Task.Delay(30000, stoppingToken);
        }
    }
}
