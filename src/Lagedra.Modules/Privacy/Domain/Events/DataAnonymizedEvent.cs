using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Events;

public sealed record DataAnonymizedEvent(
    Guid UserId,
    DateTime AnonymizedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
