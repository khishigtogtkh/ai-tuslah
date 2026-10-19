using AiAgent.Application.Tenants;
using AiAgent.Infrastructure.Identity;
using AiAgent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AiAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connection = config.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connection))
        {
            string Required(string key) => !string.IsNullOrWhiteSpace(config[key]) ? config[key]!
                : throw new InvalidOperationException($"Required database setting is missing: {key}");
            connection = new NpgsqlConnectionStringBuilder
            {
                Host = config["POSTGRES_HOST"] ?? "127.0.0.1",
                Port = int.TryParse(config["POSTGRES_PORT"], out var port) ? port : 5433,
                Database = Required("POSTGRES_DB"), Username = Required("POSTGRES_USER"),
                Password = Required("POSTGRES_PASSWORD"), IncludeErrorDetail = false
            }.ConnectionString;
        }
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager().AddDefaultTokenProviders();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<TenantService>();
        return services;
    }
}
