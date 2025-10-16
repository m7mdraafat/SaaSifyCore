using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Enums;
using SaaSifyCore.Domain.Events.SubscriptionEvents;
using SaaSifyCore.Domain.Exceptions;

namespace SaaSifyCore.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;

    public Guid PlanId { get; private set; }
    public SubscriptionPlan Plan { get; private set; } = null!;

    public SubscriptionStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // Stripe Integration
    public string? StripeSubscriptionId { get; private set; }
    public string? StripeCustomerId { get; private set; }

    // EF Core constructor
    private Subscription() { }

    public static Subscription CreateTrial(Guid tenantId, Guid planId, int trialDays = 14)
    {
        var subscription = new Subscription
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Trialing,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(trialDays)
        };

        subscription.RaiseDomainEvent(new SubscriptionCreatedEvent(subscription.Id, tenantId, true));
        return subscription;
    }

    public static Subscription CreatePaid(
        Guid tenantId,
        Guid planId,
        string stripeSubscriptionId,
        string stripeCustomerId)
    {
        var subscription = new Subscription
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            StripeSubscriptionId = stripeSubscriptionId,
            StripeCustomerId = stripeCustomerId
        };

        subscription.RaiseDomainEvent(new SubscriptionCreatedEvent(subscription.Id, tenantId, false));
        return subscription;
    }

    public void Activate()
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new DomainException("Cannot activate a cancelled subscription");

        if (Status == SubscriptionStatus.Active)
            return;

        Status = SubscriptionStatus.Active;
        RaiseDomainEvent(new SubscriptionActivatedEvent(Id, TenantId));
    }

    public void Cancel()
    {
        if (Status == SubscriptionStatus.Cancelled)
            return;

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        EndDate = CancelledAt;
        RaiseDomainEvent(new SubscriptionCancelledEvent(Id, TenantId));
    }

    public void MarkPastDue()
    {
        if (Status != SubscriptionStatus.Active)
            throw new DomainException("Only active subscriptions can be marked as past due");

        Status = SubscriptionStatus.PastDue;
        RaiseDomainEvent(new SubscriptionPastDueEvent(Id, TenantId));
    }
    public bool IsActive() => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trialing;
}
