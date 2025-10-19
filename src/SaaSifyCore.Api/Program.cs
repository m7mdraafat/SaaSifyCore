using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Api.Configuration;
using SaaSifyCore.Api.Middleware;
using SaaSifyCore.Infrastructure;
using SaaSifyCore.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// Add API-specific services to the container.
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

builder.Services.AddRateLimiting(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy(builder.Configuration);

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SaaSifyCore API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

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

app.Run();

public partial class Program {}