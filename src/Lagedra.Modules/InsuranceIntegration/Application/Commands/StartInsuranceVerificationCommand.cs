using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

public sealed record StartInsuranceVerificationCommand(
    Guid DealId,
    Guid TenantUserId) : IRequest<Result<InsuranceStatusDto>>;

public sealed class StartInsuranceVerificationCommandHandler(
    InsuranceDbContext dbContext)
    : IRequestHandler<StartInsuranceVerificationCommand, Result<InsuranceStatusDto>>
{
    public async Task<Result<InsuranceStatusDto>> Handle(
        StartInsuranceVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = await dbContext.PolicyRecords
            .FirstOrDefaultAsync(r => r.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            record = InsurancePolicyRecord.Create(request.TenantUserId, request.DealId);
            dbContext.PolicyRecords.Add(record);
        }

        var attempt = new InsuranceVerificationAttempt(
            record.Id, "Verification initiated", VerificationSource.API);
        record.AddAttempt(attempt);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InsuranceStatusDto>.Success(MapToDto(record));
    }

    private static InsuranceStatusDto MapToDto(InsurancePolicyRecord r) =>
        new(r.Id, r.DealId, r.TenantUserId, r.State,
            r.Provider, r.PolicyNumber, r.VerifiedAt,
            r.ExpiresAt, r.CoverageScope);
}
