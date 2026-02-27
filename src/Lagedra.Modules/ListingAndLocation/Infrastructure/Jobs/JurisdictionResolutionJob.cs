using Lagedra.Modules.ListingAndLocation.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class JurisdictionResolutionJob(
    ListingRepository repository,
    ILogger<JurisdictionResolutionJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var listings = await repository
            .GetListingsWithoutJurisdictionAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (listings.Count == 0)
        {
            LogNoListingsPending(logger);
            return;
        }

        var resolved = 0;
        foreach (var listing in listings)
        {
            if (listing.PreciseAddress is null)
            {
                continue;
            }

            var code = ResolveJurisdiction(
                listing.PreciseAddress.State,
                listing.PreciseAddress.Country);

            if (code is not null)
            {
                listing.LockPreciseAddress(listing.PreciseAddress, code);
                resolved++;
            }
        }

        LogSweepComplete(logger, listings.Count, resolved);
    }

    private static string? ResolveJurisdiction(string state, string country)
    {
        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(country))
        {
            return null;
        }

        return $"{country.ToUpperInvariant()}-{state.ToUpperInvariant()}";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Jurisdiction resolution: no listings pending resolution")]
    private static partial void LogNoListingsPending(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Jurisdiction resolution sweep complete: {Total} checked, {Resolved} resolved")]
    private static partial void LogSweepComplete(ILogger logger, int total, int resolved);
}
