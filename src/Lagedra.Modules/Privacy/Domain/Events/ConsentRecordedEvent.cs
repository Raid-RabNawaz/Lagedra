using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Events;

public sealed record ConsentRecordedEvent(
    Guid UserId,
    ConsentType ConsentType,
    DateTime GrantedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
