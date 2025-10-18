using SaaSifyCore.Domain.Entities;

namespace SaaSifyCore.Domain.Interfaces;

/// <summary>
/// Generates JWT tokens for authentication.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    /// <param name="user">Authenticated user</param>
    /// <param name="tenantId">Tenant context</param>
    /// <returns>JWT token string.</returns>
    string GenerateToken(User user, Guid tenantId);
}