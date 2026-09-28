using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Events;

/// <summary>
/// Raised when the guest actually submits a question. This is what emails
/// the host — not the moment an empty thread is opened.
/// </summary>
public sealed record InquiryTenantQuestionSubmittedEvent(
    Guid SessionId,
    Guid QuestionId,
    Guid ListingId,
    Guid LandlordUserId,
    Guid TenantUserId,
    string QuestionText,
    DateTime SubmittedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = SubmittedAt;
}
