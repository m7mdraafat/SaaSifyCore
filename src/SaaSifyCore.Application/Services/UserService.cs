using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Application.DTOs;
using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Interfaces;
using SaaSifyCore.Domain.ValueObjects;

namespace SaaSifyCore.Application.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _dbContext;
    public UserService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Result<UserDto>> GetCurrentUserAsync(
        string externalId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.ExternalId == externalId)
            .Select(u => new
            {
                u.Id,
                Email = u.Email.Value,
                u.FirstName,
                u.LastName,
                u.Role,
                u.TenantId
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user == null)
        {
            return Result.Failure<UserDto>(DomainErrors.User.NotFound);
        }

        if (user.TenantId != tenantId)
        {
            return Result.Failure<UserDto>(DomainErrors.User.TenantMismatch);
        }

        return Result.Success(new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString()
        ));
    }
}