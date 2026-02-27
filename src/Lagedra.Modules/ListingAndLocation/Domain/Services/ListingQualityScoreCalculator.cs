namespace Lagedra.Modules.ListingAndLocation.Domain.Services;

/// <summary>
/// Computes a 0–100 quality score for a listing based on completeness,
/// host verification, and responsiveness. The score is read-only and
/// never persisted — it is computed on every query so it always reflects
/// the latest data.
/// </summary>
public static class ListingQualityScoreCalculator
{
    private const int MaxPhotos = 5;
    private const int MaxDescriptionLength = 500;
    private const int MaxAmenities = 10;
    private const int MaxSafetyDevices = 3;

    private const int PhotoWeight = 25;
    private const int DescriptionWeight = 15;
    private const int AmenityWeight = 15;
    private const int SafetyWeight = 10;
    private const int HouseRulesWeight = 5;
    private const int CancellationPolicyWeight = 5;
    private const int HostVerifiedWeight = 10;
    private const int ResponseRateWeight = 15;

    public static int Calculate(
        int photoCount,
        int descriptionLength,
        int amenityCount,
        int safetyDeviceCount,
        bool hasHouseRules,
        bool hasCancellationPolicy,
        bool isHostVerified,
        int? hostResponseRatePercent)
    {
        var score = 0.0;

        score += Ratio(photoCount, MaxPhotos) * PhotoWeight;
        score += Ratio(descriptionLength, MaxDescriptionLength) * DescriptionWeight;
        score += Ratio(amenityCount, MaxAmenities) * AmenityWeight;
        score += Ratio(safetyDeviceCount, MaxSafetyDevices) * SafetyWeight;

        if (hasHouseRules) score += HouseRulesWeight;
        if (hasCancellationPolicy) score += CancellationPolicyWeight;
        if (isHostVerified) score += HostVerifiedWeight;

        if (hostResponseRatePercent.HasValue)
        {
            score += (hostResponseRatePercent.Value / 100.0) * ResponseRateWeight;
        }

        return (int)Math.Round(Math.Clamp(score, 0, 100));
    }

    private static double Ratio(int actual, int target) =>
        Math.Min((double)actual / target, 1.0);
}
