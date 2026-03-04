using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Events;

public sealed record InquiryLoggedAsComplianceSignalEvent(
    Guid SessionId,
    Guid DealId,
    int QuestionCount,
    int AnswerCount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
