using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

public sealed record UploadManualProofCommand(
    Guid DealId,
    string DocumentReference) : IRequest<Result>;

public sealed class UploadManualProofCommandHandler(
    InsuranceDbContext dbContext)
    : IRequestHandler<UploadManualProofCommand, Result>
{
    public async Task<Result> Handle(
        UploadManualProofCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = await dbContext.PolicyRecords
            .FirstOrDefaultAsync(r => r.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return Result.Failure(
                new Error("Insurance.NotFound", $"No policy record found for deal '{request.DealId}'."));
        }

        var attempt = new InsuranceVerificationAttempt(
            record.Id,
            $"Manual proof uploaded: {request.DocumentReference}",
            VerificationSource.ManualUpload);

        record.AddAttempt(attempt);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
