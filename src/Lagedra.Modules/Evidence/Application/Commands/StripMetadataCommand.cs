using Lagedra.Modules.Evidence.Domain.Entities;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record StripMetadataCommand(
    Guid UploadId,
    string RemovedFieldsJson) : IRequest<Result>;

public sealed class StripMetadataCommandHandler(
    EvidenceDbContext dbContext)
    : IRequestHandler<StripMetadataCommand, Result>
{
    public async Task<Result> Handle(
        StripMetadataCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var upload = await dbContext.Uploads
            .FirstOrDefaultAsync(u => u.Id == request.UploadId, cancellationToken)
            .ConfigureAwait(false);

        if (upload is null)
        {
            return Result.Failure(
                new Error("Evidence.UploadNotFound", "Upload not found."));
        }

        var log = MetadataStrippingLog.Create(
            request.UploadId, DateTime.UtcNow, request.RemovedFieldsJson);

        dbContext.MetadataStrippingLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
