using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record SubmitLandlordResponseCommand(
    Guid DealId,
    Guid QuestionId,
    ResponseType ResponseType,
    string AnswerValue) : IRequest<Result<InquiryAnswerDto>>;

public sealed class SubmitLandlordResponseCommandHandler(
    InquiryDbContext dbContext)
    : IRequestHandler<SubmitLandlordResponseCommand, Result<InquiryAnswerDto>>
{
    public async Task<Result<InquiryAnswerDto>> Handle(
        SubmitLandlordResponseCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .FirstOrDefaultAsync(s => s.DealId == request.DealId
                && s.Status == InquirySessionStatus.Open, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryAnswerDto>.Failure(
                new Error("Inquiry.NotFound", "No open inquiry session found for this deal."));
        }

        var answer = session.AddAnswer(request.QuestionId, request.ResponseType, request.AnswerValue);

        dbContext.Answers.Add(answer);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryAnswerDto>.Success(
            new InquiryAnswerDto(answer.Id, answer.ResponseType, answer.AnswerValue, answer.AnsweredAt));
    }
}
