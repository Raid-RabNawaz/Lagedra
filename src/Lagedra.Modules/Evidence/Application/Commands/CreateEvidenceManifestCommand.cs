using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.Modules.Evidence.Domain.Aggregates;
using Lagedra.Modules.Evidence.Domain.Enums;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record CreateEvidenceManifestCommand(
    Guid DealId,
    ManifestType ManifestType) : IRequest<Result<ManifestDto>>;

public sealed class CreateEvidenceManifestCommandHandler(
    EvidenceDbContext dbContext)
    : IRequestHandler<CreateEvidenceManifestCommand, Result<ManifestDto>>
{
    public async Task<Result<ManifestDto>> Handle(
        CreateEvidenceManifestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var manifest = EvidenceManifest.Create(request.DealId, request.ManifestType);

        dbContext.Manifests.Add(manifest);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ManifestDto>.Success(MapToDto(manifest));
    }

    private static ManifestDto MapToDto(EvidenceManifest m) =>
        new(m.Id, m.DealId, m.ManifestType, m.Status,
            m.CreatedAt, m.SealedAt, m.HashOfAllFiles, []);
}
