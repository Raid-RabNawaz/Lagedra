using Lagedra.SharedKernel.Results;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Queries;

public sealed record GetSnapshotQuery(Guid SnapshotId) : IRequest<Result<TruthSurfaceDto>>;

public sealed class GetSnapshotQueryHandler(TruthSurfaceDbContext dbContext)
    : IRequestHandler<GetSnapshotQuery, Result<TruthSurfaceDto>>
{
    public async Task<Result<TruthSurfaceDto>> Handle(GetSnapshotQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await dbContext.Snapshots
            .AsNoTracking()
            .Include(s => s.Proof)
            .FirstOrDefaultAsync(s => s.Id == request.SnapshotId, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result<TruthSurfaceDto>.Failure(new Error("TruthSurface.NotFound", "Snapshot not found."));
        }

        return Result<TruthSurfaceDto>.Success(MapToDto(snapshot));
    }

    private static TruthSurfaceDto MapToDto(TruthSnapshot s) =>
        new(s.Id, s.DealId, s.Status, s.ProtocolVersion,
            s.JurisdictionPackVersion, s.InquiryClosed,
            s.LandlordConfirmed, s.TenantConfirmed,
            s.CreatedAt, s.SealedAt,
            s.Proof is not null
                ? new SnapshotProofDto(s.Proof.Id, s.Proof.Hash, s.Proof.Signature, s.Proof.SignedAt, true)
                : null);
}
