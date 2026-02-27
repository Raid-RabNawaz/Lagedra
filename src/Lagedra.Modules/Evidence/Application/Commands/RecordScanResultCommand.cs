using Lagedra.Modules.Evidence.Domain.Enums;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record RecordScanResultCommand(
    Guid UploadId,
    ScanStatus Status) : IRequest<Result>;

public sealed class RecordScanResultCommandHandler(
    EvidenceDbContext dbContext)
    : IRequestHandler<RecordScanResultCommand, Result>
{
    public async Task<Result> Handle(
        RecordScanResultCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scanResult = await dbContext.ScanResults
            .FirstOrDefaultAsync(s => s.UploadId == request.UploadId, cancellationToken)
            .ConfigureAwait(false);

        if (scanResult is null)
        {
            return Result.Failure(
                new Error("Evidence.ScanNotFound", "Scan result not found for this upload."));
        }

        var now = DateTime.UtcNow;

        if (request.Status == ScanStatus.Clean)
        {
            scanResult.MarkClean(now);
        }
        else
        {
            scanResult.MarkInfected(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
