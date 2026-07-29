using Lagedra.Modules.Analytics.Application.DTOs;
using Lagedra.SharedKernel.Results;
using MediatR;
using Npgsql;
using NpgsqlTypes;

namespace Lagedra.Modules.Analytics.Application.Queries;

public sealed record GetPlatformSummaryQuery(
    DateTime? StartDate,
    DateTime? EndDate) : IRequest<Result<PlatformSummaryDto>>;

/// <summary>
/// Cross-module platform KPIs read directly via ADO.NET. Every table lives in
/// its owning module's schema, so all names are schema-qualified. The date
/// range is inclusive of the whole end day and applies to the "in period"
/// figures (listings added, applications, new deals, conversion); active
/// deals and MRR are point-in-time as of the end of the period.
/// </summary>
public sealed class GetPlatformSummaryQueryHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<GetPlatformSummaryQuery, Result<PlatformSummaryDto>>
{
    // Hosting subscription price used for MRR until per-account pricing lands.
    private const long MonthlyFeeCents = 7900;

    public async Task<Result<PlatformSummaryDto>> Handle(
        GetPlatformSummaryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var utcNow = DateTime.UtcNow;
        var start = DateTime.SpecifyKind(
            (request.StartDate ?? utcNow.AddMonths(-1)).Date, DateTimeKind.Utc);
        // Exclusive upper bound so the selected end date counts in full.
        var endExclusive = DateTime.SpecifyKind(
            (request.EndDate?.Date.AddDays(1)) ?? utcNow, DateTimeKind.Utc);
        var periodEnd = DateTime.SpecifyKind(
            (request.EndDate ?? utcNow).Date, DateTimeKind.Utc);

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand("""
            SELECT
                (SELECT COUNT(*) FROM listings.listings
                 WHERE "IsDeleted" = false) AS total_listings,
                (SELECT COUNT(*) FROM listings.listings
                 WHERE "IsDeleted" = false
                   AND "CreatedAt" >= @start AND "CreatedAt" < @end) AS listings_added,
                (SELECT COUNT(*) FROM activation_billing.billing_accounts
                 WHERE "IsDeleted" = false AND "Status" = 'Active'
                   AND "CreatedAt" < @end) AS active_deals,
                (SELECT COUNT(*) FROM activation_billing.billing_accounts
                 WHERE "IsDeleted" = false
                   AND "CreatedAt" >= @start AND "CreatedAt" < @end) AS new_deals,
                (SELECT COUNT(*) FROM activation_billing.deal_applications
                 WHERE "IsDeleted" = false
                   AND "CreatedAt" >= @start AND "CreatedAt" < @end) AS applications
            """, conn);

        cmd.Parameters.Add(new NpgsqlParameter("start", NpgsqlDbType.TimestampTz) { Value = start });
        cmd.Parameters.Add(new NpgsqlParameter("end", NpgsqlDbType.TimestampTz) { Value = endExclusive });

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        var totalListings = (int)reader.GetInt64(0);
        var listingsAdded = (int)reader.GetInt64(1);
        var activeDeals = (int)reader.GetInt64(2);
        var newDeals = (int)reader.GetInt64(3);
        var totalApplications = (int)reader.GetInt64(4);

        var mrrCents = MonthlyFeeCents * activeDeals;
        var conversionRate = totalApplications > 0
            ? (double)newDeals / totalApplications * 100
            : 0;

        return Result<PlatformSummaryDto>.Success(new PlatformSummaryDto(
            totalListings,
            listingsAdded,
            activeDeals,
            newDeals,
            totalApplications,
            mrrCents,
            Math.Round(conversionRate, 2),
            start,
            periodEnd));
    }
}
