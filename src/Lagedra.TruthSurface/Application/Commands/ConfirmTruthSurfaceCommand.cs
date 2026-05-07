using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Crypto;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Commands;

public enum ConfirmingParty { Landlord, Tenant }

public sealed record ConfirmTruthSurfaceCommand(
    Guid SnapshotId,
    ConfirmingParty Party) : IRequest<Result<TruthSurfaceDto>>;

public sealed class ConfirmTruthSurfaceCommandHandler(
    TruthSurfaceDbContext dbContext,
    ICryptographicSigner signer,
    IClock clock)
    : IRequestHandler<ConfirmTruthSurfaceCommand, Result<TruthSurfaceDto>>
{
    public async Task<Result<TruthSurfaceDto>> Handle(ConfirmTruthSurfaceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await dbContext.Snapshots
            .FirstOrDefaultAsync(s => s.Id == request.SnapshotId, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result<TruthSurfaceDto>.Failure(new Error("TruthSurface.NotFound", "Snapshot not found."));
        }

        try
        {
            switch (request.Party)
            {
                case ConfirmingParty.Landlord:
                    snapshot.ConfirmByLandlord();
                    break;
                case ConfirmingParty.Tenant:
                    snapshot.ConfirmByTenant();
                    break;
                default:
                    return Result<TruthSurfaceDto>.Failure(new Error("TruthSurface.InvalidParty", "Unknown confirming party."));
            }

            if (snapshot.LandlordConfirmed && snapshot.TenantConfirmed)
            {
                if (string.IsNullOrWhiteSpace(snapshot.CanonicalContent))
                {
                    return Result<TruthSurfaceDto>.Failure(
                        new Error("TruthSurface.NoContent", "Cannot seal: snapshot has no canonical content."));
                }

                var hash = CanonicalHasher.ComputeHash(snapshot.CanonicalContent);
                var signature = signer.Sign(System.Text.Encoding.UTF8.GetBytes(hash));
                snapshot.Seal(hash, signature, clock.UtcNow);

                // The CryptographicProof constructor sets Id = Guid.NewGuid(), but
                // EF Core's ValueGeneratedOnAdd treats non-default keys as existing
                // entities (Modified), generating an UPDATE instead of INSERT.
                // Explicitly mark the new proof as Added so EF Core inserts it.
                if (snapshot.Proof is not null)
                {
                    dbContext.Entry(snapshot.Proof).State = EntityState.Added;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<TruthSurfaceDto>.Success(SnapshotMapper.Map(snapshot, signer));
        }
        catch (InvalidOperationException ex)
        {
            return Result<TruthSurfaceDto>.Failure(new Error("TruthSurface.InvalidOperation", ex.Message));
        }
    }
}
