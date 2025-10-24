using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.Interfaces;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Handler for RefreshTokenCommand.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find refresh token (ignore tenant filter to validate tenant manually)
        var refreshToken = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        // Validate token exists and belongs to the correct tenant
        if (refreshToken is null || refreshToken.User.TenantId != request.TenantId)
        {
            // TODO: Log security event if tenant mismatch
            return Result.Failure<AuthResponse>(DomainErrors.Auth.InvalidToken);
        }

        // Check if token is revoked
        if (refreshToken.IsRevoked)
        {
            return Result.Failure<AuthResponse>(DomainErrors.Auth.RefreshTokenRevoked);
        }

        // Check if token is expired
        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Result.Failure<AuthResponse>(DomainErrors.Auth.TokenExpired);
        }

        var user = refreshToken.User;

        // Revoke old token
        refreshToken.Revoke();

        // Generate new tokens
        var newAccessToken = _jwtTokenGenerator.GenerateToken(user, request.TenantId);
        var newRefreshToken = RefreshToken.Create(user.Id);

        await _context.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Build response
        var response = new AuthResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresAt: DateTime.UtcNow.AddMinutes(60),
            User: new UserDto(
                Id: user.Id,
                Email: user.Email.Value,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Role: user.Role.ToString()
            )
        );

        return Result.Success(response);
    }
}
