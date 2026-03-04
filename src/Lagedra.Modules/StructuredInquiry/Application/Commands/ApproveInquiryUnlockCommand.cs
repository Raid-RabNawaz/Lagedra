using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record ApproveInquiryUnlockCommand(Guid DealId) : IRequest<Result<InquiryDto>>;

public sealed class ApproveInquiryUnlockCommandHandler(
    InquiryDbContext dbContext)
    : IRequestHandler<ApproveInquiryUnlockCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        ApproveInquiryUnlockCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
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

        return Result<InquiryDto>.Success(MapToDto(session));
    }

    private static InquiryDto MapToDto(Domain.Aggregates.InquirySession s) =>
        new(s.Id, s.DealId, s.Status, s.UnlockedByLandlordAt, s.ClosedAt, s.CreatedAt, []);
}
