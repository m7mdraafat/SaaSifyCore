namespace SaaSifyCore.Api.Configuration;

/// <summary>
/// Constants for authentication configuration.
/// Centralizes magic numbers for maintainability.
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// Access token expiration in seconds (15 minutes).
    /// </summary>
    public const int AccessTokenExpirationSeconds = 900;

    /// <summary>
    /// Refresh token expiration in days.
    /// </summary>
    public const int RefreshTokenExpirationDays = 7;

    /// <summary>
    /// Cookie name for refresh token.
    /// </summary>
    public const string RefreshTokenCookieName = "refreshToken";

    /// <summary>
    /// Cookie path restriction for auth endpoints.
    /// </summary>
    public const string CookiePath = "/api/auth";
}
