using Lagedra.SharedKernel.Results;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Application.Services;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;

namespace Lagedra.TruthSurface.Application.Commands;

public sealed record CreateTruthSurfaceForDealCommand(
    Guid DealId,
    Guid RequestedByUserId) : IRequest<Result<TruthSurfaceDto>>;

public sealed class CreateTruthSurfaceForDealCommandHandler(
    TruthSurfaceDbContext dbContext,
    ITruthSurfaceSnapshotBuilder snapshotBuilder)
    : IRequestHandler<CreateTruthSurfaceForDealCommand, Result<TruthSurfaceDto>>
{
    public async Task<Result<TruthSurfaceDto>> Handle(
        CreateTruthSurfaceForDealCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var buildResult = await snapshotBuilder
            .BuildDraftAsync(request.DealId, request.RequestedByUserId, consent: null, cancellationToken)
            .ConfigureAwait(false);

        if (!buildResult.IsSuccess)
        {
            return Result<TruthSurfaceDto>.Failure(buildResult.Error);
        }

        var snapshot = buildResult.Value;
        dbContext.Snapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TruthSurfaceDto>.Success(new TruthSurfaceDto(
            snapshot.Id, snapshot.DealId, snapshot.Status,
            snapshot.ProtocolVersion, snapshot.JurisdictionPackVersion,
            snapshot.CanonicalContent,
            snapshot.InquiryClosed, snapshot.LandlordConfirmed, snapshot.TenantConfirmed,
            snapshot.CreatedAt, snapshot.SealedAt, null,
            snapshot.IsLocked, snapshot.LockedAt));
    }
}
