namespace SaaSifyCore.Domain.Events.SubscriptionEvents;

using SaaSifyCore.Domain.Common;

public record SubscriptionPastDueEvent(Guid SubscriptionId, Guid TenantId) : DomainEvent;