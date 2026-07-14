using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

/// <summary>
/// Host-only opt-in to re-lock an inquiry thread that defaulted to Open
/// under the Phase 16 BookingFlow.V2 rollout. Once locked, the existing
/// <see cref="ApproveInquiryUnlockCommand"/> flow gates further questions
/// behind the host's explicit unlock.
/// </summary>
public sealed record LockInquirySessionCommand(
    Guid DealId,
    Guid CallerUserId) : IRequest<Result<InquiryDto>>;

public sealed class LockInquirySessionCommandHandler(
    InquiryDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<LockInquirySessionCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        LockInquirySessionCommand request,
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

        if (participants.LandlordUserId != request.CallerUserId)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the deal's host can lock an inquiry thread."));
        }

        var session = await dbContext.Sessions
            .Include(s => s.Offers)
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .FirstOrDefaultAsync(
                s => s.DealId == request.DealId
                    && s.Status == InquirySessionStatus.Open,
                cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "No open inquiry session found for this deal."));
        }

        session.Lock();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryDto>.Success(InquiryDtoMapper.ToDto(session));
    }
}
