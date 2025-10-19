using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.Interfaces;
using SaaSifyCore.Domain.ValueObjects;
using SaaSifyCore.Infrastructure.Data;

namespace SaaSifyCore.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variable BEFORE any service registration happens
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        // Also set it on the builder
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Add InMemory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Register IApplicationDbContext interface
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        if (!context.Tenants.Any())
        {
            var tenant1 = Tenant.Create(
                TenantName.Create("Test Tenant 1"),
                SubDomain.Create("testtenant1"));

            var tenant2 = Tenant.Create(
                TenantName.Create("Test Tenant 2"),
                SubDomain.Create("testtenant2"));

            context.Tenants.AddRange(tenant1, tenant2);
            context.SaveChanges();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureDeleted();
            }
            catch (ObjectDisposedException)
            {
                // Services already disposed - acceptable
            }
        }
        base.Dispose(disposing);
    }
}