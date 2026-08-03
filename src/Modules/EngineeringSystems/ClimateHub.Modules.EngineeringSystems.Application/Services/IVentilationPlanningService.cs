using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public interface IVentilationPlanningService
{
    Task<CapabilityPlanResultDto> PlanVentilationAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct);
}