using MediatR;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Command to refresh access token using a valid refresh token.
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken,
    Guid TenantId
) : IRequest<Result<AuthResponse>>;
