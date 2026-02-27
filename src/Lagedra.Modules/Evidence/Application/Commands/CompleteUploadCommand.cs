using Lagedra.Modules.Evidence.Domain.Entities;
using Lagedra.Modules.Evidence.Domain.ValueObjects;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record CompleteUploadCommand(
    Guid ManifestId,
    Guid UploadId,
    string OriginalFileName,
    string StorageKey,
    string MimeType,
    string FileHashHex) : IRequest<Result>;

public sealed class CompleteUploadCommandHandler(
    EvidenceDbContext dbContext)
    : IRequestHandler<CompleteUploadCommand, Result>
{
    public async Task<Result> Handle(
        CompleteUploadCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var manifest = await dbContext.Manifests
            .Include(m => m.Uploads)
            .FirstOrDefaultAsync(m => m.Id == request.ManifestId, cancellationToken)
            .ConfigureAwait(false);

        if (manifest is null)
        {
            return Result.Failure(
                new Error("Evidence.ManifestNotFound", "Manifest not found."));
        }

        var upload = manifest.AddUpload(
            request.OriginalFileName, request.StorageKey, request.MimeType);

        upload.SetFileHash(FileHash.Create(request.FileHashHex));

        var scanResult = MalwareScanResult.CreatePending(upload.Id);
        dbContext.ScanResults.Add(scanResult);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
