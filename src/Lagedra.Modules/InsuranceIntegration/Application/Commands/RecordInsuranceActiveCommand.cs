using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

public sealed record RecordInsuranceActiveCommand(
    Guid DealId,
    string? Provider,
    string? PolicyNumber,
    string? CoverageScope,
    DateTime? ExpiresAt) : IRequest<Result<InsuranceStatusDto>>;

public sealed class RecordInsuranceActiveCommandHandler(
    InsuranceDbContext dbContext)
    : IRequestHandler<RecordInsuranceActiveCommand, Result<InsuranceStatusDto>>
{
    public async Task<Result<InsuranceStatusDto>> Handle(
        RecordInsuranceActiveCommand request,
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

        record.RecordActive(request.Provider, request.PolicyNumber, request.CoverageScope, request.ExpiresAt);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InsuranceStatusDto>.Success(MapToDto(record));
    }

    private static InsuranceStatusDto MapToDto(InsurancePolicyRecord r) =>
        new(r.Id, r.DealId, r.TenantUserId, r.State,
            r.Provider, r.PolicyNumber, r.VerifiedAt,
            r.ExpiresAt, r.CoverageScope);
}
