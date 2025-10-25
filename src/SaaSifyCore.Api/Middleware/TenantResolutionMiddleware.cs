using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.Enums;
using SaaSifyCore.Domain.Interfaces;
using SaaSifyCore.Domain.ValueObjects;
using SaaSifyCore.Infrastructure.MultiTenancy;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SaaSifyCore.Api.Middleware;

/// <summary>
/// Middleware for resolving tenant context from subdomain.
/// Production: Extracts subdomain from Host header (e.g., acme.saasify.com -> "acme")
/// Development: Falls back to X-Tenant-Subdomain header when running on localhost
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private static readonly Regex SubdomainRegex =
        new("^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TenantResolutionMiddleware(RequestDelegate next, IDistributedCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IApplicationDbContext dbContext)
    {
        // Skip for swagger, health, static assets.
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/swagger") || path.StartsWith("/health") ||
            path.StartsWith("/favicon.ico") || path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var subdomain = ExtractSubdomain(context);
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant subdomain not provided.");
            return;
        }

        if (!SubdomainRegex.IsMatch(subdomain))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid tenant subdomain format.");
            return;
        }

        var cacheKey = $"tenant:{subdomain}";
        var cachedTenant = await _cache.GetStringAsync(cacheKey);

        Tenant? tenant;
        if (!string.IsNullOrEmpty(cachedTenant))
        {
            tenant = JsonSerializer.Deserialize<TenantCacheDto>(cachedTenant)?.ToTenant();
        }
        else
        {
            // Cache miss - fetch from database
            tenant = await dbContext.Tenants
                .Where(t => t.Subdomain == SubDomain.Create(subdomain))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (tenant is not null)
            {
                // Cache for 1 hour
                var cacheDto = TenantCacheDto.FromTenant(tenant);
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(cacheDto),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    });
            }
        }

        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Tenant not found.");
            return;
        }

        if (tenant.Status != TenantStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Tenant is not active.");
            return;
        }

        // Populate tenant context
        if (tenantContext is TenantContext concrete)
        {
            concrete.Set(tenant.Id, tenant.Subdomain.Value);
        }

        await _next(context);
    }

    private static string? ExtractSubdomain(HttpContext context)
    {
        // Primary: Subdomain from Host (e.g., acme.saasify.com -> "acme")
        var host = context.Request.Host.Host;
        
        // For localhost development, allow header as fallback
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("127.0.0.1") ||
            host.StartsWith("::1"))
        {
            // Development fallback: Allow header for testing
            if (context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var devHeader) && 
                !string.IsNullOrWhiteSpace(devHeader))
            {
                return devHeader.ToString().Trim();
            }
            return null;
        }

        // Production: Extract subdomain from host
        var parts = host.Split('.');
        if (parts.Length >= 3) // e.g., acme.saasify.com or acme.api.saasify.com
        {
            return parts[0].Trim();
        }

        return null;
    }

    private record TenantCacheDto(Guid Id, string Subdomain, TenantStatus Status)
    {
        public static TenantCacheDto FromTenant(Tenant tenant) =>
            new(tenant.Id, tenant.Subdomain.Value, tenant.Status);

        public Tenant ToTenant()
        {
            // Use reflection or factory to reconstruct minimal tenant
            var tenant = (Tenant)Activator.CreateInstance(typeof(Tenant), true)!;
            typeof(Tenant).GetProperty("Id")!.SetValue(tenant, Id);
            typeof(Tenant).GetProperty("Subdomain")!.SetValue(tenant, SubDomain.Create(Subdomain));
            typeof(Tenant).GetProperty("Status")!.SetValue(tenant, Status);
            return tenant;
        }
    }
}