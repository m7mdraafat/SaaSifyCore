namespace SaaSifyCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaaSifyCore.Domain.Interfaces;
using SaaSifyCore.Infrastructure.Data;
using SaaSifyCore.Infrastructure.MultiTenancy;

/// <summary>
/// Infrastructure layer dependency injection configuration.
/// BEST PRACTICE: Encapsulates all infrastructure service registrations.
/// </summary>
public static class DependencyInjection
{
    private const string TestingEnvironment = "Testing";

    /// <summary>
    /// Registers Infrastructure layer services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (environment != TestingEnvironment)
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

                    // Performance optimizations
                    npgsqlOptions.CommandTimeout(30); // seconds
                    npgsqlOptions.MaxBatchSize(100);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

                // Enable detailed errors and sensitive data logging in Development
                if (environment == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
                else
                {
                    // Production optimizations
                    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
                }
            });

            // Register IApplicationDbContext interface for Application layer
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
        }

        services.AddScoped<ITenantContext, TenantContext>();

        // TODO: Add other infrastructure services here as we build them:
        // - Email services (Phase 3)
        // - Blob storage (Phase 3)
        // - Caching (Redis) (Phase 3)
        // - External APIs (Stripe) (Phase 4)

        return services;
    }
}