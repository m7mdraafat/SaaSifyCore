using System.ComponentModel.DataAnnotations;

namespace SaaSifyCore.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

}