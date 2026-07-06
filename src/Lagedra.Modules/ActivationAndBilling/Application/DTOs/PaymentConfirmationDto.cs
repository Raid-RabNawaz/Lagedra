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
    DateTime? HostPaidPlatformAt,
    long ServiceFeeCents = 0,
    string? StripePaymentStatus = null,
    // Deposit return handshake (non-custodial, host-held).
    DateTime? MoveOutInitiatedAt = null,
    DateTime? HostConfirmedDepositReturnedAt = null,
    DateTime? TenantConfirmedDepositReceivedAt = null,
    long? DepositReturnAmountCents = null,
    string? DepositReturnMethod = null,
    string? DepositReturnNote = null,
    DateTime? DepositReturnSettledAt = null,
    // Deposit minus approved/settled damage deductions — what the host is
    // expected to return. Computed on the status query only.
    long? NetReturnableDepositCents = null);
