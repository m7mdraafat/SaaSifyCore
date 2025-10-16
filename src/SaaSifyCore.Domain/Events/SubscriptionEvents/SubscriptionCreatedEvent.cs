namespace SaaSifyCore.Domain.Events.SubscriptionEvents;

using SaaSifyCore.Domain.Common;

public record SubscriptionCreatedEvent(Guid SubscriptionId, Guid TenantId, bool IsTrial) : DomainEvent;
