using Lagedra.SharedKernel.Results;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;

namespace Lagedra.TruthSurface.Application.Commands;

public sealed record CreateSnapshotCommand(
    Guid DealId,
    string ProtocolVersion,
    string JurisdictionPackVersion,
    string CanonicalContent) : IRequest<Result<TruthSurfaceDto>>;

public sealed class CreateSnapshotCommandHandler(
    TruthSurfaceDbContext dbContext)
    : IRequestHandler<CreateSnapshotCommand, Result<TruthSurfaceDto>>
{
    public async Task<Result<TruthSurfaceDto>> Handle(CreateSnapshotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = TruthSnapshot.CreateDraft(
            request.DealId,
            request.ProtocolVersion,
            request.JurisdictionPackVersion,
            request.CanonicalContent);

        snapshot.SubmitForConfirmation();

        dbContext.Snapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TruthSurfaceDto>.Success(MapToDto(snapshot));
    }

    private static TruthSurfaceDto MapToDto(TruthSnapshot s) =>
        new(s.Id, s.DealId, s.Status, s.ProtocolVersion,
            s.JurisdictionPackVersion, s.InquiryClosed,
            s.LandlordConfirmed, s.TenantConfirmed,
            s.CreatedAt, s.SealedAt, null);
}
