namespace ClimateHub.Modules.IAM.Domain;

public class UserAccount
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string SecurityStamp { get; private set; } = string.Empty;

    private UserAccount() { }

    public UserAccount(Guid id, string email, string passwordHash, string displayName)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        SecurityStamp = Guid.NewGuid().ToString("N");
    }
}