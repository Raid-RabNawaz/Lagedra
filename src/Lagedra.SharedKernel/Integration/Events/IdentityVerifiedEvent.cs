using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

public sealed record IdentityVerifiedEvent(
    Guid ProfileId,
    Guid UserId,
    DateTime VerifiedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
