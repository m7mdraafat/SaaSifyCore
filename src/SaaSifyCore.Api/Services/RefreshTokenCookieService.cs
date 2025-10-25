using Microsoft.AspNetCore.Http;

namespace SaaSifyCore.Api.Services;

/// <summary>
/// Implementation of refresh token cookie management.
/// </summary>
public class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private const string CookieName = "refreshToken";
    private const string CookiePath = "/api/auth";
    private const int RefreshTokenExpirationDays = 7;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenCookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        var cookieOptions = CreateSecureCookieOptions(expiresAt);
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(
            CookieName,
            refreshToken,
            cookieOptions);
    }

    public string? GetRefreshTokenFromCookie()
    {
        if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue(CookieName, out var refreshToken) == true)
        {
            return string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken;
        }
        return null;
    }

    public void DeleteRefreshTokenCookie()
    {
        var cookieOptions = CreateSecureCookieOptions(DateTime.UtcNow.AddDays(-1));
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(CookieName, cookieOptions);
    }

    private static CookieOptions CreateSecureCookieOptions(DateTime expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = CookiePath
        };
    }
}
