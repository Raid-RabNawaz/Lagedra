using Lagedra.SharedKernel.Results;
using Lagedra.Modules.Analytics.Application.DTOs;
using MediatR;
using Npgsql;

namespace Lagedra.Modules.Analytics.Application.Queries;

public sealed record GetListingAnalyticsQuery : IRequest<Result<IReadOnlyList<ListingAnalyticsItemDto>>>;

public sealed class GetListingAnalyticsQueryHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<GetListingAnalyticsQuery, Result<IReadOnlyList<ListingAnalyticsItemDto>>>
{
    public async Task<Result<IReadOnlyList<ListingAnalyticsItemDto>>> Handle(
        GetListingAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var items = new List<ListingAnalyticsItemDto>();

        await using var cmd = new NpgsqlCommand("""
            SELECT l."Id", l."Title", l."QualityScore",
                   COALESCE(a.app_count, 0) AS app_count,
                   COALESCE(d.deal_count, 0) AS deal_count
            FROM listings l
            LEFT JOIN (
                SELECT "ListingId", COUNT(*) AS app_count
                FROM deal_applications WHERE "IsDeleted" = false
                GROUP BY "ListingId"
            ) a ON a."ListingId" = l."Id"
            LEFT JOIN (
                SELECT da."ListingId", COUNT(*) AS deal_count
                FROM deal_applications da
                INNER JOIN billing_accounts ba ON ba."DealId" = da."DealId"
                WHERE da."IsDeleted" = false AND ba."IsDeleted" = false
                GROUP BY da."ListingId"
            ) d ON d."ListingId" = l."Id"
            WHERE l."IsDeleted" = false
            ORDER BY COALESCE(a.app_count, 0) DESC
            """, conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var listingId = reader.GetGuid(0);
            var title = reader.GetString(1);
            var qualityScore = await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
                ? 0.0
                : reader.GetDouble(2);
            var appCount = reader.GetInt32(3);
            var dealCount = reader.GetInt32(4);
            var conversionPercent = appCount > 0 ? (double)dealCount / appCount * 100 : 0;

            items.Add(new ListingAnalyticsItemDto(
                listingId, title, 0, appCount, Math.Round(conversionPercent, 2), qualityScore));
        }

        return Result<IReadOnlyList<ListingAnalyticsItemDto>>.Success(items);
    }
}
