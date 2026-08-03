using ClimateHub.Modules.EngineeringSystems.Application.Resolvers;
using ClimateHub.Modules.EngineeringSystems.Application.Services;
using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class EngineeringSystemsModuleService : IEngineeringSystemsModule
{
    private readonly IEngineeringSystemRepository _systemRepo;
    private readonly ICommandPlanRepository _planRepo;
    private readonly CapabilityRouter _router;
    private readonly ResourceManager _resourceManager;
    private readonly IEngineeringCapabilityFamilyResolver _familyResolver;
    private readonly IVentilationPlanningService _ventilationPlanning;
    private readonly IThermalPlanningService _thermalPlanning;
    private readonly IHumidificationPlanningService _humidificationPlanning;
    private readonly ILightingPlanningService _lightingPlanning;
    private readonly IStandardEngineeringPlanningService _standardPlanning;

    public EngineeringSystemsModuleService(
        IEngineeringSystemRepository systemRepo,
        ICommandPlanRepository planRepo,
        CapabilityRouter router,
        ResourceManager resourceManager,
        IEngineeringCapabilityFamilyResolver familyResolver,
        IVentilationPlanningService ventilationPlanning,
        IThermalPlanningService thermalPlanning,
        IHumidificationPlanningService humidificationPlanning,
        ILightingPlanningService lightingPlanning,
        IStandardEngineeringPlanningService standardPlanning)
    {
        _systemRepo = systemRepo;
        _planRepo = planRepo;
        _router = router;
        _resourceManager = resourceManager;
        _familyResolver = familyResolver;
        _ventilationPlanning = ventilationPlanning;
        _thermalPlanning = thermalPlanning;
        _humidificationPlanning = humidificationPlanning;
        _lightingPlanning = lightingPlanning;
        _standardPlanning = standardPlanning;
    }

    public async Task<CapabilityPlanResultDto> PlanAsync(
        EngineeringCapabilityRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var routing = await _router.RouteAsync(request.RoomId, request.CapabilityCode, cancellationToken);
        if (!routing.IsSuccess)
            return new CapabilityPlanResultDto(null, false, routing.FailureCode, routing.FailureReason);

        var system = routing.System!;
        var family = _familyResolver.ResolveFamily(request.CapabilityCode);

        return family switch
        {
            EngineeringCapabilityFamily.Ventilation => await _ventilationPlanning.PlanVentilationAsync(request, system, cancellationToken),
            EngineeringCapabilityFamily.Thermal => await _thermalPlanning.PlanThermalAsync(request, system, cancellationToken),
            EngineeringCapabilityFamily.Humidification => await _humidificationPlanning.PlanHumidificationAsync(request, system, cancellationToken),
            EngineeringCapabilityFamily.Lighting => await _lightingPlanning.PlanLightingAsync(request, system, cancellationToken),
            _ => await _standardPlanning.PlanStandardAsync(request, system, routing, cancellationToken)
        };
    }

    public async Task<CommandPlanStatusDto?> GetCommandPlanStatusAsync(
        CommandPlanIdDto commandPlanId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepo.GetByIdAsync(commandPlanId.Value, cancellationToken);
        if (plan is null) return null;

        return new CommandPlanStatusDto(
            commandPlanId, plan.Status.ToString(),
            plan.FailureCode, plan.CreatedAt, plan.CompletedAt);
    }

    public async Task<CancelCommandPlanResultDto> CancelCommandPlanAsync(
        CommandPlanIdDto commandPlanId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepo.GetByIdAsync(commandPlanId.Value, cancellationToken);
        if (plan is null)
            return new CancelCommandPlanResultDto(false, EngineeringErrors.CommandPlanNotFound);

        if (plan.Status is CommandPlanStatus.Succeeded or CommandPlanStatus.Failed
            or CommandPlanStatus.Cancelled or CommandPlanStatus.Expired)
            return new CancelCommandPlanResultDto(false, EngineeringErrors.CommandPlanAlreadyTerminal);

        plan.Cancel();
        await _planRepo.UpdateAsync(plan, cancellationToken);
        await _resourceManager.ReleaseAsync(plan, cancellationToken);
        return new CancelCommandPlanResultDto(true, null);
    }
}