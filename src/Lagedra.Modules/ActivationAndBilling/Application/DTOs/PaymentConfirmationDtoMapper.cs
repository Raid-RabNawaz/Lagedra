using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

/// <summary>
/// Maps a <see cref="DealPaymentConfirmation"/> aggregate to its DTO. The
/// deposit-return handshake fields are always projected; the net returnable
/// deposit is optional because it requires reading damage-claim deductions and
/// is only computed on the status query.
/// </summary>
public static class PaymentConfirmationDtoMapper
{
    public static PaymentConfirmationDto ToDto(
        DealPaymentConfirmation c,
        long? netReturnableDepositCents = null)
    {
        ArgumentNullException.ThrowIfNull(c);

        return new PaymentConfirmationDto(
            c.Id,
            c.DealId,
            c.Status,
            c.HostConfirmed,
            c.HostConfirmedAt,
            c.TenantDisputed,
            c.TenantDisputedAt,
            c.DisputeReason,
            c.GracePeriodExpiresAt,
            c.TotalTenantPaymentCents,
            c.TotalHostPlatformPaymentCents,
            c.FirstMonthRentCents,
            c.DepositAmountCents,
            c.InsuranceFeeCents,
            c.MonthlyProtocolFeeCents,
            c.HostPaidPlatform,
            c.HostPaidPlatformAt,
            c.ServiceFeeCents,
            c.StripePaymentStatus,
            c.MoveOutInitiatedAt,
            c.HostConfirmedDepositReturnedAt,
            c.TenantConfirmedDepositReceivedAt,
            c.DepositReturnAmountCents,
            c.DepositReturnMethod,
            c.DepositReturnNote,
            c.DepositReturnSettledAt,
            netReturnableDepositCents);
    }
}
