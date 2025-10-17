namespace SaaSifyCore.Api.Configuration;

using Microsoft.OpenApi.Models;

/// <summary>
/// API layer service configuration extensions.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Configures Swagger/OpenAPI documentation.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SaaSifyCore API",
                Version = "v1",
                Description = "A multi-tenant SaaS backend boilerplate built with .NET 8, Clean Architecture, and PostgreSQL",
                Contact = new OpenApiContact
                {
                    Name = "Mohamed Raafat",
                    Email = "mohamedraafat.engineer@gmail.com",
                    Url = new Uri("https://github.com/m7mdraafat/SaaSifyCore")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

        });

        return services;
    }

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