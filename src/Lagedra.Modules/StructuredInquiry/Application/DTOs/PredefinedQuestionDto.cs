using Lagedra.Modules.StructuredInquiry.Domain.Enums;

namespace Lagedra.Modules.StructuredInquiry.Application.DTOs;

public sealed record PredefinedQuestionDto(
    Guid Id,
    InquiryCategory Category,
    string Text,
    ResponseType ExpectedResponseType);
