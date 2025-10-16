using SaaSifyCore.Domain.Common;
using System;


namespace SaaSifyCore.Domain.Events.TenantEvents;

public record TenantActivatedEvent(Guid TenantId) : DomainEvent;

