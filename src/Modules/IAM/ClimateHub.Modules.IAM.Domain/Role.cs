namespace ClimateHub.Modules.IAM.Domain;

public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public List<Permission> Permissions { get; private set; } = new();

    private Role() { }

    public Role(Guid id, string name, string? description, List<Permission> permissions)
    {
        Id = id;
        Name = name;
        Description = description;
        Permissions = permissions;
    }

    public void UpdatePermissions(List<Permission> permissions)
    {
        Permissions = permissions;
    }

    public bool HasPermission(Permission permission)
    {
        return Permissions.Contains(permission);
    }

    public bool HasAnyPermission(IEnumerable<Permission> permissions)
    {
        return permissions.Any(p => Permissions.Contains(p));
    }
}
