using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public interface IThermalPlanningService
{
    Task<CapabilityPlanResultDto> PlanThermalAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct);
}