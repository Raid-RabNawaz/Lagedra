using Lagedra.SharedKernel.Results;
using Lagedra.Modules.Analytics.Application.DTOs;
using MediatR;
using Npgsql;
using NpgsqlTypes;

namespace Lagedra.Modules.Analytics.Application.Queries;

/// <summary>
/// Per-listing performance metrics for the admin portal, filterable by
/// landlord (exact id or name/email search), listing status, and the date
/// the listing was added.
/// </summary>
public sealed record GetListingAnalyticsQuery(
    Guid? LandlordUserId = null,
    string? Search = null,
    string? Status = null,
    DateTime? AddedFrom = null,
    DateTime? AddedTo = null) : IRequest<Result<IReadOnlyList<ListingAnalyticsItemDto>>>;

public sealed class GetListingAnalyticsQueryHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<GetListingAnalyticsQuery, Result<IReadOnlyList<ListingAnalyticsItemDto>>>
{
    // Statuses stored by ListingAndLocation (enum persisted as string). Kept in
    // sync manually because module boundaries forbid referencing that assembly.
    private static readonly HashSet<string> KnownStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "InReview", "Published", "Activated", "Closed", "Denied",
    };

    public async Task<Result<IReadOnlyList<ListingAnalyticsItemDto>>> Handle(
        GetListingAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            status = KnownStatuses.FirstOrDefault(
                s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
            if (status is null)
            {
                return Result<IReadOnlyList<ListingAnalyticsItemDto>>.Failure(
                    new Error("Analytics.InvalidStatus",
                        $"Unknown listing status '{request.Status}'."));
            }
        }

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var items = new List<ListingAnalyticsItemDto>();

        await using var cmd = new NpgsqlCommand("""
            SELECT l."Id", l."Title", l."LandlordUserId",
                   COALESCE(u."FirstName", '') AS first_name,
                   COALESCE(u."LastName", '') AS last_name,
                   u."Email",
                   l."Status", l."CreatedAt", l."MonthlyRentCents",
                   COALESCE(a.app_count, 0) AS app_count,
                   COALESCE(d.deal_count, 0) AS deal_count,
                   LENGTH(l."Description") AS description_length,
                   COALESCE(p.photo_count, 0) AS photo_count,
                   COALESCE(am.amenity_count, 0) AS amenity_count,
                   COALESCE(sd.safety_count, 0) AS safety_count,
                   (l.house_rules_max_guests IS NOT NULL
                        OR l.house_rules_pets_allowed IS NOT NULL) AS has_house_rules,
                   (l.cancellation_policy_type IS NOT NULL) AS has_cancellation_policy,
                   COALESCE(u."IsGovernmentIdVerified", false) AS host_verified
            FROM listings.listings l
            LEFT JOIN auth."AspNetUsers" u ON u."Id" = l."LandlordUserId"
            LEFT JOIN (
                SELECT "ListingId", COUNT(*) AS app_count
                FROM activation_billing.deal_applications WHERE "IsDeleted" = false
                GROUP BY "ListingId"
            ) a ON a."ListingId" = l."Id"
            LEFT JOIN (
                SELECT da."ListingId", COUNT(*) AS deal_count
                FROM activation_billing.deal_applications da
                INNER JOIN activation_billing.billing_accounts ba ON ba."DealId" = da."DealId"
                WHERE da."IsDeleted" = false AND ba."IsDeleted" = false
                GROUP BY da."ListingId"
            ) d ON d."ListingId" = l."Id"
            LEFT JOIN (
                SELECT "ListingId", COUNT(*) AS photo_count
                FROM listings.listing_photos WHERE "IsDeleted" = false
                GROUP BY "ListingId"
            ) p ON p."ListingId" = l."Id"
            LEFT JOIN (
                SELECT "ListingId", COUNT(*) AS amenity_count
                FROM listings.listing_amenities
                GROUP BY "ListingId"
            ) am ON am."ListingId" = l."Id"
            LEFT JOIN (
                SELECT "ListingId", COUNT(*) AS safety_count
                FROM listings.listing_safety_devices
                GROUP BY "ListingId"
            ) sd ON sd."ListingId" = l."Id"
            WHERE l."IsDeleted" = false
              AND (@landlordUserId IS NULL OR l."LandlordUserId" = @landlordUserId)
              AND (@status IS NULL OR l."Status" = @status)
              AND (@addedFrom IS NULL OR l."CreatedAt" >= @addedFrom)
              AND (@addedTo IS NULL OR l."CreatedAt" < @addedTo)
              AND (@search IS NULL
                   OR u."Email" ILIKE @search
                   OR CONCAT(u."FirstName", ' ', u."LastName") ILIKE @search)
            ORDER BY l."CreatedAt" DESC
            """, conn);

        var addedFrom = request.AddedFrom is { } from
            ? DateTime.SpecifyKind(from.Date, DateTimeKind.Utc)
            : (DateTime?)null;
        // Exclusive upper bound so the selected "to" date counts in full.
        var addedToExclusive = request.AddedTo is { } to
            ? DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc)
            : (DateTime?)null;
        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : $"%{request.Search.Trim()}%";

        cmd.Parameters.Add(new NpgsqlParameter("landlordUserId", NpgsqlDbType.Uuid)
        { Value = ToDbValue(request.LandlordUserId) });
        cmd.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text)
        { Value = ToDbValue(status) });
        cmd.Parameters.Add(new NpgsqlParameter("addedFrom", NpgsqlDbType.TimestampTz)
        { Value = ToDbValue(addedFrom) });
        cmd.Parameters.Add(new NpgsqlParameter("addedTo", NpgsqlDbType.TimestampTz)
        { Value = ToDbValue(addedToExclusive) });
        cmd.Parameters.Add(new NpgsqlParameter("search", NpgsqlDbType.Text)
        { Value = ToDbValue(search) });

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var listingId = reader.GetGuid(0);
            var title = reader.GetString(1);
            var landlordUserId = reader.GetGuid(2);
            var firstName = reader.GetString(3);
            var lastName = reader.GetString(4);
            var email = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                ? null
                : reader.GetString(5);
            var listingStatus = reader.GetString(6);
            var createdAt = reader.GetDateTime(7);
            var rentCents = reader.GetInt64(8);
            var appCount = (int)reader.GetInt64(9);
            var dealCount = (int)reader.GetInt64(10);
            var descriptionLength = reader.GetInt32(11);
            var photoCount = (int)reader.GetInt64(12);
            var amenityCount = (int)reader.GetInt64(13);
            var safetyCount = (int)reader.GetInt64(14);
            var hasHouseRules = reader.GetBoolean(15);
            var hasCancellationPolicy = reader.GetBoolean(16);
            var hostVerified = reader.GetBoolean(17);

            var landlordName = $"{firstName} {lastName}".Trim();
            if (landlordName.Length == 0)
            {
                landlordName = email ?? "Unknown";
            }

            var conversionPercent = appCount > 0 ? (double)dealCount / appCount * 100 : 0;
            var qualityScore = ComputeQualityScore(
                photoCount, descriptionLength, amenityCount, safetyCount,
                hasHouseRules, hasCancellationPolicy, hostVerified);

            items.Add(new ListingAnalyticsItemDto(
                listingId, title, landlordUserId, landlordName, email,
                listingStatus, createdAt, rentCents,
                appCount, Math.Round(conversionPercent, 2), qualityScore));
        }

        return Result<IReadOnlyList<ListingAnalyticsItemDto>>.Success(items);
    }

    private static object ToDbValue<T>(T? value) where T : struct =>
        value.HasValue ? value.Value : DBNull.Value;

    private static object ToDbValue(string? value) =>
        value is null ? DBNull.Value : value;

    /// <summary>
    /// Mirrors ListingAndLocation's ListingQualityScoreCalculator completeness
    /// weights (that domain service can't be referenced across the module
    /// boundary). Host response rate and rating aren't tracked here, so those
    /// components contribute 0 — identical to how the calculator treats
    /// missing inputs.
    /// </summary>
    private static double ComputeQualityScore(
        int photoCount,
        int descriptionLength,
        int amenityCount,
        int safetyCount,
        bool hasHouseRules,
        bool hasCancellationPolicy,
        bool hostVerified)
    {
        var score = 0.0;
        score += Math.Min(photoCount / 5.0, 1.0) * 25;
        score += Math.Min(descriptionLength / 500.0, 1.0) * 15;
        score += Math.Min(amenityCount / 10.0, 1.0) * 15;
        score += Math.Min(safetyCount / 3.0, 1.0) * 10;
        if (hasHouseRules) score += 5;
        if (hasCancellationPolicy) score += 5;
        if (hostVerified) score += 10;
        return Math.Round(Math.Clamp(score, 0, 100));
    }
}
