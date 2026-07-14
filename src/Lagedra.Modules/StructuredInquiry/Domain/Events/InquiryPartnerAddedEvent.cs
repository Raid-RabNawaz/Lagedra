using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Events;

public sealed record InquiryPartnerAddedEvent(
    Guid SessionId,
    Guid ListingId,
    Guid TenantUserId,
    Guid PartnerOrganizationId,
    Guid AddedByUserId,
    Guid LandlordUserId,
    DateTime AddedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = AddedAt;
}
