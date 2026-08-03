using ClimateHub.Modules.IAM.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class IamDbContext : DbContext
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<BuildingAccessGrant> BuildingAccessGrants => Set<BuildingAccessGrant>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    public IamDbContext(DbContextOptions<IamDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("iam");

        modelBuilder.Entity<UserAccount>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            e.Property(x => x.SecurityStamp).IsRequired();
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.Permissions)
                .HasConversion(
                    v => string.Join(',', v.Select(p => p.ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => Enum.Parse<Permission>(p)).ToList());
        });

        modelBuilder.Entity<UserRoleAssignment>(e =>
        {
            e.ToTable("user_role_assignments");
            e.HasKey(x => new { x.UserId, x.RoleId });
        });

        modelBuilder.Entity<BuildingAccessGrant>(e =>
        {
            e.ToTable("building_access_grants");
            e.HasKey(x => new { x.UserId, x.BuildingId });
        });

        modelBuilder.Entity<RefreshSession>(e =>
        {
            e.ToTable("refresh_sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.RefreshToken).HasMaxLength(512).IsRequired();
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.TokenFamilyId).IsRequired();
            e.HasIndex(x => x.TokenFamilyId);
            e.Property(x => x.UserId).IsRequired();
        });
    }
}