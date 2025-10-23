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
}