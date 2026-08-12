using ClimateHub.Modules.IAM.Domain;
using Microsoft.AspNetCore.Authorization;

namespace ClimateHub.Api.Authorization;

public class BuildingAccessHandler : AuthorizationHandler<BuildingAccessRequirement>
{
    private readonly IBuildingAccessGrantRepository _buildingAccessGrantRepository;

    public BuildingAccessHandler(IBuildingAccessGrantRepository buildingAccessGrantRepository)
    {
        _buildingAccessGrantRepository = buildingAccessGrantRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BuildingAccessRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        var hasAccess = await _buildingAccessGrantRepository.HasAccessAsync(userId, requirement.BuildingId);
        if (hasAccess)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}

public class BuildingAccessRequirement : IAuthorizationRequirement
{
    public Guid BuildingId { get; }

    public BuildingAccessRequirement(Guid buildingId)
    {
        BuildingId = buildingId;
    }
}
