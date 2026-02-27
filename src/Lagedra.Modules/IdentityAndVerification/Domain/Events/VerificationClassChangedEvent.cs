using Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Events;

public sealed record VerificationClassChangedEvent(
    Guid ProfileId,
    Guid UserId,
    VerificationClass OldClass,
    VerificationClass NewClass) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
