using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Interfaces;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Handler for LogoutCommand.
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public LogoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Find refresh token (tenant filter automatically applied)
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            // Token not found - could be invalid or doesn't belong to tenant
            return Result.Failure(DomainErrors.Auth.InvalidToken);
        }

        // Check if token is already revoked
        if (refreshToken.IsRevoked)
        {
            return Result.Failure(DomainErrors.Auth.RefreshTokenRevoked);
        }

        // Revoke the token
        refreshToken.Revoke();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
