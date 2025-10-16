namespace SaaSifyCore.Domain.Events.SubscriptionEvents;

using SaaSifyCore.Domain.Common;

public record SubscriptionActivatedEvent(Guid SubscriptionId, Guid TenantId) : DomainEvent;
