using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public interface IStandardEngineeringPlanningService
{
    Task<CapabilityPlanResultDto> PlanStandardAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CapabilityRoutingResult routing,
        CancellationToken ct);
}
