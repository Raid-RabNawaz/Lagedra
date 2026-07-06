using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Queries;

/// <summary>
/// Lists the external listings pulled for one of the host's connections, with
/// their imported Lagedra listing id (when materialised).
/// </summary>
public sealed record ListChannelListingsQuery(
    Guid ConnectionId,
    Guid HostUserId) : IRequest<Result<IReadOnlyList<ChannelListingMapDto>>>;

public sealed class ListChannelListingsQueryHandler(
    ChannelDbContext dbContext)
    : IRequestHandler<ListChannelListingsQuery, Result<IReadOnlyList<ChannelListingMapDto>>>
{
    private static readonly Error NotFound = new(
        "Channel.NotFound",
        "Channel connection not found.");

    public async Task<Result<IReadOnlyList<ChannelListingMapDto>>> Handle(
        ListChannelListingsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ownsConnection = await dbContext.Connections
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ConnectionId && c.HostUserId == request.HostUserId, cancellationToken)
            .ConfigureAwait(false);

        if (!ownsConnection)
        {
            return Result<IReadOnlyList<ChannelListingMapDto>>.Failure(NotFound);
        }

        var maps = await dbContext.ListingMaps
            .AsNoTracking()
            .Where(m => m.ConnectionId == request.ConnectionId)
            .OrderByDescending(m => m.LastImportedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ChannelListingMapDto> dtos = maps
            .Select(ChannelListingMapMapper.ToDto)
            .ToList();

        return Result<IReadOnlyList<ChannelListingMapDto>>.Success(dtos);
    }
}
