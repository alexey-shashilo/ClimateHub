using Microsoft.AspNetCore.Authorization;

namespace ClimateHub.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value);

        if (permissions.Contains(requirement.Permission) || permissions.Contains("admin"))
            context.Succeed(requirement);
        else
            context.Fail();

        return Task.CompletedTask;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : Attribute
{
    public string Permission { get; }

    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
    }
}