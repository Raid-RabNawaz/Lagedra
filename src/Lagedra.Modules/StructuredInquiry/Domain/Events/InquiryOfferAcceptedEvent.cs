using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Events;

public sealed record InquiryOfferAcceptedEvent(
    Guid SessionId,
    Guid OfferId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    Guid AcceptedByUserId,
    long RentCents,
    long DepositCents,
    DateTime AcceptedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = AcceptedAt;
}
