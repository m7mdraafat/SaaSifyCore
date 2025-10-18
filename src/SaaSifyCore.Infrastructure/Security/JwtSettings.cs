using System.ComponentModel.DataAnnotations;

namespace SaaSifyCore.Infrastructure.Security;

/// <summary>
/// JWT configuration settings.
/// Bond from appsettings.json "JwtSettings" section.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required]
    public string SecretKey { get; init; } = null!;

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    [Required]
    public int ExpiryMinutes { get; init; } = 60;

    [Required]
    public int RefreshTokenExpiryDays { get; init; } = 30;
}