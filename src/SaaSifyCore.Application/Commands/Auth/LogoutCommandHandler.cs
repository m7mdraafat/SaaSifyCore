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
            return Result.Failure(DomainErrors.Auth.InvalidToken);
        }

        // Revoke the token
        refreshToken.Revoke();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
