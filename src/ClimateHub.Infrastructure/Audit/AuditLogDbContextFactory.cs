using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClimateHub.Infrastructure.Audit;

public class AuditLogDbContextFactory : IDesignTimeDbContextFactory<AuditLogDbContext>
{
    public AuditLogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditLogDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=climate_hub;Username=climate_hub;Password=placeholder");
        return new AuditLogDbContext(optionsBuilder.Options);
    }
}