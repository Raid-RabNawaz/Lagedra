using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

/// <summary>
/// Phase 17 — host posts a response to a question on an inquiry session
/// addressed by session id. Authorizes the host either as the deal's
/// landlord (deal-linked sessions) or the listing's landlord (pre-booking
/// sessions).
/// </summary>
public sealed record SubmitResponseToSessionCommand(
    Guid SessionId,
    Guid CallerUserId,
    Guid QuestionId,
    ResponseType ResponseType,
    string AnswerValue) : IRequest<Result<InquiryAnswerDto>>;

public sealed class SubmitResponseToSessionCommandHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<SubmitResponseToSessionCommand, Result<InquiryAnswerDto>>
{
    public async Task<Result<InquiryAnswerDto>> Handle(
        SubmitResponseToSessionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryAnswerDto>.Failure(
                new Error("Inquiry.NotFound", "Inquiry thread not found."));
        }

        if (session.Status != InquirySessionStatus.Open)
        {
            return Result<InquiryAnswerDto>.Failure(
                new Error("Inquiry.NotOpen",
                    $"Cannot post a response to a session in status '{session.Status}'."));
        }

        var landlordId = await ResolveLandlordIdAsync(session.DealId, session.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (landlordId is null || landlordId != request.CallerUserId)
        {
            return Result<InquiryAnswerDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the listing's host can post responses to this thread."));
        }

        var answer = session.AddAnswer(request.QuestionId, request.ResponseType, request.AnswerValue);
        dbContext.Answers.Add(answer);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryAnswerDto>.Success(
            new InquiryAnswerDto(answer.Id, answer.ResponseType, answer.AnswerValue, answer.AnsweredAt));
    }

    private async Task<Guid?> ResolveLandlordIdAsync(
        Guid? dealId,
        Guid listingId,
        CancellationToken ct)
    {
        if (dealId is { } id)
        {
            var participants = await dealStatusProvider
                .GetParticipantsAsync(id, ct)
                .ConfigureAwait(false);

            if (participants is not null)
            {
                return participants.LandlordUserId;
            }
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(listingId, ct)
            .ConfigureAwait(false);

        return listing?.LandlordUserId;
    }
}
