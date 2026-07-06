using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;

/// <summary>
/// The pricing snapshot captured on a <c>DealApplication</c> at reservation
/// request time. The deposit is the predetermined amount selected for the
/// tenant's <see cref="Tier"/>; the fees are quoted up-front so the tenant sees
/// (and the agreement records) the exact total payable before the host decides.
/// Snapshotting at request time guarantees the price can't drift between
/// request and host approval.
/// </summary>
public sealed record ReservationDepositSnapshot(
    TenantVerificationTier Tier,
    long DepositAmountCents,
    long FirstMonthRentCents,
    long InsuranceFeeCents,
    long ServiceFeeCents,
    string DepositReason)
{
    public long TotalPayableCents =>
        DepositAmountCents + FirstMonthRentCents + InsuranceFeeCents + ServiceFeeCents;
}
