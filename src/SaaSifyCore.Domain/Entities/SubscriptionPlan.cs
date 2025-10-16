using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Exceptions;

namespace SaaSifyCore.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal PricePerMonth { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxStorageGB { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? StripePriceId { get; private set; }

    private readonly List<Subscription> _subscriptions = new();
    public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();

    // EF Core constructor
    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(
        string name,
        string description,
        decimal pricePerMonth,
        int maxUsers,
        int maxStorageGB,
        string? stripePriceId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Plan name cannot be empty");

        if (pricePerMonth < 0)
            throw new DomainException("Price per month cannot be negative");

        if (maxUsers <= 0)
            throw new DomainException("Max users must be greater than zero");

        if (maxStorageGB <= 0)
            throw new DomainException("Max storage must be greater than zero");

        return new SubscriptionPlan
        {
            Name = name,
            Description = description,
            PricePerMonth = pricePerMonth,
            MaxUsers = maxUsers,
            MaxStorageGB = maxStorageGB,
            StripePriceId = stripePriceId
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void UpdatePricing(decimal newPrice, string? newStripePriceId = null)
    {
        if (newPrice < 0)
            throw new DomainException("Price per month cannot be negative");

        PricePerMonth = newPrice;
        if (!string.IsNullOrEmpty(newStripePriceId))
            StripePriceId = newStripePriceId;
    }
}