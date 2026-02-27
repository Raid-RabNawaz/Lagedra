namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record CancellationResultDto(
    Guid DealId,
    long TenantRefundCents,
    long InsuranceRefundCents,
    string PolicyApplied);
