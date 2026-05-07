using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record QuarantineUploadCommand(Guid UploadId) : IRequest<Result>;

public sealed class QuarantineUploadCommandHandler(EvidenceDbContext dbContext)
    : IRequestHandler<QuarantineUploadCommand, Result>
{
    public async Task<Result> Handle(QuarantineUploadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scanResult = await dbContext.ScanResults
            .FirstOrDefaultAsync(s => s.UploadId == request.UploadId, cancellationToken)
            .ConfigureAwait(false);

        if (scanResult is null)
            return Result.Failure(new Error("Evidence.ScanNotFound", "Scan result not found for this upload."));

        scanResult.MarkInfected(DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
