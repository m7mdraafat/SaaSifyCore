using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.Interfaces;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Handler for LoginCommand.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by email and tenant
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == request.TenantId, cancellationToken);

        // Use constant-time comparison to prevent timing attacks
        string dummyHash = "$2a$12$dummy.hash.for.timing.constant.protection";
        string hashToVerify = user?.PasswordHash ?? dummyHash;
        bool isValid = await _passwordHasher.VerifyPasswordAsync(request.Password, hashToVerify);

        if (user is null || !isValid)
        {
            return Result.Failure<AuthResponse>(DomainErrors.User.InvalidCredentials);
        }

        // Check if email is verified (optional - uncomment if required)
        // if (!user.IsEmailVerified)
        // {
        //     return Result.Failure<AuthResponse>(DomainErrors.User.EmailNotVerified);
        // }

        // Manage existing refresh tokens - keep only the 4 most recent
        var existingTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync(cancellationToken);

        if (existingTokens.Count >= 4)
        {
            var tokensToRevoke = existingTokens.Skip(3);
            foreach (var token in tokensToRevoke)
            {
                token.Revoke();
            }
        }

        // Generate new tokens
        string accessToken = _jwtTokenGenerator.GenerateToken(user, request.TenantId);
        RefreshToken refreshToken = RefreshToken.Create(user.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Build response
        var response = new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
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
