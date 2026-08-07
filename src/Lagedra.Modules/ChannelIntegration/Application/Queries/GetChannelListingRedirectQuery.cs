using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Queries;

/// <summary>
/// Resolves an external provider listing id to the imported Lagedra listing id,
/// backing the public channel deep-link / redirect endpoint.
/// </summary>
public sealed record GetChannelListingRedirectQuery(
    string ProviderKey,
    string ExternalListingId) : IRequest<Result<Guid>>;

public sealed class GetChannelListingRedirectQueryHandler(
    ChannelDbContext dbContext)
    : IRequestHandler<GetChannelListingRedirectQuery, Result<Guid>>
{
    private static readonly Error NotMapped = new(
        "Channel.ListingNotMapped",
        "No imported Lagedra listing is mapped to that channel listing.");

    public async Task<Result<Guid>> Handle(
        GetChannelListingRedirectQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var providerKey = request.ProviderKey.Trim();
        var externalListingId = request.ExternalListingId.Trim();

        var listingId = await (
            from map in dbContext.ListingMaps.AsNoTracking()
            join connection in dbContext.Connections.AsNoTracking()
                on map.ConnectionId equals connection.Id
            where connection.ProviderKey == providerKey
               && connection.Status != ChannelConnectionStatus.Revoked
               && map.ProviderListingId == externalListingId
               && map.ListingId != null
            select map.ListingId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return listingId is null
            ? Result<Guid>.Failure(NotMapped)
            : Result<Guid>.Success(listingId.Value);
    }
}
