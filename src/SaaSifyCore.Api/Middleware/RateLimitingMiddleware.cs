using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SaaSifyCore.Api.Middleware;

/// <summary>
/// Custom rate limiting middleware with per-tenant and per-IP tracking.
/// Provides protection against brute force attacks on authentication endpoints.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Rate limit configurations per endpoints
    private static readonly Dictionary<string, RateLimitConfig> _rateLimits = new()
    {
        ["POST:/api/auth/login"] = new RateLimitConfig(5, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)),
        ["POST:/api/auth/register"] = new RateLimitConfig(3, TimeSpan.FromHours(1), TimeSpan.FromHours(2)),
        ["POST:/api/auth/refresh"] = new RateLimitConfig(10, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)),
        ["POST:/api/auth/logout"] = new RateLimitConfig(10, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)),
    };

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = $"{context.Request.Method}:{context.Request.Path.Value}";

        if (!_rateLimits.TryGetValue(endpoint, out var config))
        {
            await _next(context);
            return;
        }

        // Generate unique key based on IP + tenant + endpoint
        var clientKey = GenerateClientKey(context, endpoint);
        var cacheKey = $"ratelimit:{clientKey}";
        var blockCacheKey = $"ratelimit:block:{clientKey}";

        if (_cache.TryGetValue(blockCacheKey, out _))
        {
            await ReturnRateLimitResponse(context, 0, config.BlockDuration);
            return;
        }

        // Get or create rate limit entry
        var rateLimitEntry = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = config.Window;
            return new RateLimitEntry
            {
                Count = 0,
                ResetTime = DateTimeOffset.UtcNow.Add(config.Window)
            };
        });

        // Increment request count
        rateLimitEntry!.Count++;
        _cache.Set(cacheKey, rateLimitEntry);

        // Check if limit exceeded
        if (rateLimitEntry.Count > config.Limit)
        {
            // Block the client for extended period
            _cache.Set(blockCacheKey, true, config.BlockDuration);

            _logger.LogWarning(
                $"Rate limit exceeded for {clientKey} on {endpoint}. Blocked for {config.BlockDuration}");

            await ReturnRateLimitResponse(context, 0, config.BlockDuration);
        }

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = config.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (config.Limit - rateLimitEntry.Count).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = rateLimitEntry.ResetTime.ToUnixTimeSeconds().ToString();

        await _next(context);
    }

    private static string GenerateClientKey(HttpContext context, string endpoint)
    {
        // Combine IP address, tenant header, and endpoint for unique key
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tenant = context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var tenantValue)
            ? tenantValue.ToString()
            : context.Request.Query.TryGetValue("tenantSubdomain", out var queryTenant)
                ? queryTenant.ToString()
                : "notenant";

        var combined = $"{ipAddress}|{tenant}|{endpoint}";

        // Hash to keep keys consistent and manageable
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hashBytes);
    }
    
    private async Task ReturnRateLimitResponse(HttpContext context, int remaining, TimeSpan retryAfter)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

        var response = new
        {
            statusCode = 429,
            message = "Too many requests. Please try again later.",
            retryAfter = (int)retryAfter.TotalSeconds
        };

        await context.Response.WriteAsJsonAsync(response);
    }
    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTimeOffset ResetTime { get; set; }
    }

    private record RateLimitConfig(int Limit, TimeSpan Window, TimeSpan BlockDuration);
}
