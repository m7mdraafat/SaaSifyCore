namespace SaaSifyCore.Domain.Events.SubscriptionEvents;

using SaaSifyCore.Domain.Common;

public record SubscriptionCancelledEvent(Guid SubscriptionId, Guid TenantId) : DomainEvent;
