using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.Enums;
using SaaSifyCore.Domain.Interfaces;
using SaaSifyCore.Domain.ValueObjects;

namespace SaaSifyCore.Application.Commands.Auth;

/// <summary>
/// Handles user registration with validation and token generation.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<RegisterCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing registration for email: {Email}", request.Email);

        // 1. Validate email format
        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Invalid email format: {Email}", request.Email);
            return Result.Failure<AuthResponse>(
                new Error("Validation.InvalidFormat", ex.Message));
        }

        // 2. Check if tenant exists and is active
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
        {
            _logger.LogWarning("Tenant not found: {TenantId}", request.TenantId);
            return Result.Failure<AuthResponse>(DomainErrors.Tenant.NotFound);
        }

        if (tenant.Status != TenantStatus.Active)
        {
            _logger.LogWarning("Tenant not active: {TenantId}, Status: {Status}",
                request.TenantId, tenant.Status);
            return Result.Failure<AuthResponse>(DomainErrors.Tenant.NotActive);
        }

        // 3. Check if email already exists
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("Email already exists: {Email}", request.Email);
            return Result.Failure<AuthResponse>(DomainErrors.User.EmailAlreadyExists);
        }

        // 4. Validate password
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure<AuthResponse>(
                new Error("Validation.Required", "Password is required"));
        }

        if (request.Password.Length < 8)
        {
            return Result.Failure<AuthResponse>(DomainErrors.Validation.TooShort("Password", 8));
        }

        // 5. Validate names
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return Result.Failure<AuthResponse>(DomainErrors.Validation.Required("FirstName"));
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Failure<AuthResponse>(DomainErrors.Validation.Required("LastName"));
        }

        // 6. Hash password
        var passwordHash = await _passwordHasher.HashPasswordAsync(request.Password);

        // 7. Create user entity (raises UserCreatedEvent)
        var user = User.Create(
            email,
            passwordHash,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            UserRole.User, // Default role
            request.TenantId);

        // 8. Create refresh token
        var refreshToken = RefreshToken.Create(user.Id, expiryDays: 30);

        // 9. Save to database
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully: {UserId}, Email: {Email}",
            user.Id, user.Email.Value);

        // 10. Generate JWT token
        var accessToken = _jwtTokenGenerator.GenerateToken(user, request.TenantId);
        var expiresAt = DateTime.UtcNow.AddHours(1); // TODO: Make configurable

        // 11. Build response
        var response = new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAt: expiresAt,
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
