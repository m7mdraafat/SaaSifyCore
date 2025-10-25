namespace SaaSifyCore.Api.Services;

/// <summary>
/// Service for managing refresh token cookies.
/// Encapsulates cookie configuration and security settings.
/// </summary>
public interface IRefreshTokenCookieService
{
    /// <summary>
    /// Sets the refresh token as an HTTP-only secure cookie.
    /// </summary>
    void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt);

    /// <summary>
    /// Retrieves the refresh token from the cookie.
    /// </summary>
    string? GetRefreshTokenFromCookie();

    /// <summary>
    /// Deletes the refresh token cookie.
    /// </summary>
    void DeleteRefreshTokenCookie();
}
