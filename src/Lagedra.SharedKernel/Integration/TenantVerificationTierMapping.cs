namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Maps persisted and API tier names, including pre-2026 snapshot strings
/// (<c>BackgroundVerified</c>, <c>PartnerGuaranteed</c>).
/// </summary>
public static class TenantVerificationTierMapping
{
    public static string ToStorage(TenantVerificationTier tier) => tier.ToString();

    public static TenantVerificationTier Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TenantVerificationTier.Unverified;
        }

        return value switch
        {
            "Verified" => TenantVerificationTier.Verified,
            "Trusted" or "BackgroundVerified" => TenantVerificationTier.Trusted,
            "Elite" or "PartnerGuaranteed" => TenantVerificationTier.Elite,
            "Blocklisted" => TenantVerificationTier.Blocklisted,
            _ => TenantVerificationTier.Unverified
        };
    }

    public static TenantVerificationTier? TryParse(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Parse(value);
}
