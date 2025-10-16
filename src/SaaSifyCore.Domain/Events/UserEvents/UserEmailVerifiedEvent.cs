namespace SaaSifyCore.Domain.Events.UserEvents;

using global::SaaSifyCore.Domain.Common;
using System;

public record UserEmailVerifiedEvent(Guid UserId, string Email) : DomainEvent;
