using Lagedra.Modules.PartnerNetwork.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.PartnerNetwork.Domain.Entities;

public sealed class DirectReservation : Entity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public string GuestName { get; private set; } = string.Empty;
    public string GuestEmail { get; private set; } = string.Empty;
    public Guid ListingId { get; private set; }
    public Guid? DealApplicationId { get; private set; }
    public Guid ReservedByUserId { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private DirectReservation() { }

    public static DirectReservation Create(
        Guid organizationId,
        string guestName,
        string guestEmail,
        Guid listingId,
        Guid reservedByUserId,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guestName);
        ArgumentException.ThrowIfNullOrWhiteSpace(guestEmail);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        var reservation = new DirectReservation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GuestName = guestName,
            GuestEmail = guestEmail,
            ListingId = listingId,
            ReservedByUserId = reservedByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        reservation._domainEvents.Add(new DirectReservationCreatedEvent(
            organizationId, reservation.Id, listingId, guestEmail));

        return reservation;
    }

    public void LinkDealApplication(Guid dealApplicationId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        DealApplicationId = dealApplicationId;
        UpdatedAt = clock.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
