using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record ApproveInquiryUnlockCommand(
    Guid DealId,
    Guid CallerUserId) : IRequest<Result<InquiryDto>>;

public sealed class ApproveInquiryUnlockCommandHandler(
    InquiryDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<ApproveInquiryUnlockCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        ApproveInquiryUnlockCommand request,
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
                    "Only the deal's host can approve a detail unlock."));
        }

        var session = await dbContext.Sessions
            .Include(s => s.Offers)
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .FirstOrDefaultAsync(s => s.DealId == request.DealId
                && s.Status == Domain.Enums.InquirySessionStatus.Locked, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "No locked inquiry session found for this deal."));
        }

        session.Unlock();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryDto>.Success(InquiryDtoMapper.ToDto(session));
    }
}
