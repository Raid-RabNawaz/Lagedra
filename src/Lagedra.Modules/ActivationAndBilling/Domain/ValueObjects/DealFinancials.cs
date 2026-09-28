namespace Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;

public sealed record DealFinancials
{
    public long FirstMonthRentCents { get; init; }
    public long DepositAmountCents { get; init; }
    public long InsuranceFeeCents { get; init; }
    public long MonthlyProtocolFeeCents { get; init; }

    /// <summary>
    /// Platform service fee charged to the tenant at checkout and kept by the
    /// platform. Included in <see cref="TotalTenantPaymentCents"/>.
    /// </summary>
    public long ServiceFeeCents { get; init; }

    /// <summary>
    /// Snapshotted Lagedra Partner Eviction Protection fee. Kept by the
    /// platform. Zero when the guest was not endorsed, or when the admin
    /// waived the fee.
    /// </summary>
    public long EvictionProtectionFeeCents { get; init; }

    public long TotalTenantPaymentCents =>
        FirstMonthRentCents + DepositAmountCents + InsuranceFeeCents + ServiceFeeCents
        + EvictionProtectionFeeCents;

    public long TotalHostPlatformPaymentCents =>
        InsuranceFeeCents + MonthlyProtocolFeeCents;

    private DealFinancials() { }

    public static DealFinancials Create(
        long firstMonthRentCents,
        long depositAmountCents,
        long insuranceFeeCents,
        long monthlyProtocolFeeCents,
        long serviceFeeCents = 0,
        long evictionProtectionFeeCents = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstMonthRentCents);
        ArgumentOutOfRangeException.ThrowIfNegative(depositAmountCents);
        ArgumentOutOfRangeException.ThrowIfNegative(insuranceFeeCents);
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyProtocolFeeCents);
        ArgumentOutOfRangeException.ThrowIfNegative(serviceFeeCents);
        ArgumentOutOfRangeException.ThrowIfNegative(evictionProtectionFeeCents);

        return new DealFinancials
        {
            FirstMonthRentCents = firstMonthRentCents,
            DepositAmountCents = depositAmountCents,
            InsuranceFeeCents = insuranceFeeCents,
            MonthlyProtocolFeeCents = monthlyProtocolFeeCents,
            ServiceFeeCents = serviceFeeCents,
            EvictionProtectionFeeCents = evictionProtectionFeeCents
        };
    }
}
