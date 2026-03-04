using Lagedra.Modules.StructuredInquiry.Domain.Enums;

namespace Lagedra.Modules.StructuredInquiry.Application.DTOs;

public sealed record InquiryDto(
    Guid SessionId,
    Guid DealId,
    InquirySessionStatus Status,
    DateTime? UnlockedByLandlordAt,
    DateTime? ClosedAt,
    DateTime CreatedAt,
    IReadOnlyList<InquiryQuestionDto> Questions);

public sealed record InquiryQuestionDto(
    Guid QuestionId,
    Guid? PredefinedQuestionId,
    InquiryCategory Category,
    DateTime SubmittedAt,
    InquiryAnswerDto? Answer,
    string? CustomText = null);

public sealed record InquiryAnswerDto(
    Guid AnswerId,
    ResponseType ResponseType,
    string AnswerValue,
    DateTime AnsweredAt);
