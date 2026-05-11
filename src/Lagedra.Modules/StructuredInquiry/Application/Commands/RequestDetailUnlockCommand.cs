using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record RequestDetailUnlockCommand(
    Guid DealId,
    Guid CallerUserId) : IRequest<Result<InquiryDto>>;

public sealed class RequestDetailUnlockCommandHandler(
    InquiryDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider)
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

        var session = Domain.Aggregates.InquirySession.Create(request.DealId);

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryDto>.Success(MapToDto(session));
    }

    private static InquiryDto MapToDto(Domain.Aggregates.InquirySession s) =>
        new(s.Id, s.DealId, s.Status, s.UnlockedByLandlordAt, s.ClosedAt, s.CreatedAt, []);
}
