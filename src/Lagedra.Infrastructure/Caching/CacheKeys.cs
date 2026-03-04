using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Lagedra.Infrastructure.Caching;

/// <summary>
/// Static helper for cache key format constants and builder methods.
/// Prevents key collisions across modules. Keys use format: {module}:{entity}:{identifier}.
/// </summary>
public static class CacheKeys
{
    public const string PlatformSettingPrefix = "platform_setting";
    public const string ListingDefsPrefix = "listing_defs";
    public const string JurisdictionPackPrefix = "jurisdiction_pack";
    public const string BlogPrefix = "blog";
    public const string SeoPrefix = "seo";
    public const string ListingPrefix = "listing";
    public const string NotifPrefsPrefix = "notif_prefs";
    public const string VerificationPrefix = "verification";
    public const string RiskPrefix = "risk";
    public const string InsurancePrefix = "insurance";
    public const string InquiryPrefix = "inquiry";
    public const string IdempotencyPrefix = "idempotency";

    public static string PlatformSetting(string key) =>
        $"{PlatformSettingPrefix}:{key}";

    public static string ListingDefinition(string type) =>
        $"{ListingDefsPrefix}:{type}";

    public static string JurisdictionPack(string code) =>
        $"{JurisdictionPackPrefix}:{code}";

    public static string BlogPublished(int page, int size) =>
        $"{BlogPrefix}:published:page:{page}:size:{size}";

    public static string BlogBySlug(string slug) =>
        $"{BlogPrefix}:slug:{slug}";

    public static string SeoBySlug(string slug) =>
        $"{SeoPrefix}:slug:{slug}";

    public static string BlogSitemap() =>
        $"{BlogPrefix}:sitemap";

    public static string ListingDetails(Guid id) =>
        $"{ListingPrefix}:{id}:details";

    /// <summary>
    /// Builds a cache key for listing search results. Uses a hash of the search params
    /// to keep keys bounded. TTL should be short (e.g. 2 min).
    /// </summary>
    public static string ListingSearch(string paramHash) =>
        $"{ListingPrefix}:search:{paramHash}";

    /// <summary>
    /// Computes a stable hash of search parameters for cache key.
    /// </summary>
    public static string ComputeSearchParamHash(
        string? keyword,
        double? lat, double? lon, double? radiusKm,
        string? propertyType,
        int? minBedrooms, int? minBathrooms,
        int? minStayDays, int? maxStayDays,
        long? minPrice, long? maxPrice,
        DateOnly? availableFrom, DateOnly? availableTo,
        string? amenityIds, string? safetyDeviceIds, string? considerationIds,
        string? sortBy,
        int page, int pageSize)
    {
        var sb = new StringBuilder();
        sb.Append(keyword ?? "");
        sb.Append('|').Append(lat ?? 0).Append('|').Append(lon ?? 0).Append('|').Append(radiusKm ?? 0);
        sb.Append('|').Append(propertyType ?? "").Append('|').Append(minBedrooms ?? 0).Append('|').Append(minBathrooms ?? 0);
        sb.Append('|').Append(minStayDays ?? 0).Append('|').Append(maxStayDays ?? 0);
        sb.Append('|').Append(minPrice ?? 0).Append('|').Append(maxPrice ?? 0);
        sb.Append('|').Append(availableFrom?.ToString("O", CultureInfo.InvariantCulture) ?? "").Append('|').Append(availableTo?.ToString("O", CultureInfo.InvariantCulture) ?? "");
        sb.Append('|').Append(amenityIds ?? "").Append('|').Append(safetyDeviceIds ?? "").Append('|').Append(considerationIds ?? "");
        sb.Append('|').Append(sortBy ?? "").Append('|').Append(page).Append('|').Append(pageSize);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes)[..16];
    }

    public static string NotificationPreferences(Guid userId) =>
        $"{NotifPrefsPrefix}:{userId}";

    public static string VerificationStatus(Guid userId) =>
        $"{VerificationPrefix}:{userId}";

    public static string RiskProfile(Guid tenantUserId) =>
        $"{RiskPrefix}:{tenantUserId}";

    public static string InsuranceStatus(Guid dealId) =>
        $"{InsurancePrefix}:{dealId}";

    public static string InquiryQuestions() =>
        $"{InquiryPrefix}:questions";

    public static string Idempotency(string key) =>
        $"{IdempotencyPrefix}:{key}";
}
