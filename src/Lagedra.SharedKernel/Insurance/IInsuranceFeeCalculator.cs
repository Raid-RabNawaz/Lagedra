namespace Lagedra.SharedKernel.Insurance;

public interface IInsuranceFeeCalculator
{
    Task<InsuranceFeeQuote> CalculateFeeAsync(
        long monthlyRentCents,
        int stayDurationDays,
        CancellationToken ct = default);
}
