using Lagedra.SharedKernel.Results;
using MediatR;
using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

public sealed record ListPredefinedQuestionsQuery(
    InquiryCategory? Category) : IRequest<Result<IReadOnlyList<PredefinedQuestionDto>>>;

public sealed class ListPredefinedQuestionsQueryHandler
    : IRequestHandler<ListPredefinedQuestionsQuery, Result<IReadOnlyList<PredefinedQuestionDto>>>
{
    private static readonly IReadOnlyList<PredefinedQuestionDto> s_questions =
    [
        new(Guid.Parse("a1000000-0000-0000-0000-000000000001"), InquiryCategory.UtilitySpecifics, "What utilities are included in rent?", ResponseType.MultipleChoice),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000002"), InquiryCategory.UtilitySpecifics, "What is the average monthly utility cost?", ResponseType.Numeric),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000003"), InquiryCategory.UtilitySpecifics, "Is there a separate meter for the unit?", ResponseType.YesNo),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000004"), InquiryCategory.AccessibilityLayout, "Is the property wheelchair accessible?", ResponseType.YesNo),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000005"), InquiryCategory.AccessibilityLayout, "Is there an elevator in the building?", ResponseType.YesNo),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000006"), InquiryCategory.AccessibilityLayout, "What floor is the unit on?", ResponseType.Numeric),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000007"), InquiryCategory.RuleClarification, "Are pets allowed?", ResponseType.YesNo),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000008"), InquiryCategory.RuleClarification, "Is subletting permitted?", ResponseType.YesNo),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000009"), InquiryCategory.RuleClarification, "Are there quiet hours?", ResponseType.YesNo),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000a"), InquiryCategory.Proximity, "How far is the nearest public transit stop?", ResponseType.Numeric),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000b"), InquiryCategory.Proximity, "Is there on-site parking available?", ResponseType.YesNo),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000c"), InquiryCategory.Proximity, "Are there grocery stores within walking distance?", ResponseType.YesNo),
    ];

    public Task<Result<IReadOnlyList<PredefinedQuestionDto>>> Handle(
        ListPredefinedQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<PredefinedQuestionDto> result = request.Category.HasValue
            ? s_questions.Where(q => q.Category == request.Category.Value).ToList()
            : s_questions;

        return Task.FromResult(Result<IReadOnlyList<PredefinedQuestionDto>>.Success(result));
    }
}
