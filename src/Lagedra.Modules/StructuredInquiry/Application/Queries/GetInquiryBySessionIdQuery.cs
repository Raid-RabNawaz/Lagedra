using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

/// <summary>
/// Phase 17 — fetch a single inquiry thread by its session id, regardless
/// of whether it's currently listing-scoped or deal-scoped. Used by the
/// generic inquiry thread page that handles both pre-booking and post-
/// application views.
/// </summary>
/// <remarks>
/// Authorization rules:
/// <list type="bullet">
///   <item>Tenant who owns the thread → always allowed.</item>
///   <item>Host of the listing for a pre-booking thread → allowed via
///         <see cref="IListingProvider"/> landlord lookup.</item>
///   <item>Host of the linked deal → allowed via <see cref="IDealApplicationStatusProvider"/>.</item>
///   <item>Platform admin → allowed.</item>
/// </list>
/// </remarks>
public sealed record GetInquiryBySessionIdQuery(
    Guid SessionId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<InquiryDto>>;

public sealed class GetInquiryBySessionIdQueryHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<GetInquiryBySessionIdQuery, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        GetInquiryBySessionIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "Inquiry thread not found."));
        }

        if (!request.IsAdmin && !await IsAuthorizedAsync(session, request.CallerUserId, cancellationToken)
            .ConfigureAwait(false))
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.Forbidden", "You do not have access to this inquiry thread."));
        }

        return Result<InquiryDto>.Success(MapToDto(session));
    }

    private async Task<bool> IsAuthorizedAsync(
        InquirySession session,
        Guid callerUserId,
        CancellationToken ct)
    {
        if (session.TenantUserId == callerUserId)
        {
            return true;
        }

        // For deal-linked threads, prefer the deal participants resolver
        // because it covers both host and tenant in a single round trip.
        if (session.DealId is { } dealId)
        {
            var participants = await dealStatusProvider
                .GetParticipantsAsync(dealId, ct)
                .ConfigureAwait(false);

            if (participants is not null
                && (participants.LandlordUserId == callerUserId
                    || participants.TenantUserId == callerUserId))
            {
                return true;
            }
        }

        // Pre-booking threads have no deal, so fall back to the listing's
        // landlord. This is also the path host-side polling uses to surface
        // open inquiries on their own listings before the tenant applies.
        var listing = await listingProvider
            .GetListingDetailsAsync(session.ListingId, ct)
            .ConfigureAwait(false);

        return listing is not null && listing.LandlordUserId == callerUserId;
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
