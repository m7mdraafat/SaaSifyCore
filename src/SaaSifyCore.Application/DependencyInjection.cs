using Microsoft.Extensions.DependencyInjection;
using SaaSifyCore.Application.Services;

namespace SaaSifyCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}