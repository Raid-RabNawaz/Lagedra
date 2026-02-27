using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record RequestDetailUnlockCommand(Guid DealId) : IRequest<Result<InquiryDto>>;

public sealed class RequestDetailUnlockCommandHandler(
    InquiryDbContext dbContext)
    : IRequestHandler<RequestDetailUnlockCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        RequestDetailUnlockCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = Domain.Aggregates.InquirySession.Create(request.DealId);

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryDto>.Success(MapToDto(session));
    }

    private static InquiryDto MapToDto(Domain.Aggregates.InquirySession s) =>
        new(s.Id, s.DealId, s.Status, s.UnlockedByLandlordAt, s.ClosedAt, s.CreatedAt, []);
}
