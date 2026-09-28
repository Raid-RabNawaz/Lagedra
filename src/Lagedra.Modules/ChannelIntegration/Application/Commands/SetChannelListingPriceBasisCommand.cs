using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Host says whether the stored PMS amount is monthly or nightly. The choice
/// is kept on the listing map and the listing rent is updated immediately.
/// Later syncs reuse the same choice.
/// </summary>
public sealed record SetChannelListingPriceBasisCommand(
    Guid ConnectionId,
    Guid MapId,
    Guid HostUserId,
    ChannelPmsPriceBasis PriceBasis) : IRequest<Result<ChannelListingMapDto>>;

public sealed class SetChannelListingPriceBasisCommandHandler(
    ChannelDbContext dbContext,
    IListingImporter listingImporter,
    IClock clock) : IRequestHandler<SetChannelListingPriceBasisCommand, Result<ChannelListingMapDto>>
{
    private static readonly Error NotFound = new(
        "Channel.NotFound",
        "Channel connection not found.");

    private static readonly Error MapNotFound = new(
        "Channel.ListingMapNotFound",
        "Imported listing not found on this connection.");

    private static readonly Error MissingAmount = new(
        "Channel.PriceBasis.MissingAmount",
        "Sync this listing again before choosing monthly or nightly.");

    public async Task<Result<ChannelListingMapDto>> Handle(
        SetChannelListingPriceBasisCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ownsConnection = await dbContext.Connections
            .AsNoTracking()
            .AnyAsync(
                c => c.Id == request.ConnectionId
                  && c.HostUserId == request.HostUserId
                  && c.Status != ChannelConnectionStatus.Revoked,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ownsConnection)
        {
            return Result<ChannelListingMapDto>.Failure(NotFound);
        }

        var map = await dbContext.ListingMaps
            .FirstOrDefaultAsync(
                m => m.Id == request.MapId && m.ConnectionId == request.ConnectionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (map is null)
        {
            return Result<ChannelListingMapDto>.Failure(MapNotFound);
        }

        if (map.PmsSourceAmountCents is not > 0 || map.ListingId is not Guid listingId)
        {
            return Result<ChannelListingMapDto>.Failure(MissingAmount);
        }

        var explicitBasis = request.PriceBasis == ChannelPmsPriceBasis.Monthly
            ? ChannelPriceBasis.Monthly
            : ChannelPriceBasis.Nightly;
        var rate = ChannelRateNormalizer.Normalize(map.PmsSourceAmountCents, explicitBasis);
        if (rate.MonthlyRentCents is not > 0)
        {
            return Result<ChannelListingMapDto>.Failure(MissingAmount);
        }

        map.SetPmsPriceBasis(request.PriceBasis, clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await listingImporter
            .TryApplyChannelMonthlyRentAsync(listingId, rate.MonthlyRentCents.Value, cancellationToken)
            .ConfigureAwait(false);

        return Result<ChannelListingMapDto>.Success(ChannelListingMapMapper.ToDto(map));
    }
}
