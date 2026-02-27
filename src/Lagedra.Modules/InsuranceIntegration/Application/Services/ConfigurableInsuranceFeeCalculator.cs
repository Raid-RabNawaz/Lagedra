using Lagedra.SharedKernel.Insurance;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

public sealed class ConfigurableInsuranceFeeCalculator(IConfiguration configuration)
    : IInsuranceFeeCalculator
{
    public Task<InsuranceFeeQuote> CalculateFeeAsync(
        long monthlyRentCents,
        int stayDurationDays,
        CancellationToken ct = default)
    {
        var rateStr = configuration["Insurance:FeeRatePerMonth"];
        var rate = !string.IsNullOrWhiteSpace(rateStr) && decimal.TryParse(rateStr,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0.05m;

        var months = (int)Math.Ceiling(stayDurationDays / 30.0);
        var feeCents = (long)(monthlyRentCents * rate * months);

        var quote = new InsuranceFeeQuote(feeCents, "Configurable", null);
        return Task.FromResult(quote);
    }
}
