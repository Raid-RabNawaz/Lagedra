using System.Globalization;
using Lagedra.Modules.Analytics.Application.DTOs;
using Lagedra.SharedKernel.Results;
using MediatR;
using Npgsql;

namespace Lagedra.Modules.Analytics.Application.Queries;

public sealed record GetPlatformSummaryQuery(
    DateTime? StartDate,
    DateTime? EndDate) : IRequest<Result<PlatformSummaryDto>>;

public sealed class GetPlatformSummaryQueryHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<GetPlatformSummaryQuery, Result<PlatformSummaryDto>>
{
    public async Task<Result<PlatformSummaryDto>> Handle(
        GetPlatformSummaryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var start = request.StartDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = request.EndDate ?? DateTime.UtcNow;

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var totalListings = 0;
        await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM listings WHERE \"IsDeleted\" = false", conn))
        {
            totalListings = Convert.ToInt32(
                await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                CultureInfo.InvariantCulture);
        }

        var activeDeals = 0;
        await using (var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM billing_accounts WHERE \"Status\" = 1 AND \"IsDeleted\" = false", conn))
        {
            activeDeals = Convert.ToInt32(
                await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                CultureInfo.InvariantCulture);
        }

        long mrrCents = 0;
        await using (var cmd = new NpgsqlCommand(
            "SELECT COALESCE(SUM(7900), 0) FROM billing_accounts WHERE \"Status\" = 1 AND \"IsDeleted\" = false", conn))
        {
            mrrCents = Convert.ToInt64(
                await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                CultureInfo.InvariantCulture);
        }

        var totalApplications = 0;
        await using (var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM deal_applications WHERE \"IsDeleted\" = false", conn))
        {
            totalApplications = Convert.ToInt32(
                await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                CultureInfo.InvariantCulture);
        }

        var conversionRate = totalApplications > 0
            ? (double)activeDeals / totalApplications * 100
            : 0;

        return Result<PlatformSummaryDto>.Success(new PlatformSummaryDto(
            totalListings, activeDeals, mrrCents, Math.Round(conversionRate, 2), start, end));
    }
}
