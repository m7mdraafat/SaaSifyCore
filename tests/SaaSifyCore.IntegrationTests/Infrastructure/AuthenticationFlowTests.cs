using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Api.DTOs.Auth;
using System.Net;
using System.Net.Http.Json;

namespace SaaSifyCore.IntegrationTests.Infrastructure;

public class AuthenticationFlowTests : IntegrationTestBase
{
    public AuthenticationFlowTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompleteAuthFlow_RegisterLoginRefreshLogout_ShouldSucceed()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act & Assert: Register
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);
        registerResult.Should().NotBeNull();
        registerResult.AccessToken.Should().NotBeNullOrEmpty();
        registerResult.RefreshToken.Should().NotBeNullOrEmpty();
        registerResult.User.Email.Should().Be(registerRequest.Email);

        // Act & Assert: Login
        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken: TestContext.Current.CancellationToken);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();

        // Act & Assert: Access Protected Endpoint
        SetAuthorizationHeader(loginResult.AccessToken);
        var meResponse = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meResult = await meResponse.Content.ReadFromJsonAsync<UserDto>(cancellationToken: TestContext.Current.CancellationToken);
        meResult.Should().NotBeNull();
        meResult!.Email.Should().Be(registerRequest.Email);

        // Act & Assert: Refresh Token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResult.RefreshToken
        };

        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest, cancellationToken: TestContext.Current.CancellationToken);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify old refresh token is revoked
        var oldRefreshToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == loginResult.RefreshToken, cancellationToken: TestContext.Current.CancellationToken);
        oldRefreshToken.Should().NotBeNull();
        oldRefreshToken!.IsRevoked.Should().BeTrue();

        // Act & Assert: Logout
        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshResult.RefreshToken
        };

        SetAuthorizationHeader(refreshResult.AccessToken);
        var logoutResponse = await Client.PostAsJsonAsync("/api/auth/logout", logoutRequest, cancellationToken: TestContext.Current.CancellationToken);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify refresh token is revoked
        var logoutRefreshToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshResult.RefreshToken, cancellationToken: TestContext.Current.CancellationToken);
        logoutRefreshToken.Should().NotBeNull();
        logoutRefreshToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        var registerRequest = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "StrongP@ssw0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "weak",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_DuplicateEmailInSameTenant_ShouldReturnConflict()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        var email = $"test{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "StrongP@ssw0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act - Register first time
        var firstResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Register second time with same email
        var secondResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
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
            Password = "WrongPassword!"
        };

        // Act
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        var loginRequest = new LoginRequest
        {
            Email = $"nonexistent{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        ClearAuthorizationHeader();

        // Act
        var response = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        SetAuthorizationHeader("invalid.jwt.token");

        // Act
        var response = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        SetTenantHeader("testtenant1");
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/auth/refresh",
            refreshRequest,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_ShouldReturnUnauthorized()
    {
        // Arrange
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

        // Act - Logout to revoke the token
        SetAuthorizationHeader(registerResult!.AccessToken);
        await Client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = registerResult.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Act - Try to refresh with revoked token
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = registerResult.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithInvalidRefreshToken_ShouldReturnBadRequest()
    {
        // Arrange
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

        SetAuthorizationHeader(registerResult!.AccessToken);

        // Act - Logout with invalid refresh token
        var logoutResponse = await Client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        logoutResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_WithoutTenantHeader_ShouldReturnBadRequest()
    {
        // Arrange - Don't set tenant header
        Client.DefaultRequestHeaders.Remove("X-Tenant-Subdomain");

        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidTenantSubdomain_ShouldReturnNotFound()
    {
        // Arrange
        SetTenantHeader("nonexistent-tenant");

        var loginRequest = new LoginRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "StrongP@ssw0rd!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateTokens()
    {
        // Arrange
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

        var originalAccessToken = registerResult!.AccessToken;
        var originalRefreshToken = registerResult.RefreshToken;

        // Act - Refresh token
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert - New tokens should be different
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBe(originalAccessToken);
        refreshResult.RefreshToken.Should().NotBe(originalRefreshToken);

        // Assert - Old refresh token should be revoked
        var oldToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == originalRefreshToken, cancellationToken: TestContext.Current.CancellationToken);
        oldToken.Should().NotBeNull();
        oldToken!.IsRevoked.Should().BeTrue();

        // Assert - New access token should work
        SetAuthorizationHeader(refreshResult.AccessToken);
        var meResponse = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleLogins_ShouldGenerateDifferentTokens()
    {
        // Arrange
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

        // Act - Login twice
        var login1Response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken: TestContext.Current.CancellationToken);
        var login1Result = await login1Response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        var login2Response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken: TestContext.Current.CancellationToken);
        var login2Result = await login2Response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Both logins should succeed with different tokens
        login1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        login2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        login1Result!.AccessToken.Should().NotBe(login2Result!.AccessToken);
        login1Result.RefreshToken.Should().NotBe(login2Result.RefreshToken);

        // Assert - Both tokens should work
        SetAuthorizationHeader(login1Result.AccessToken);
        var me1Response = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);
        me1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        SetAuthorizationHeader(login2Result.AccessToken);
        var me2Response = await Client.GetAsync("/api/auth/me", cancellationToken: TestContext.Current.CancellationToken);
        me2Response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}