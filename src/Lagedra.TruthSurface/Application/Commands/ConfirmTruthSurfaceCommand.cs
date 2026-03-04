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
            .Include(s => s.Proof)
            .FirstOrDefaultAsync(s => s.Id == request.SnapshotId, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result<TruthSurfaceDto>.Failure(new Error("TruthSurface.NotFound", "Snapshot not found."));
        }

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

        // If both parties have confirmed, seal the snapshot
        if (snapshot.LandlordConfirmed && snapshot.TenantConfirmed)
        {
            var hash = CanonicalHasher.ComputeHash(snapshot.CanonicalContent ?? string.Empty);
            var signature = signer.Sign(System.Text.Encoding.UTF8.GetBytes(hash));
            snapshot.Seal(hash, signature, clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
