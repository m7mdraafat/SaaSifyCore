using MediatR;
using SaaSifyCore.Application.Commands.Auth;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Application.Queries.Auth;

/// <summary>
/// Query to get current authenticated user information.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="TenantId">The tenant ID from the request context.</param>
public record GetCurrentUserQuery(
    Guid UserId,
    Guid TenantId
) : IRequest<Result<UserDto>>;
