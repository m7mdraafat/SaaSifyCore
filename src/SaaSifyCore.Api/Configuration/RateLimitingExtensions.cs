using AspNetCoreRateLimit;
using SaaSifyCore.Api.Middleware;

namespace SaaSifyCore.Api.Configuration;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Memory Cache
        services.AddMemoryCache();

        services.AddOptions();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));

        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    public static IApplicationBuilder UseRateLimiting(
        this IApplicationBuilder app)
    {
        // Use both: AspNetCoreRateLimit for general protection
        app.UseIpRateLimiting();

        // Custom Rate Limiting Middleware for specific endpoints
        app.UseMiddleware<RateLimitingMiddleware>();


        return app;
    }
}
