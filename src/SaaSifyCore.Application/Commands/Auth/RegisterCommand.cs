using MediatR;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Command to register a new user.
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Guid TenantId
) : IRequest<Result<AuthResponse>>;

/// <summary>
/// Response returned after successful authentication.
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

/// <summary>
/// User data transfer object.
/// </summary>
public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role
);