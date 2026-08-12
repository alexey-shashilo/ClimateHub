using ClimateHub.Modules.IAM.Application;
using ClimateHub.Modules.IAM.Domain;
using ClimateHub.Modules.IAM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Modules.IAM;

public static class DependencyInjection
{
    public static IServiceCollection AddIamModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IamDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "iam")
                      .MigrationsAssembly(typeof(IamDbContext).Assembly.FullName!)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IBuildingAccessGrantRepository, BuildingAccessGrantRepository>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations();

        return services;
    }
}
