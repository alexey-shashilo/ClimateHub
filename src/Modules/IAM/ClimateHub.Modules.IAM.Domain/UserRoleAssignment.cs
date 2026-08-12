namespace ClimateHub.Modules.IAM.Domain;

public class UserRoleAssignment
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserRoleAssignment() { }

    public UserRoleAssignment(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
