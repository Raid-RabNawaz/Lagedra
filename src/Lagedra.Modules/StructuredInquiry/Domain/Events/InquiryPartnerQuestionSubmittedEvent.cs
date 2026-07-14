using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Events;

public sealed record InquiryPartnerQuestionSubmittedEvent(
    Guid SessionId,
    Guid QuestionId,
    Guid ListingId,
    Guid TenantUserId,
    Guid PartnerOrganizationId,
    Guid SubmittedByUserId,
    Guid LandlordUserId,
    DateTime SubmittedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = SubmittedAt;
}
