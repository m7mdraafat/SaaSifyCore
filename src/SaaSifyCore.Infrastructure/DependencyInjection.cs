namespace SaaSifyCore.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaaSifyCore.Domain.Interfaces;
using SaaSifyCore.Infrastructure.Data;

/// <summary>
/// Infrastructure layer dependency injection configuration.
/// BEST PRACTICE: Encapsulates all infrastructure service registrations.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure layer services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Store migrations in Infrastructure project
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                // Retry on transient failures
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Command timeout for long-running queries
                npgsqlOptions.CommandTimeout(30);
            });

            // Enable detailed errors and sensitive data logging in Development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Register IApplicationDbContext interface for Application layer
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // TODO: Add other infrastructure services here as we build them:
        // - ITenantContext implementation (Phase 2)
        // - Email services (Phase 3)
        // - Blob storage (Phase 3)
        // - Caching (Redis) (Phase 3)
        // - External APIs (Stripe) (Phase 4)

        return services;
    }
}