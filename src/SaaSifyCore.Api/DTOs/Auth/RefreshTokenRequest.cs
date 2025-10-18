using System.ComponentModel.DataAnnotations;

namespace SaaSifyCore.Api.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}