using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Events;

public sealed record ListingPublishedEvent(
    Guid ListingId,
    Guid LandlordUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
