using SaaSifyCore.Domain.Common;
using System;


namespace SaaSifyCore.Domain.Events.TenantEvents;

public record TenantSubscriptionExpiredEvent(Guid TenantId) : DomainEvent;