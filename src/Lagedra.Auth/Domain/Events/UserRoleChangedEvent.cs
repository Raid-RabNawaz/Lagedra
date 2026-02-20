using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Auth.Domain.Events;

public sealed record UserRoleChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    UserRole OldRole,
    UserRole NewRole) : IDomainEvent;
