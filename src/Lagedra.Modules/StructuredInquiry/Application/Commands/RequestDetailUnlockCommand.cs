using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record RequestDetailUnlockCommand(
    Guid DealId,
    Guid CallerUserId) : IRequest<Result<InquiryDto>>;

public sealed class RequestDetailUnlockCommandHandler(
    InquiryDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider,
    IFeatureFlags featureFlags)
    : IRequestHandler<RequestDetailUnlockCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        RequestDetailUnlockCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var participants = await dealStatusProvider
            .GetParticipantsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (participants is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.DealNotFound", "Deal not found."));
        }

        if (participants.TenantUserId != request.CallerUserId)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the deal's tenant can request a detail unlock."));
        }

        // Idempotent: if an inquiry session already exists for this deal,
        // return it instead of erroring or duplicating. This is the no-op
        // path the V2 booking flow relies on — sessions default to Open so
        // the tenant can ask questions without an unlock dance.
        var existing = await dbContext.Sessions
            .Where(s => s.DealId == request.DealId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result<InquiryDto>.Success(MapToDto(existing));
        }

        var initialStatus = featureFlags.BookingFlowV2Enabled
            ? InquirySessionStatus.Open
            : InquirySessionStatus.Locked;

        // Phase 17 — sessions are now keyed on (listingId, tenantUserId) too,
        // so look up the deal's listing and create the session with the full
        // identity. The legacy unlock-request path only ever runs for an
        // already-approved deal, so the listing/tenant lookup is cheap.
        var session = Domain.Aggregates.InquirySession.Create(
            request.DealId,
            participants.ListingId,
            participants.TenantUserId,
            initialStatus);

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryDto>.Success(MapToDto(session));
    }

    private static InquiryDto MapToDto(Domain.Aggregates.InquirySession s) =>
        new(s.Id, s.DealId, s.ListingId, s.TenantUserId, s.Status,
            s.UnlockedByLandlordAt, s.ClosedAt, s.CreatedAt, []);
}
