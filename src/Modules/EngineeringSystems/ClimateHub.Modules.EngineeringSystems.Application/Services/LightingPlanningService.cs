using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.Environment.Contracts;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public class LightingPlanningService : ILightingPlanningService
{
    private readonly ICommandPlanRepository _planRepo;
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly LightingStrategyEngine _lightingStrategy;
    private readonly CommandPlanExecutor _executor;

    public LightingPlanningService(
        ICommandPlanRepository planRepo,
        IRoomEnvironmentStateReader envReader,
        LightingStrategyEngine lightingStrategy,
        CommandPlanExecutor executor)
    {
        _planRepo = planRepo;
        _envReader = envReader;
        _lightingStrategy = lightingStrategy;
        _executor = executor;
    }

    public async Task<CapabilityPlanResultDto> PlanLightingAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.LightingConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.LightingConfigurationInvalid,
                "Lighting system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var currentLux = 300.0;
        var luxParam = parameters.FirstOrDefault(p => p.Parameter == "illuminance");
        if (luxParam?.Value.HasValue == true) currentLux = luxParam.Value.Value;

        var targetLux = request.CapabilityCode == EngineeringCapabilityCodes.IncreaseIlluminance
            ? config.DefaultBrightnessLux : config.MinimumBrightnessLux;

        var outdoorBrightness = 10000.0;
        var outdoorTemp = 25.0;

        var result = _lightingStrategy.PlanLighting(config, currentLux, targetLux,
            outdoorBrightness, outdoorTemp, 55.75, 37.62, DateTime.UtcNow, 3,
            3.5, 180, 20, null, null,
            request.CapabilityCode == EngineeringCapabilityCodes.IncreaseIlluminance);

        if (!result.IsSuccess)
            return new CapabilityPlanResultDto(null, false, result.FailureCode, result.FailureReason);

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            result.TargetBrightnessLux, "lux", "LightingStrategyEngine",
            result.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: $"Lighting: {result.TargetBrightnessLux} lux, {result.ColorTemperatureK}K, scene={result.AppliedScene}",
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);
        var execResult = await _executor.ExecuteAsync(commandPlan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Lighting command plan execution failed");
    }
}