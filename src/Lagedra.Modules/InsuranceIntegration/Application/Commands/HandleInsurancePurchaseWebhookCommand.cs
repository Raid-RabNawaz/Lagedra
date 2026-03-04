using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

public sealed record HandleInsurancePurchaseWebhookCommand(
    Guid DealId,
    string Provider,
    string PolicyNumber,
    string? CoverageScope,
    DateTime? PolicyExpiresAt,
    string WebhookPayload) : IRequest<Result<InsuranceStatusDto>>;

public sealed class HandleInsurancePurchaseWebhookCommandHandler(
    InsuranceDbContext dbContext)
    : IRequestHandler<HandleInsurancePurchaseWebhookCommand, Result<InsuranceStatusDto>>
{
    public async Task<Result<InsuranceStatusDto>> Handle(
        HandleInsurancePurchaseWebhookCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = await dbContext.PolicyRecords
            .FirstOrDefaultAsync(r => r.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return Result<InsuranceStatusDto>.Failure(
                new Error("Insurance.NotFound",
                    $"No policy record found for deal '{request.DealId}'."));
        }

        var attempt = new InsuranceVerificationAttempt(
            record.Id,
            $"Webhook received: {request.WebhookPayload[..Math.Min(200, request.WebhookPayload.Length)]}",
            VerificationSource.API);
        record.AddAttempt(attempt);

        record.RecordActive(
            request.Provider,
            request.PolicyNumber,
            request.CoverageScope,
            request.PolicyExpiresAt);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InsuranceStatusDto>.Success(MapToDto(record));
    }

    private static InsuranceStatusDto MapToDto(InsurancePolicyRecord r) =>
        new(r.Id, r.DealId, r.TenantUserId, r.State,
            r.Provider, r.PolicyNumber, r.VerifiedAt,
            r.ExpiresAt, r.CoverageScope);
}
