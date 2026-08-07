using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>An encrypted OAuth token set as issued by the provider.</summary>
public sealed record ChannelOAuthTokens(
    string EncryptedAccessToken,
    string? EncryptedRefreshToken,
    DateTime? ExpiresAt);

/// <summary>
/// What to link: the host, the provider, and the account plus credentials to
/// store. Either <paramref name="EncryptedSecret"/> (credential-based providers)
/// or <paramref name="Tokens"/> (OAuth providers) carries the secret.
/// </summary>
public sealed record LinkChannelRequest(
    Guid HostUserId,
    string ProviderKey,
    string ExternalAccountId,
    string DisplayName,
    string? Username = null,
    string? EncryptedSecret = null,
    ChannelOAuthTokens? Tokens = null);

/// <summary>
/// Applies the "one connection per provider per host" rule and produces the
/// <see cref="ChannelConnection"/> to persist. Shared by every way a host can
/// link an account — pasted credentials and OAuth callbacks alike — so the
/// invariant and the reconnect behaviour cannot drift between them.
///
/// Does not call <c>SaveChangesAsync</c>: the caller owns the unit of work.
/// </summary>
public sealed class ChannelConnectionLinker(ChannelDbContext dbContext, IClock clock)
{
    public static Error AlreadyConnected(string displayName) => new(
        "Channel.ProviderAlreadyConnected",
        $"You already have a connection to this PMS (\"{displayName}\"). Sync it to update your "
        + "listings, or disconnect it first to connect a different account.");

    public async Task<Result<ChannelConnection>> LinkAsync(
        LinkChannelRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var providerKey = request.ProviderKey;
        var externalAccountId = request.ExternalAccountId.Trim();

        var existing = await dbContext.Connections
            .Where(c => c.HostUserId == request.HostUserId && c.ProviderKey == providerKey)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (existing.Find(c => c.Status != ChannelConnectionStatus.Revoked) is { } live)
        {
            return Result<ChannelConnection>.Failure(AlreadyConnected(live.DisplayName));
        }

        var encryptedSecret = request.Tokens?.EncryptedAccessToken ?? request.EncryptedSecret;

        // Reuse a previously disconnected row for this provider so its listing
        // mappings survive: reconnecting then updates the drafts already imported
        // instead of creating a second copy of every property. The row for the
        // same account is the one worth reviving, since only its mappings still
        // describe reachable properties.
        var sameAccount = existing.Find(c => c.Status == ChannelConnectionStatus.Revoked
            && string.Equals(c.ExternalAccountId, externalAccountId, StringComparison.OrdinalIgnoreCase));
        var connection = sameAccount ?? existing.Find(c => c.Status == ChannelConnectionStatus.Revoked);

        if (connection is not null)
        {
            connection.Relink(
                externalAccountId, request.DisplayName, request.Username, encryptedSecret, clock);

            if (sameAccount is null)
            {
                // The mappings describe properties on the previous account, so
                // they can never match this one — drop them and let the next sync
                // import the new account's properties from scratch.
                await DropListingMapsAsync(connection.Id, ct).ConfigureAwait(false);
            }
        }
        else
        {
            connection = ChannelConnection.Create(
                request.HostUserId,
                providerKey,
                externalAccountId,
                request.DisplayName,
                request.Username,
                encryptedSecret,
                clock);

            dbContext.Connections.Add(connection);
        }

        if (request.Tokens is { } tokens)
        {
            connection.StoreOAuthTokens(
                tokens.EncryptedAccessToken, tokens.EncryptedRefreshToken, tokens.ExpiresAt, clock);
        }

        return Result<ChannelConnection>.Success(connection);
    }

    private async Task DropListingMapsAsync(Guid connectionId, CancellationToken ct)
    {
        var maps = await dbContext.ListingMaps
            .Where(m => m.ConnectionId == connectionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        dbContext.ListingMaps.RemoveRange(maps);
    }
}
