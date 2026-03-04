using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Auth.Domain.Events;

/// <summary>Fired after a user verifies their email. Downstream modules listen to create their identity profile.</summary>
public sealed record UserRegisteredEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    UserRole Role) : IDomainEvent;
