namespace SaaSifyCore.Api.Middleware;

using Microsoft.Extensions.Primitives;

/// <summary>
/// Middleware to log security-related events for audit purposes.
/// Logs authentication attempts, cross-tenant access attempts, and suspicious activity.
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            await _next(context);

            // Log security events after response
            await LogSecurityEventsAsync(context);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private Task LogSecurityEventsAsync(HttpContext context)
    {
        var statusCode = context.Response.StatusCode;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method;

        // Log authentication failures
        if (statusCode == 401)
        {
            _logger.LogWarning(
                "Authentication failed for {Method} {Path}. IP: {IP}, Tenant: {Tenant}",
                method,
                path,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var tenant) ? tenant.ToString() : "None");
        }

        // Log authorization failures (potential cross-tenant access)
        if (statusCode == 403)
        {
            var userId = context.User?.FindFirst("sub")?.Value;
            var tenantInToken = context.User?.FindFirst("tenantId")?.Value;
            var tenantInHeader = context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var headerTenant)
                ? headerTenant.ToString()
                : "None";

            _logger.LogWarning(
                "Authorization failed - Potential cross-tenant access attempt. " +
                "User: {UserId}, TokenTenant: {TokenTenant}, HeaderTenant: {HeaderTenant}, " +
                "Path: {Path}, IP: {IP}",
                userId ?? "Anonymous",
                tenantInToken ?? "None",
                tenantInHeader,
                path,
                context.Connection.RemoteIpAddress?.ToString());
        }

        // Log successful auth endpoints
        if (statusCode >= 200 && statusCode < 300 && path.Contains("/api/auth/"))
        {
            var userId = context.User?.FindFirst("sub")?.Value;
            var email = context.User?.FindFirst("email")?.Value;
            var tenantSubdomain = context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var subdomainHeader)
                ? subdomainHeader.ToString()
                : "None";

            if (path.Contains("/login"))
            {
                _logger.LogInformation(
                    "User login successful. Email: {Email}, Tenant: {Tenant}, IP: {IP}",
                    email ?? "Unknown",
                    tenantSubdomain,
                    context.Connection.RemoteIpAddress?.ToString());
            }
            else if (path.Contains("/register"))
            {
                _logger.LogInformation(
                    "User registration successful. Email: {Email}, Tenant: {Tenant}, IP: {IP}",
                    email ?? "Unknown",
                    tenantSubdomain,
                    context.Connection.RemoteIpAddress?.ToString());
            }
            else if (path.Contains("/logout"))
            {
                _logger.LogInformation(
                    "User logout. User: {UserId}, Tenant: {Tenant}, IP: {IP}",
                    userId ?? "Unknown",
                    tenantSubdomain,
                    context.Connection.RemoteIpAddress?.ToString());
            }
        }

        // Log rate limiting
        if (statusCode == 429)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Method} {Path}. IP: {IP}, Tenant: {Tenant}",
                method,
                path,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var rateLimitTenant)
                    ? rateLimitTenant.ToString()
                    : "None");
        }

        return Task.CompletedTask;
    }
}