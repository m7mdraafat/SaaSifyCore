namespace SaaSifyCore.Api.DTOs.Auth;

/// <summary>
/// Response returned after successful login (only access token, refresh token in cookie).
/// </summary>
public record LoginResponse(
    string AccessToken,
    int ExpiresIn // Seconds until expiration
);
