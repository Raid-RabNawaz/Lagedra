using Lagedra.Modules.Evidence.Domain.Enums;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Queries;

public sealed record EvidenceScanQueueItemDto(
    Guid UploadId,
    Guid ManifestId,
    Guid DealId,
    string OriginalFileName,
    string MimeType,
    DateTime UploadedAt,
    string ScanStatus,
    DateTime? ScannedAt);

public sealed record GetEvidenceScanQueueQuery : IRequest<Result<IReadOnlyList<EvidenceScanQueueItemDto>>>;

public sealed class GetEvidenceScanQueueQueryHandler(EvidenceDbContext dbContext)
    : IRequestHandler<GetEvidenceScanQueueQuery, Result<IReadOnlyList<EvidenceScanQueueItemDto>>>
{
    public async Task<Result<IReadOnlyList<EvidenceScanQueueItemDto>>> Handle(
        GetEvidenceScanQueueQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = await dbContext.ScanResults
            .AsNoTracking()
            .Where(s => s.Status != ScanStatus.Clean)
            .Join(
                dbContext.Uploads.AsNoTracking(),
                s => s.UploadId,
                u => u.Id,
                (s, u) => new { Scan = s, Upload = u })
            .Join(
                dbContext.Manifests.AsNoTracking(),
                su => su.Upload.ManifestId,
                m => m.Id,
                (su, m) => new EvidenceScanQueueItemDto(
                    su.Upload.Id,
                    su.Upload.ManifestId,
                    m.DealId,
                    su.Upload.OriginalFileName,
                    su.Upload.MimeType,
                    su.Upload.UploadedAt,
                    su.Scan.Status.ToString(),
                    su.Scan.ScannedAt))
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<EvidenceScanQueueItemDto>>.Success(items);
    }
}
