using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.Modules.Evidence.Domain.Aggregates;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record SealEvidenceManifestCommand(Guid ManifestId) : IRequest<Result<ManifestDto>>;

public sealed class SealEvidenceManifestCommandHandler(
    EvidenceDbContext dbContext)
    : IRequestHandler<SealEvidenceManifestCommand, Result<ManifestDto>>
{
    public async Task<Result<ManifestDto>> Handle(
        SealEvidenceManifestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var manifest = await dbContext.Manifests
            .Include(m => m.Uploads)
            .FirstOrDefaultAsync(m => m.Id == request.ManifestId, cancellationToken)
            .ConfigureAwait(false);

        if (manifest is null)
        {
            return Result<ManifestDto>.Failure(
                new Error("Evidence.ManifestNotFound", "Manifest not found."));
        }

        manifest.Seal();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ManifestDto>.Success(MapToDto(manifest));
    }

    private static ManifestDto MapToDto(EvidenceManifest m) =>
        new(m.Id, m.DealId, m.ManifestType, m.Status,
            m.CreatedAt, m.SealedAt, m.HashOfAllFiles,
            m.Uploads.Select(u => new ManifestUploadDto(
                u.Id, u.OriginalFileName, u.MimeType,
                u.FileHash?.Value, u.UploadedAt)).ToList());
}
