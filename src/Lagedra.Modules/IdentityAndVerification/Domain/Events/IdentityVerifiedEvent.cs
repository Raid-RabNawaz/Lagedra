using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Events;

public sealed record IdentityVerifiedEvent(
    Guid ProfileId,
    Guid UserId,
    DateTime VerifiedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
