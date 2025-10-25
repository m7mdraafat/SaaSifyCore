using SaaSifyCore.Api.Services;

namespace SaaSifyCore.Api.Configuration;

/// <summary>
/// API layer service configuration extensions.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Configures CORS policies.
    /// BEST PRACTICE: Define allowed origins based on environment.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Registers API layer services following SOLID principles.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Register HTTP context accessor (required for cookie service)
        services.AddHttpContextAccessor();

        // Register application services
        services.AddScoped<IRefreshTokenCookieService, RefreshTokenCookieService>();
        services.AddScoped<IResultMapper, ResultMapper>();

        return services;
    }
}