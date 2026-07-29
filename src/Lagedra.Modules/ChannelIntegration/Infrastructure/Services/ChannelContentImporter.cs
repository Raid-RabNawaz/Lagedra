using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// Outcome of a content sync for a single connection.
/// </summary>
public sealed record ChannelContentSyncResult(int Pulled, int Created, int Updated)
{
    public static ChannelContentSyncResult Empty { get; } = new(0, 0, 0);
}

/// <summary>
/// Pulls a connection's external listing content through its
/// <see cref="IChannelProvider"/> and materialises each one into a Lagedra
/// listing via <see cref="IListingImporter"/>, reconciling the
/// <see cref="ChannelListingMap"/> rows (ProviderListingId → ListingId) so the
/// import is idempotent across runs. Shared by the scheduled
/// <c>ChannelContentSyncJob</c> and the on-demand "sync now" command.
/// </summary>
public sealed partial class ChannelContentImporter(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    IEncryptionService encryption,
    IListingImporter listingImporter,
    IClock clock,
    ILogger<ChannelContentImporter> logger)
{
    public async Task<ChannelContentSyncResult> SyncAsync(ChannelConnection connection, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var provider = providers.Resolve(connection.ProviderKey);
        if (provider is null)
        {
            LogNoProvider(logger, connection.ProviderKey, connection.Id);
            return ChannelContentSyncResult.Empty;
        }

        IReadOnlyList<ChannelListingSnapshot> snapshots;
        try
        {
            snapshots = await provider
                .PullListingsAsync(connection.ToCredentials(encryption), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            LogPullFailed(logger, connection.Id, ex);
            connection.MarkError($"Content pull failed: {ex.Message}", clock);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return ChannelContentSyncResult.Empty;
        }

        var existingMaps = await dbContext.ListingMaps
            .Where(m => m.ConnectionId == connection.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var byExternalId = existingMaps
            .GroupBy(m => m.ProviderListingId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var updated = 0;
        foreach (var snapshot in snapshots)
        {
            byExternalId.TryGetValue(snapshot.ExternalListingId, out var map);

            var request = BuildImportRequest(connection.ProviderKey, connection.HostUserId, snapshot, map?.ListingId);

            ListingImportResult importResult;
            try
            {
                importResult = await listingImporter.ImportOrUpdateAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or DbUpdateException)
            {
                LogImportFailed(logger, connection.Id, snapshot.ExternalListingId, ex);
                continue;
            }

            if (map is null)
            {
                map = ChannelListingMap.Create(connection.Id, snapshot.ExternalListingId, snapshot.Title, clock);
                dbContext.ListingMaps.Add(map);
                byExternalId[snapshot.ExternalListingId] = map;
            }

            map.LinkLagedraListing(importResult.ListingId, snapshot.Title, clock);

            if (importResult.Created)
            {
                created++;
            }
            else
            {
                updated++;
            }

            // Persist the mapping immediately so a later failure in this loop
            // can never orphan an imported listing (which would re-import as a
            // duplicate on the next run).
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        connection.RecordContentSync(clock);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogSynced(logger, connection.Id, connection.ProviderKey, snapshots.Count, created, updated);
        return new ChannelContentSyncResult(snapshots.Count, created, updated);
    }

    private static ListingImportRequest BuildImportRequest(
        string providerKey,
        Guid hostUserId,
        ChannelListingSnapshot snapshot,
        Guid? existingListingId)
    {
        var monthlyRentCents = snapshot.MonthlyRentCents
            ?? (snapshot.NightlyRateCents.HasValue ? snapshot.NightlyRateCents.Value * 30 : 0);

        var address = snapshot.Address is null
            ? null
            : new ListingImportAddress(
                snapshot.Address.Line1,
                snapshot.Address.City,
                snapshot.Address.State,
                snapshot.Address.PostalCode,
                snapshot.Address.Country);

        var photos = snapshot.Photos?
            .Select(p => new ListingImportPhoto(p.ExternalId, p.Url, p.Caption))
            .ToList();

        return new ListingImportRequest(
            LandlordUserId: hostUserId,
            ExternalSource: providerKey,
            ExternalListingId: snapshot.ExternalListingId,
            ExistingListingId: existingListingId,
            Title: snapshot.Title,
            Description: snapshot.Description,
            MonthlyRentCents: monthlyRentCents,
            MaxDepositCents: snapshot.DepositCents ?? 0,
            Bedrooms: snapshot.Bedrooms ?? 0,
            Bathrooms: snapshot.Bathrooms ?? 1m,
            MinStayDays: snapshot.MinStayNights ?? 30,
            MaxStayDays: snapshot.MaxStayNights ?? 180,
            PropertyType: ParsePropertyType(snapshot.PropertyType),
            SquareFootage: snapshot.SquareFootage,
            Latitude: snapshot.Latitude,
            Longitude: snapshot.Longitude,
            Address: address,
            Photos: photos,
            AmenityNames: snapshot.AmenityCodes);
    }

    private static ListingImportPropertyType ParsePropertyType(string? type) => (type ?? string.Empty).ToUpperInvariant() switch
    {
        "APARTMENT" => ListingImportPropertyType.Apartment,
        "HOUSE" => ListingImportPropertyType.House,
        "CONDO" => ListingImportPropertyType.Condo,
        "TOWNHOUSE" => ListingImportPropertyType.Townhouse,
        "STUDIO" => ListingImportPropertyType.Studio,
        "LOFT" => ListingImportPropertyType.Loft,
        "VILLA" => ListingImportPropertyType.Villa,
        "COTTAGE" => ListingImportPropertyType.Cottage,
        "CABIN" => ListingImportPropertyType.Cabin,
        _ => ListingImportPropertyType.Other,
    };

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No channel provider registered for key '{ProviderKey}' (connection {ConnectionId}) — skipping")]
    private static partial void LogNoProvider(ILogger logger, string providerKey, Guid connectionId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Content pull failed for connection {ConnectionId}; marked in error")]
    private static partial void LogPullFailed(ILogger logger, Guid connectionId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Import failed for connection {ConnectionId} listing '{ExternalListingId}' — skipping")]
    private static partial void LogImportFailed(ILogger logger, Guid connectionId, string externalListingId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Content-synced connection {ConnectionId} ({ProviderKey}): pulled {Pulled}, created {Created}, updated {Updated}")]
    private static partial void LogSynced(ILogger logger, Guid connectionId, string providerKey, int pulled, int created, int updated);
}
