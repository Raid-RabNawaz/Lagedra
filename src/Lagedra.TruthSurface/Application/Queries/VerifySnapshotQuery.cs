using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Infrastructure.Crypto;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Queries;

public sealed record VerifySnapshotQuery(Guid SnapshotId) : IRequest<Result<SnapshotProofDto>>;

public sealed class VerifySnapshotQueryHandler(
    TruthSurfaceDbContext dbContext,
    ICryptographicSigner signer)
    : IRequestHandler<VerifySnapshotQuery, Result<SnapshotProofDto>>
{
    public async Task<Result<SnapshotProofDto>> Handle(VerifySnapshotQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await dbContext.Snapshots
            .AsNoTracking()
            .Include(s => s.Proof)
            .FirstOrDefaultAsync(s => s.Id == request.SnapshotId, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result<SnapshotProofDto>.Failure(new Error("TruthSurface.NotFound", "Snapshot not found."));
        }

        if (snapshot.Proof is null || snapshot.CanonicalContent is null)
        {
            return Result<SnapshotProofDto>.Failure(new Error("TruthSurface.NotSealed", "Snapshot has not been sealed yet."));
        }

        var recomputedHash = CanonicalHasher.ComputeHash(snapshot.CanonicalContent);
        var hashMatches = string.Equals(recomputedHash, snapshot.Proof.Hash, StringComparison.Ordinal);

        var signatureValid = signer.Verify(
            System.Text.Encoding.UTF8.GetBytes(snapshot.Proof.Hash),
            snapshot.Proof.Signature);

        var isValid = hashMatches && signatureValid;

        return Result<SnapshotProofDto>.Success(
            new SnapshotProofDto(snapshot.Proof.Id, snapshot.Proof.Hash, snapshot.Proof.Signature, snapshot.Proof.SignedAt, isValid));
    }
}
