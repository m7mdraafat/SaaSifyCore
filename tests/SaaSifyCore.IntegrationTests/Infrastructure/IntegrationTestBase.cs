using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.ValueObjects;
using SaaSifyCore.Infrastructure.Data;
using System.Net.Http.Headers;

namespace SaaSifyCore.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected HttpClient Client;
    protected IServiceScope Scope;
    protected ApplicationDbContext Context;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    // Helper to get fresh context for querying latest data
    protected ApplicationDbContext GetFreshContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    protected void SetTenantHeader(string subDomain)
    {
        Client.DefaultRequestHeaders.Remove("X-Tenant-Subdomain");
        Client.DefaultRequestHeaders.Add("X-Tenant-Subdomain", subDomain);
    }

    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthorizationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Initializes the test by creating a fresh HttpClient, service scope, and database context,
    /// ensuring the database is created and seeded with default tenants if necessary.
    /// </summary>
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // Create fresh client and scope for each test
        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        Context = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
        // Ensure database is initialized
        await Context.Database.EnsureCreatedAsync();
    
        // Seed tenants if not exists
        if (!await Context.Tenants.AnyAsync())
        {
            var tenant1 = Tenant.Create(
                TenantName.Create("Test Tenant 1"),
                SubDomain.Create("testtenant1"));
    
            var tenant2 = Tenant.Create(
                TenantName.Create("Test Tenant 2"),
                SubDomain.Create("testtenant2"));
    
            Context.Tenants.AddRange(tenant1, tenant2);
    /// <summary>
    /// Disposes resources used by the test, including scope and client, to ensure proper cleanup after each test run.
    /// </summary>
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Scope?.Dispose();
        Client?.Dispose();
        return ValueTask.CompletedTask;
    }
}       Scope?.Dispose();
        Client?.Dispose();
        return ValueTask.CompletedTask;
    }
}