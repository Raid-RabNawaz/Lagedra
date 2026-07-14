using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Events;

public sealed record InquiryOfferProposedEvent(
    Guid SessionId,
    Guid OfferId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    Guid ProposedByUserId,
    long RentCents,
    long DepositCents,
    DateTime ProposedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = ProposedAt;
}
