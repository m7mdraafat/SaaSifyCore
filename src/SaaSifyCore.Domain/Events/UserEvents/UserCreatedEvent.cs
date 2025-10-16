namespace SaaSifyCore.Domain.Events.UserEvents;

using global::SaaSifyCore.Domain.Common;
using System;

public record UserCreatedEvent(Guid UserId, string Email, Guid TenantId) : DomainEvent;
