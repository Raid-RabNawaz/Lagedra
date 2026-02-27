using Lagedra.Modules.Privacy.Application.DTOs;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using DomainExportRequest = Lagedra.Modules.Privacy.Domain.Entities.DataExportRequest;

namespace Lagedra.Modules.Privacy.Application.Commands;

public sealed record EnqueueDataExportCommand(Guid UserId) : IRequest<Result<DataExportStatusDto>>;

public sealed class EnqueueDataExportCommandHandler(PrivacyDbContext dbContext)
    : IRequestHandler<EnqueueDataExportCommand, Result<DataExportStatusDto>>
{
    public async Task<Result<DataExportStatusDto>> Handle(
        EnqueueDataExportCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exportRequest = DomainExportRequest.Create(request.UserId);
        dbContext.DataExportRequests.Add(exportRequest);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DataExportStatusDto>.Success(new DataExportStatusDto(
            exportRequest.Id, exportRequest.UserId, exportRequest.Status,
            exportRequest.RequestedAt, exportRequest.CompletedAt, exportRequest.PackageUrl));
    }
}
