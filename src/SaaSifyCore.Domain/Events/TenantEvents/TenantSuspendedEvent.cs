using SaaSifyCore.Domain.Common;
using System;


namespace SaaSifyCore.Domain.Events.TenantEvents;

public record TenantSuspendedEvent(Guid TenantId, string Reason) : DomainEvent;
