using Lagedra.Modules.ActivationAndBilling.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record PaymentConfirmationDto(
    Guid Id,
    Guid DealId,
    PaymentConfirmationStatus Status,
    bool HostConfirmed,
    DateTime? HostConfirmedAt,
    bool TenantDisputed,
    DateTime? TenantDisputedAt,
    string? DisputeReason,
    DateTime GracePeriodExpiresAt,
    long TotalTenantPaymentCents,
    long TotalHostPlatformPaymentCents,
    long FirstMonthRentCents,
    long DepositAmountCents,
    long InsuranceFeeCents,
    long MonthlyProtocolFeeCents,
    bool HostPaidPlatform,
    DateTime? HostPaidPlatformAt);
