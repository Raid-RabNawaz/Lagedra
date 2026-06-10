using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.Modules.Evidence.Application.Services;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Queries;

public sealed record GetScanStatusQuery(Guid UploadId, EvidenceCallerContext Caller)
    : IRequest<Result<ScanResultDto>>;

public sealed class GetScanStatusQueryHandler(
    EvidenceDbContext dbContext,
    EvidenceViewAccessService accessService)
    : IRequestHandler<GetScanStatusQuery, Result<ScanResultDto>>
{
    public async Task<Result<ScanResultDto>> Handle(
        GetScanStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await accessService
            .RequireUploadViewAsync(request.Caller, request.UploadId, cancellationToken)
            .ConfigureAwait(false);

        if (!access.IsSuccess)
        {
            return Result<ScanResultDto>.Failure(access.Error);
        }

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
