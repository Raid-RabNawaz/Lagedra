using Lagedra.Modules.Privacy.Application.DTOs;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Application.Queries;

public sealed record GetDataExportStatusQuery(Guid ExportId) : IRequest<Result<DataExportStatusDto>>;

public sealed class GetDataExportStatusQueryHandler(PrivacyDbContext dbContext)
    : IRequestHandler<GetDataExportStatusQuery, Result<DataExportStatusDto>>
{
    public async Task<Result<DataExportStatusDto>> Handle(
        GetDataExportStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var export = await dbContext.DataExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.ExportId, cancellationToken)
            .ConfigureAwait(false);

        if (export is null)
        {
            return Result<DataExportStatusDto>.Failure(
                new Error("Privacy.Export.NotFound", "Data export request not found."));
        }

        return Result<DataExportStatusDto>.Success(new DataExportStatusDto(
            export.Id, export.UserId, export.Status,
            export.RequestedAt, export.CompletedAt, export.PackageUrl));
    }
}
