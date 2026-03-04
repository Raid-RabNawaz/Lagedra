using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Entities;

public sealed class InquiryAnswer : Entity<Guid>
{
    public Guid QuestionId { get; private set; }
    public ResponseType ResponseType { get; private set; }
    public string AnswerValue { get; private set; } = string.Empty;
    public DateTime AnsweredAt { get; private set; }

    private InquiryAnswer() { }

    internal static InquiryAnswer Create(
        Guid questionId,
        ResponseType responseType,
        string answerValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(answerValue);

        return new InquiryAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            ResponseType = responseType,
            AnswerValue = answerValue,
            AnsweredAt = DateTime.UtcNow
        };
    }
}
