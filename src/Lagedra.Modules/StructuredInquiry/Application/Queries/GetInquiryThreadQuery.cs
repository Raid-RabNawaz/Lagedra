using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

public sealed record GetInquiryThreadQuery(
    Guid DealId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<InquiryDto>>;

public sealed class GetInquiryThreadQueryHandler(
    InquiryDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<GetInquiryThreadQuery, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        GetInquiryThreadQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsAdmin)
        {
            var participants = await dealStatusProvider
                .GetParticipantsAsync(request.DealId, cancellationToken)
                .ConfigureAwait(false);

            if (participants is null)
            {
                return Result<InquiryDto>.Failure(
                    new Error("Inquiry.NotFound", "No inquiry session found for this deal."));
            }

            if (participants.TenantUserId != request.CallerUserId
                && participants.LandlordUserId != request.CallerUserId)
            {
                return Result<InquiryDto>.Failure(
                    new Error("Inquiry.Forbidden",
                        "You do not have access to this deal's inquiry thread."));
            }
        }

        var session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(s => s.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "No inquiry session found for this deal."));
        }

        return Result<InquiryDto>.Success(MapToDto(session));
    }

    private static InquiryDto MapToDto(InquirySession s) =>
        new(s.Id, s.DealId, s.ListingId, s.TenantUserId, s.Status,
            s.UnlockedByLandlordAt, s.ClosedAt, s.CreatedAt,
            s.Questions.Select(q => new InquiryQuestionDto(
                q.Id,
                q.PredefinedQuestionId,
                q.Category,
                q.SubmittedAt,
                q.Answer is not null
                    ? new InquiryAnswerDto(q.Answer.Id, q.Answer.ResponseType,
                        q.Answer.AnswerValue, q.Answer.AnsweredAt)
                    : null,
                q.CustomText,
                q.OpenQuestionText))
            .ToList());
}
