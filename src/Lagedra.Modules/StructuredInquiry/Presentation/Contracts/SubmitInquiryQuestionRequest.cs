using Lagedra.Modules.StructuredInquiry.Domain.Enums;

namespace Lagedra.Modules.StructuredInquiry.Presentation.Contracts;

public sealed record SubmitInquiryQuestionRequest(
    InquiryCategory Category,
    Guid PredefinedQuestionId);
