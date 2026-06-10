using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Services;

public sealed class EvidenceViewAccessService(
    EvidenceDbContext dbContext,
    IDealApplicationStatusProvider dealProvider,
    IArbitrationEvidenceManifestAccessProvider arbitrationAccess)
{
    public static readonly Error Forbidden = new(
        "Evidence.Forbidden",
        "You do not have permission to view this evidence.");

    public async Task<Result> RequireManifestViewAsync(
        EvidenceCallerContext caller,
        Guid manifestId,
        CancellationToken cancellationToken)
    {
        if (caller.IsPlatformAdmin)
        {
            return Result.Success();
        }

        var manifest = await dbContext.Manifests
            .AsNoTracking()
            .Where(m => m.Id == manifestId)
            .Select(m => new { m.Id, m.DealId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (manifest is null)
        {
            return Result.Failure(new Error("Evidence.ManifestNotFound", "Manifest not found."));
        }

        if (await IsDealPartyAsync(caller.UserId, manifest.DealId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Success();
        }

        if (caller.IsArbitrator
            && await arbitrationAccess
                .IsAssignedArbitratorForManifestAsync(caller.UserId, manifestId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Success();
        }

        return Result.Failure(Forbidden);
    }

    public async Task<Result<Guid>> RequireUploadViewAsync(
        EvidenceCallerContext caller,
        Guid uploadId,
        CancellationToken cancellationToken)
    {
        var manifestId = await dbContext.Uploads
            .AsNoTracking()
            .Where(u => u.Id == uploadId)
            .Select(u => (Guid?)u.ManifestId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (manifestId is null)
        {
            return Result<Guid>.Failure(new Error("Evidence.UploadNotFound", "Upload not found."));
        }

        var access = await RequireManifestViewAsync(caller, manifestId.Value, cancellationToken)
            .ConfigureAwait(false);

        return access.IsSuccess
            ? Result<Guid>.Success(manifestId.Value)
            : Result<Guid>.Failure(access.Error);
    }

    private async Task<bool> IsDealPartyAsync(
        Guid userId,
        Guid dealId,
        CancellationToken cancellationToken)
    {
        var participants = await dealProvider
            .GetParticipantsAsync(dealId, cancellationToken)
            .ConfigureAwait(false);

        return participants is not null
            && (participants.LandlordUserId == userId || participants.TenantUserId == userId);
    }
}
