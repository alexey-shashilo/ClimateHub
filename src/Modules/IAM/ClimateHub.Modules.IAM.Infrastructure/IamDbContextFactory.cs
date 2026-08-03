using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class IamDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=climate_hub;Username=climate_hub;Password=placeholder");
        return new IamDbContext(optionsBuilder.Options);
    }
}