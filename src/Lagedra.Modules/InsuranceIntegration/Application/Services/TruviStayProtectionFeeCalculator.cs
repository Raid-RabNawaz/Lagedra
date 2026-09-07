using Lagedra.SharedKernel.Insurance;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

/// <summary>
/// Tenant-facing stay-protection quote. Truvi has no quote API — this is the
/// wholesale nightly schedule they invoice Lagedra, recovered from the tenant
/// at booking.
/// </summary>
public sealed class TruviStayProtectionFeeCalculator(IConfiguration configuration)
    : IInsuranceFeeCalculator
{
    public Task<InsuranceFeeQuote> CalculateFeeAsync(
        long monthlyRentCents,
        int stayDurationDays,
        CancellationToken ct = default)
    {
        _ = monthlyRentCents;
        _ = ct;

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
