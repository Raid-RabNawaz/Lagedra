using Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;
using Lagedra.SharedKernel.Insurance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

/// <summary>
/// Tenant-facing stay-protection quote. Truvi has no quote API — this is the
/// wholesale nightly schedule they invoice Lagedra, recovered from the tenant
/// at booking. The line stays at $0 until Screen &amp; Protect can actually
/// be placed (<see cref="TruviScreenAndProtectSettings.CanCallApi"/>).
/// </summary>
public sealed class TruviStayProtectionFeeCalculator(
    IConfiguration configuration,
    IOptions<TruviScreenAndProtectSettings> settings)
    : IInsuranceFeeCalculator
{
    private static readonly InsuranceFeeQuote Unavailable = new(0, "None", null);

    public Task<InsuranceFeeQuote> CalculateFeeAsync(
        long monthlyRentCents,
        int stayDurationDays,
        CancellationToken ct = default)
    {
        _ = monthlyRentCents;
        _ = ct;

        if (!settings.Value.CanCallApi)
        {
            return Task.FromResult(Unavailable);
        }

        var firstNights = ReadInt(
            configuration,
            "Insurance:Truvi:FirstNights",
            StayProtectionFee.DefaultFirstNights);
        var firstNightsFeeCents = ReadLong(
            configuration,
            "Insurance:Truvi:FirstNightsFeeCents",
            StayProtectionFee.DefaultFirstNightsFeeCents);
        var additionalNightFeeCents = ReadLong(
            configuration,
            "Insurance:Truvi:AdditionalNightFeeCents",
            StayProtectionFee.DefaultAdditionalNightFeeCents);

        var feeCents = StayProtectionFee.ComputeNightlyFeeCents(
            stayDurationDays,
            firstNights,
            firstNightsFeeCents,
            additionalNightFeeCents);

        return Task.FromResult(new InsuranceFeeQuote(feeCents, "Truvi", null));
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
        => int.TryParse(
            configuration[key],
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : fallback;

    private static long ReadLong(IConfiguration configuration, string key, long fallback)
        => long.TryParse(
            configuration[key],
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : fallback;
}
