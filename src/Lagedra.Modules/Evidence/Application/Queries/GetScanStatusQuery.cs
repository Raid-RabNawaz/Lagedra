using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Queries;

public sealed record GetScanStatusQuery(Guid UploadId) : IRequest<Result<ScanResultDto>>;

public sealed class GetScanStatusQueryHandler(EvidenceDbContext dbContext)
    : IRequestHandler<GetScanStatusQuery, Result<ScanResultDto>>
{
    public async Task<Result<ScanResultDto>> Handle(
        GetScanStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scan = await dbContext.ScanResults
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UploadId == request.UploadId, cancellationToken)
            .ConfigureAwait(false);

        if (scan is null)
        {
            return Result<ScanResultDto>.Failure(
                new Error("Evidence.ScanNotFound", "Scan result not found for this upload."));
        }

        return Result<ScanResultDto>.Success(
            new ScanResultDto(scan.UploadId, scan.Status, scan.ScannedAt));
    }
}
