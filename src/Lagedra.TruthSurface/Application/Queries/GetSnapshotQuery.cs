using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Crypto;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Queries;

public sealed record GetSnapshotQuery(Guid SnapshotId) : IRequest<Result<TruthSurfaceDto>>;

public sealed class GetSnapshotQueryHandler(
    TruthSurfaceDbContext dbContext,
    ICryptographicSigner signer)
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

        return Result<TruthSurfaceDto>.Success(SnapshotMapper.Map(snapshot, signer));
    }
}
