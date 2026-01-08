using SaaSifyCore.Application.DTOs;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Application.Services;

public interface IUserService
{
    /// <summary>
    /// Retrieves the current user based on their external identifier and tenant.
    /// </summary>
    /// <param name="externalId">The external id for keycloak user.</param>
    /// <param name="tenantId">The unique identifier of the tenant the user belongs to.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result{T}"/> with the <see cref="UserDto"/> if successful.</returns>
    Task<Result<UserDto>> GetCurrentUserAsync(string externalId, Guid tenantId, CancellationToken cancellationToken = default);
}