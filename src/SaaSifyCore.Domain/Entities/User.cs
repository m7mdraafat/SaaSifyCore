using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Enums;
using SaaSifyCore.Domain.Exceptions;
using SaaSifyCore.Domain.ValueObjects;
using System;

namespace SaaSifyCore.Domain.Entities;

public class User : BaseEntity
{
    // Keycloak external identity
    public string? ExternalId { get; private set; }
    
    public Email Email { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    // Multi-tenancy
    public Guid TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;

    // EF Core constructor
    private User() { }

    public static User Create(
        string externalId,
        Email email,
        string firstName,
        string lastName,
        UserRole role,
        Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new DomainException("External ID cannot be empty");

        var user = new User
        {
            ExternalId = externalId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            TenantId = tenantId
        };

        return user;
    }

    public void PromoteToAdmin()
    {
        if (Role == UserRole.SuperAdmin)
            throw new DomainException("Cannot promote a SuperAdmin");

        Role = UserRole.Admin;
    }

    public string FullName => $"{FirstName} {LastName}";
}