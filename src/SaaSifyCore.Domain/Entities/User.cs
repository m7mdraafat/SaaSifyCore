using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Enums;
using SaaSifyCore.Domain.Events.UserEvents;
using SaaSifyCore.Domain.Exceptions;
using SaaSifyCore.Domain.ValueObjects;
using System;

namespace SaaSifyCore.Domain.Entities;

public class User : BaseEntity
{
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; } = false;

    // Multi-tenancy
    public Guid TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;

    // Token Management
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // EF Core constructor
    private User() { }

    public static User Create(
        Email email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role,
        Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty");

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            TenantId = tenantId
        };
        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, user.Email.Value, user.TenantId));
        return user;
    }

    public void VerifyEmail()
    {
        if (IsEmailVerified)
            return;
        
        IsEmailVerified = true;
        RaiseDomainEvent(new UserEmailVerifiedEvent(Id, Email.Value));
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("New password hash cannot be empty");

        PasswordHash = newPasswordHash;
        RaiseDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void PromoteToAdmin()
    {
        if (Role == UserRole.SuperAdmin)
            throw new DomainException("Cannot promote a SuperAdmin");

        Role = UserRole.Admin;
    }

    public string FullName => $"{FirstName} {LastName}";
}