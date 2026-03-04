using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

public sealed record CompleteManualVerificationCommand(
    Guid DealId,
    bool IsActive,
    string? Provider,
    string? PolicyNumber) : IRequest<Result<InsuranceStatusDto>>;

public sealed class CompleteManualVerificationCommandHandler(
    InsuranceDbContext dbContext)
    : IRequestHandler<CompleteManualVerificationCommand, Result<InsuranceStatusDto>>
{
    public async Task<Result<InsuranceStatusDto>> Handle(
        CompleteManualVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = await dbContext.PolicyRecords
            .FirstOrDefaultAsync(r => r.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return Result<InsuranceStatusDto>.Failure(
                new Error("Insurance.NotFound", $"No policy record found for deal '{request.DealId}'."));
        }

        if (request.IsActive)
        {
            record.RecordActive(request.Provider, request.PolicyNumber);
        }
        else
        {
            record.RecordNotActive();
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InsuranceStatusDto>.Success(MapToDto(record));
    }

    private static InsuranceStatusDto MapToDto(InsurancePolicyRecord r) =>
        new(r.Id, r.DealId, r.TenantUserId, r.State,
            r.Provider, r.PolicyNumber, r.VerifiedAt,
            r.ExpiresAt, r.CoverageScope);
}
