using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Entities;

public sealed class InquiryQuestion : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public InquiryCategory Category { get; private set; }
    public Guid PredefinedQuestionId { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    public InquiryAnswer? Answer { get; private set; }

    private InquiryQuestion() { }

    internal static InquiryQuestion Create(
        Guid sessionId,
        InquiryCategory category,
        Guid predefinedQuestionId)
    {
        return new InquiryQuestion
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Category = category,
            PredefinedQuestionId = predefinedQuestionId,
            SubmittedAt = DateTime.UtcNow
        };
    }

    internal void SetAnswer(InquiryAnswer answer)
    {
        if (Answer is not null)
        {
            throw new InvalidOperationException("Question has already been answered.");
        }

        Answer = answer;
    }
}
