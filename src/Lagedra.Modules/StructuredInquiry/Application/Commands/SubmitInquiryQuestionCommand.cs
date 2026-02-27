using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record SubmitInquiryQuestionCommand(
    Guid DealId,
    InquiryCategory Category,
    Guid PredefinedQuestionId) : IRequest<Result<InquiryQuestionDto>>;

public sealed class SubmitInquiryQuestionCommandHandler(
    InquiryDbContext dbContext)
    : IRequestHandler<SubmitInquiryQuestionCommand, Result<InquiryQuestionDto>>
{
    public async Task<Result<InquiryQuestionDto>> Handle(
        SubmitInquiryQuestionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.DealId == request.DealId
                && s.Status == InquirySessionStatus.Open, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.NotFound", "No open inquiry session found for this deal."));
        }

        var question = session.AddQuestion(request.Category, request.PredefinedQuestionId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryQuestionDto>.Success(
            new InquiryQuestionDto(question.Id, question.PredefinedQuestionId,
                question.Category, question.SubmittedAt, null));
    }
}
