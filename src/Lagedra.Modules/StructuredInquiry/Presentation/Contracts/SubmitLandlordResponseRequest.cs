using Lagedra.Modules.StructuredInquiry.Domain.Enums;

namespace Lagedra.Modules.StructuredInquiry.Presentation.Contracts;

public sealed record SubmitLandlordResponseRequest(
    Guid QuestionId,
    ResponseType ResponseType,
    string AnswerValue);
