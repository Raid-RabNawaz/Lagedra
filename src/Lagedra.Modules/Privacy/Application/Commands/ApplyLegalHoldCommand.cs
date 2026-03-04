using Lagedra.Modules.Privacy.Application.DTOs;
using Lagedra.Modules.Privacy.Domain.Entities;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Privacy.Application.Commands;

public sealed record ApplyLegalHoldCommand(
    Guid UserId,
    string Reason) : IRequest<Result<LegalHoldDto>>;

public sealed class ApplyLegalHoldCommandHandler(PrivacyDbContext dbContext)
    : IRequestHandler<ApplyLegalHoldCommand, Result<LegalHoldDto>>
{
    public async Task<Result<LegalHoldDto>> Handle(
        ApplyLegalHoldCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var legalHold = LegalHold.Apply(request.UserId, request.Reason);
        dbContext.LegalHolds.Add(legalHold);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<LegalHoldDto>.Success(new LegalHoldDto(
            legalHold.Id, legalHold.UserId, legalHold.Reason,
            legalHold.AppliedAt, legalHold.ReleasedAt));
    }
}
