using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Api.DTOs.Auth;
using System.Net;
using System.Net.Http.Json;

namespace SaaSifyCore.IntegrationTests.Infrastructure;

/// <summary>
/// Tests to ensure tenant isolation is properly enforced.
/// SECURITY CRITICAL: Validates that tenants cannot access each other's data.
/// </summary>
public class CrossTenantSecurityTests : IntegrationTestBase
{
    public CrossTenantSecurityTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithTokenFromDifferentTenant_ShouldFail()
    {
        // Arrange - Register user in tenant1
        SetTenantHeader("testtenant1");
        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Act - Try to use tenant1 token with tenant2 header
        SetTenantHeader("testtenant2");
        SetAuthorizationHeader(registerResult!.AccessToken);
        var meResponse = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_SameEmailDifferentTenants_ShouldSucceed()
    {
        // Arrange
        var email = $"test{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "StrongP@ssw0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act - Register in tenant1
        SetTenantHeader("testtenant1");
        var response1 = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Act - Register same email in tenant2
        SetTenantHeader("testtenant2");
        var response2 = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Both should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify users exist in different tenants
        var tenant1User = await Context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.Value == email && u.Tenant.Subdomain.Value == "testtenant1", cancellationToken: TestContext.Current.CancellationToken);

        var tenant2User = await Context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.Value == email && u.Tenant.Subdomain.Value == "testtenant2", cancellationToken: TestContext.Current.CancellationToken);

        tenant1User.Should().NotBeNull();
        tenant2User.Should().NotBeNull();
        tenant1User!.Id.Should().NotBe(tenant2User!.Id);
    }

    [Fact]
    public async Task RefreshToken_FromDifferentTenant_ShouldFail()
    {
        // Arrange - Register and login in tenant1
        SetTenantHeader("testtenant1");
        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken: TestContext.Current.CancellationToken);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Act - Try to use refresh token from tenant2
        SetTenantHeader("testtenant2");
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResult!.RefreshToken
        };

        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Should fail (either 400 or 401 is acceptable for security)
        refreshResponse.IsSuccessStatusCode.Should().BeFalse();
        refreshResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_QueryFiltersShouldPreventCrossTenantAccess()
    {
        // Arrange - Create users in both tenants
        SetTenantHeader("testtenant1");
        var tenant1Request = new RegisterRequest
        {
            Email = $"tenant1{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!",
            FirstName = "Tenant1",
            LastName = "User"
        };

        var tenant1Response = await Client.PostAsJsonAsync("/api/auth/register", tenant1Request, cancellationToken: TestContext.Current.CancellationToken);
        tenant1Response.EnsureSuccessStatusCode(); // Check for errors
        var tenant1Result = await tenant1Response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        SetTenantHeader("testtenant2");
        var tenant2Request = new RegisterRequest
        {
            Email = $"tenant2{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!",
            FirstName = "Tenant2",
            LastName = "User"
        };

        var tenant2Response = await Client.PostAsJsonAsync("/api/auth/register", tenant2Request, cancellationToken: TestContext.Current.CancellationToken);
        tenant2Response.EnsureSuccessStatusCode(); // Check for errors

        // Act - Verify tenant1 user can only see themselves
        SetTenantHeader("testtenant1");
        SetAuthorizationHeader(tenant1Result!.AccessToken);
        var meResponse = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Should succeed
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Only try to deserialize if response is successful
        if (meResponse.IsSuccessStatusCode)
        {
            var meResult = await meResponse.Content.ReadFromJsonAsync<UserDto>(cancellationToken: TestContext.Current.CancellationToken);
            meResult.Should().NotBeNull();
            meResult!.Email.Should().Be(tenant1Request.Email);
        }
    }

    [Fact]
    public async Task Logout_ShouldOnlyRevokeCurrentTenantToken()
    {
        // Arrange - Create users in both tenants with same email
        var email = $"test{Guid.NewGuid()}@example.com";
        var password = "StrongP@ssw0rd!";

        SetTenantHeader("testtenant1");
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        }, cancellationToken: TestContext.Current.CancellationToken);

        var tenant1LoginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        }, cancellationToken: TestContext.Current.CancellationToken);
        var tenant1LoginResult = await tenant1LoginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        SetTenantHeader("testtenant2");
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        }, cancellationToken: TestContext.Current.CancellationToken);

        var tenant2LoginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        }, cancellationToken: TestContext.Current.CancellationToken);
        var tenant2LoginResult = await tenant2LoginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Act - Logout from tenant1
        SetTenantHeader("testtenant1");
        SetAuthorizationHeader(tenant1LoginResult!.AccessToken);

        var logoutResponse = await Client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = tenant1LoginResult.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - Try to use tenant1's refresh token (should fail)
        var tenant1RefreshAttempt = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = tenant1LoginResult.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        tenant1RefreshAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "revoked token should be rejected");

        // Assert - Tenant2 token should still work
        SetTenantHeader("testtenant2");
        var tenant2RefreshAttempt = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = tenant2LoginResult!.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        tenant2RefreshAttempt.StatusCode.Should().Be(HttpStatusCode.OK,
            "tenant2 token should still be valid");

        // Verify tenant2 can still access protected endpoints
        var newTenant2Auth = await tenant2RefreshAttempt.Content.ReadFromJsonAsync<AuthResponse>();
        SetAuthorizationHeader(newTenant2Auth!.AccessToken);
        var tenant2MeResponse = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);
        tenant2MeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
