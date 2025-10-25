using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Application.Commands.Auth;
using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Interfaces;

namespace SaaSifyCore.Application.Queries.Auth;

/// <summary>
/// Handler for GetCurrentUserQuery.
/// Retrieves user information and validates tenant membership.
/// </summary>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCurrentUserQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        // Single query - get user without tenant filter, then validate tenant
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.Id,
                u.Email.Value,
                u.FirstName,
                u.LastName,
                u.Role,
                u.TenantId
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(DomainErrors.User.UserNotFound);
        }

        // Validate tenant membership
        if (user.TenantId != request.TenantId)
        {
            return Result.Failure<UserDto>(DomainErrors.User.TenantMismatch);
        }

        var userDto = new UserDto(
            user.Id,
            user.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString()
        );

        return Result.Success(userDto);
    }
}
