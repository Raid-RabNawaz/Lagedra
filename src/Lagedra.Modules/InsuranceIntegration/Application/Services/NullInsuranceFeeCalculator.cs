using Lagedra.SharedKernel.Insurance;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

/// <summary>
/// Returns a zero-fee quote with provider "None". Used when no
/// third-party insurance partner is wired up and the platform should
/// not surface any insurance line on the booking quote.
///
/// This is the default registration when <c>Insurance:FeeCalculationMode</c>
/// is unset or explicitly set to "None" — operators must opt in to a real
/// calculator (Configurable / Api) before quotes start charging.
/// </summary>
public sealed class NullInsuranceFeeCalculator : IInsuranceFeeCalculator
{
    private static readonly InsuranceFeeQuote ZeroQuote = new(0, "None", null);

    public Task<InsuranceFeeQuote> CalculateFeeAsync(
        long monthlyRentCents,
        int stayDurationDays,
        CancellationToken ct = default)
        => Task.FromResult(ZeroQuote);
}
