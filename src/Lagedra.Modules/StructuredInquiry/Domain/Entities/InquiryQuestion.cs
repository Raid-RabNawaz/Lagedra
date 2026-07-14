using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Entities;

public sealed class InquiryQuestion : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public InquiryCategory Category { get; private set; }
    public Guid? PredefinedQuestionId { get; private set; }

    /// <summary>
    /// Legacy short customization string (≤500 chars). Pre-Phase 17 this was
    /// the only way to attach tenant-typed text to a question. Kept for
    /// backward compatibility; new submissions should use
    /// <see cref="OpenQuestionText"/>.
    /// </summary>
    public string? CustomText { get; private set; }

    /// <summary>
    /// Phase 17 — free-form question text. Set when the tenant picks a
    /// category, chooses "Other", and types their own question. Goes hand
    /// in hand with <see cref="ResponseType.OpenText"/> on the answer side.
    /// </summary>
    public string? OpenQuestionText { get; private set; }

    /// <summary>
    /// Who asked. Null on legacy rows — treat as the session tenant.
    /// </summary>
    public Guid? SubmittedByUserId { get; private set; }

    /// <summary>
    /// Tenant or Partner. Null on legacy rows — treat as Tenant.
    /// </summary>
    public InquiryQuestionAuthorRole? SubmittedByRole { get; private set; }

    public DateTime SubmittedAt { get; private set; }

    public InquiryAnswer? Answer { get; private set; }

    private InquiryQuestion() { }

    internal static InquiryQuestion Create(
        Guid sessionId,
        InquiryCategory category,
        Guid? predefinedQuestionId,
        string? customText = null,
        string? openQuestionText = null,
        Guid? submittedByUserId = null,
        InquiryQuestionAuthorRole? submittedByRole = null)
    {
        if (predefinedQuestionId is null
            && string.IsNullOrWhiteSpace(customText)
            && string.IsNullOrWhiteSpace(openQuestionText))
        {
            throw new ArgumentException(
                "Either a predefined question, custom text, or open question text must be provided.");
        }

        return new InquiryQuestion
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Category = category,
            PredefinedQuestionId = predefinedQuestionId,
            CustomText = customText?.Length > 500 ? customText[..500] : customText,
            OpenQuestionText = openQuestionText?.Length > 1000 ? openQuestionText[..1000] : openQuestionText,
            SubmittedByUserId = submittedByUserId,
            SubmittedByRole = submittedByRole ?? InquiryQuestionAuthorRole.Tenant,
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
