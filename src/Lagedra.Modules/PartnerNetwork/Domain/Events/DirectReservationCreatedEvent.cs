using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.PartnerNetwork.Domain.Events;

public sealed record DirectReservationCreatedEvent(
    Guid OrganizationId,
    Guid ReservationId,
    Guid ListingId,
    string GuestEmail) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
