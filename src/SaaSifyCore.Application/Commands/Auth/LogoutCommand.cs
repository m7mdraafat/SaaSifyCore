using MediatR;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Command to logout a user by revoking their refresh token.
/// </summary>
public record LogoutCommand(
    string RefreshToken,
    Guid TenantId
) : IRequest<Result>;
