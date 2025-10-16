namespace SaaSifyCore.Domain.Events.UserEvents;

using global::SaaSifyCore.Domain.Common;
using System;

public record UserPasswordChangedEvent(Guid UserId) : DomainEvent;