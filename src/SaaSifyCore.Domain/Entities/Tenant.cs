using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Enums;
using SaaSifyCore.Domain.Events.TenantEvents;
using SaaSifyCore.Domain.ValueObjects;
using System;

namespace SaaSifyCore.Domain.Entities;

public class Tenant : BaseEntity
{
    public TenantName Name { get; private set; }
    public SubDomain SubDomain { get; private set; }
    public TenantStatus Status { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }

    // Navigation properties
    private readonly List<User> _users = new();
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    public Subscription? Subscription { get; private set; }

    // EF Core constructor
    private Tenant() { }

    public static Tenant Create(string name, string subDomain)
    {
        var tenant = new Tenant
        {
            Name = TenantName.Create(name),
            SubDomain = SubDomain.Create(subDomain),
            Status = TenantStatus.Active
        };

        tenant.RaiseDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.Name));
        return tenant;
    }

}