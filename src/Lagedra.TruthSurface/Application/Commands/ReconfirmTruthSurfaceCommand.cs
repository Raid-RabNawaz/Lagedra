using Lagedra.SharedKernel.Results;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Commands;

public sealed record ReconfirmTruthSurfaceCommand(
    Guid OriginalSnapshotId,
    string NewJurisdictionPackVersion,
    string UpdatedCanonicalContent,
    string Reason) : IRequest<Result<TruthSurfaceDto>>;

public sealed class ReconfirmTruthSurfaceCommandHandler(
    TruthSurfaceDbContext dbContext)
    : IRequestHandler<ReconfirmTruthSurfaceCommand, Result<TruthSurfaceDto>>
{
    public async Task<Result<TruthSurfaceDto>> Handle(ReconfirmTruthSurfaceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var original = await dbContext.Snapshots
            .FirstOrDefaultAsync(s => s.Id == request.OriginalSnapshotId, cancellationToken)
            .ConfigureAwait(false);

        if (original is null)
        {
            return Result<TruthSurfaceDto>.Failure(new Error("TruthSurface.NotFound", "Original snapshot not found."));
        }

        if (original.Status != TruthSurfaceStatus.Confirmed)
        {
            return Result<TruthSurfaceDto>.Failure(new Error("TruthSurface.NotConfirmed", "Only a confirmed snapshot can be superseded."));
        }

        var superseding = TruthSnapshot.CreateDraft(
            original.DealId,
            original.ProtocolVersion,
            request.NewJurisdictionPackVersion,
            request.UpdatedCanonicalContent);

        superseding.SubmitForConfirmation();

        original.Supersede(superseding.Id, request.Reason);

        dbContext.Snapshots.Add(superseding);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TruthSurfaceDto>.Success(new TruthSurfaceDto(
            superseding.Id, superseding.DealId, superseding.Status,
            superseding.ProtocolVersion, superseding.JurisdictionPackVersion,
            superseding.InquiryClosed, superseding.LandlordConfirmed,
            superseding.TenantConfirmed, superseding.CreatedAt, superseding.SealedAt, null));
    }
}
