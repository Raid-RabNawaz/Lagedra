using Lagedra.Modules.StructuredInquiry.Domain.Entities;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Aggregates;

public sealed class InquirySession : AggregateRoot<Guid>
{
    private readonly List<InquiryQuestion> _questions = [];

    public Guid DealId { get; private set; }
    public InquirySessionStatus Status { get; private set; }
    public DateTime? UnlockedByLandlordAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyList<InquiryQuestion> Questions => _questions.AsReadOnly();

    private InquirySession() { }

    public static InquirySession Create(Guid dealId)
    {
        return new InquirySession
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Status = InquirySessionStatus.Locked
        };
    }

    public void Unlock()
    {
        if (Status != InquirySessionStatus.Locked)
        {
            throw new InvalidOperationException($"Cannot unlock session in status '{Status}'.");
        }

        Status = InquirySessionStatus.Open;
        UnlockedByLandlordAt = DateTime.UtcNow;
    }

    public InquiryQuestion AddQuestion(InquiryCategory category, Guid? predefinedQuestionId, string? customText = null)
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot add questions to session in status '{Status}'.");
        }

        var question = InquiryQuestion.Create(Id, category, predefinedQuestionId, customText);
        _questions.Add(question);
        return question;
    }

    public InquiryAnswer AddAnswer(Guid questionId, ResponseType responseType, string answerValue)
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot add answers to session in status '{Status}'.");
        }

        var question = _questions.FirstOrDefault(q => q.Id == questionId)
            ?? throw new InvalidOperationException($"Question '{questionId}' not found in this session.");

        var answer = InquiryAnswer.Create(questionId, responseType, answerValue);
        question.SetAnswer(answer);
        return answer;
    }

    public void Close()
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot close session in status '{Status}'.");
        }

        Status = InquirySessionStatus.Closed;
        ClosedAt = DateTime.UtcNow;

        AddDomainEvent(new InquiryClosedEvent(Id, DealId, ClosedAt.Value));
    }
}
