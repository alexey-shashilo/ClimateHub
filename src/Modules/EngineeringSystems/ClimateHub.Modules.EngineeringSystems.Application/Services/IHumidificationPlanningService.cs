using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Application.Services;

public interface IHumidificationPlanningService
{
    Task<CapabilityPlanResultDto> PlanHumidificationAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct);
}
