using System.ComponentModel.DataAnnotations;

namespace SaaSifyCore.Api.DTOs.Auth;
public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$",
        ErrorMessage = "Password must contain uppercase, lowercase, number, and special character")]
    public string Password { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string LastName { get; init; } = string.Empty;
}
