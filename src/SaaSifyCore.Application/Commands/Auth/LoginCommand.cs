using MediatR;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Command to login a user with email and password.
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    Guid TenantId
) : IRequest<Result<AuthResponse>>;
