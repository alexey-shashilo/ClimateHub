using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public interface ILightingPlanningService
{
    Task<CapabilityPlanResultDto> PlanLightingAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct);
}
