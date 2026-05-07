using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Crypto;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Queries;

public sealed record GetSnapshotByDealIdQuery(Guid DealId) : IRequest<Result<TruthSurfaceDto>>;

public sealed class GetSnapshotByDealIdQueryHandler(
    TruthSurfaceDbContext dbContext,
    ICryptographicSigner signer)
    : IRequestHandler<GetSnapshotByDealIdQuery, Result<TruthSurfaceDto>>
{
    public async Task<Result<TruthSurfaceDto>> Handle(
        GetSnapshotByDealIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await dbContext.Snapshots
            .AsNoTracking()
            .Include(s => s.Proof)
            .Where(s => s.DealId == request.DealId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result<TruthSurfaceDto>.Failure(
                new Error("TruthSurface.NotFound", "No snapshot found for this deal."));
        }

        return Result<TruthSurfaceDto>.Success(SnapshotMapper.Map(snapshot, signer));
    }
}
