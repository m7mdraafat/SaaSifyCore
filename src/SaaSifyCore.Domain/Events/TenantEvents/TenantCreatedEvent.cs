using SaaSifyCore.Domain.Common;
using System;


namespace SaaSifyCore.Domain.Events.TenantEvents;

public record TenantCreatedEvent(Guid TenantId, string TenantName) : DomainEvent;
