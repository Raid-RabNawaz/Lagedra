namespace Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;

public sealed record DealFinancials
{
    public long FirstMonthRentCents { get; init; }
    public long DepositAmountCents { get; init; }
    public long InsuranceFeeCents { get; init; }
    public long MonthlyProtocolFeeCents { get; init; }

    public long TotalTenantPaymentCents =>
        FirstMonthRentCents + DepositAmountCents + InsuranceFeeCents;

    public long TotalHostPlatformPaymentCents =>
        InsuranceFeeCents + MonthlyProtocolFeeCents;

    private DealFinancials() { }

    public static DealFinancials Create(
        long firstMonthRentCents,
        long depositAmountCents,
        long insuranceFeeCents,
        long monthlyProtocolFeeCents)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstMonthRentCents);
        ArgumentOutOfRangeException.ThrowIfNegative(depositAmountCents);
        ArgumentOutOfRangeException.ThrowIfNegative(insuranceFeeCents);
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyProtocolFeeCents);

        return new DealFinancials
        {
            FirstMonthRentCents = firstMonthRentCents,
            DepositAmountCents = depositAmountCents,
            InsuranceFeeCents = insuranceFeeCents,
            MonthlyProtocolFeeCents = monthlyProtocolFeeCents
        };
    }
}
