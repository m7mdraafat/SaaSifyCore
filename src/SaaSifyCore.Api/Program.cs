using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using SaaSifyCore.Api.Configuration;
using SaaSifyCore.Api.Middleware;
using SaaSifyCore.Application;
using SaaSifyCore.Infrastructure;
using SaaSifyCore.Infrastructure.Data;

// Configure Serilog BEFORE building the host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SaaSifyCore")
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting SaaSifyCore API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add Application layer services (MediatR, CQRS handlers)
    builder.Services.AddApplication();

    builder.Services.AddInfrastructure(builder.Configuration);

    // Add API-specific services to the container.
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorizationPolicies();

    builder.Services.AddRateLimiting(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer(); // Required for API endpoint exploration
    builder.Services.AddCorsPolicy(builder.Configuration);

    // Add API services (cookie service, result mapper)
    builder.Services.AddApiServices();

    builder.Services.AddDistributedMemoryCache();

    var app = builder.Build();

    // Add Serilog request logging (logs HTTP requests)
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value!);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString()!);

            // Add tenant information if available
            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var subdomain))
            {
                diagnosticContext.Set("TenantSubdomain", subdomain.ToString());
            }

            // Add user information if authenticated
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value!);
                diagnosticContext.Set("UserEmail", httpContext.User.FindFirst("email")?.Value!);
            }
        };
    });

    // Security audit logging
    app.UseMiddleware<SecurityAuditMiddleware>();

    // Tenant resolution (identifies tenant)
    app.UseMiddleware<TenantResolutionMiddleware>();

    // Rate limiting (after tenant identification) - only in non-Testing environments
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseRateLimiting();
    }
    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

    app.MapControllers();

    Log.Information("SaaSifyCore API started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }