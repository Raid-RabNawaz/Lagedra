using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Queries;

public sealed record ListMyChannelConnectionsQuery(Guid HostUserId)
    : IRequest<Result<IReadOnlyList<ChannelConnectionDto>>>;

public sealed class ListMyChannelConnectionsQueryHandler(
    ChannelDbContext dbContext)
    : IRequestHandler<ListMyChannelConnectionsQuery, Result<IReadOnlyList<ChannelConnectionDto>>>
{
    public async Task<Result<IReadOnlyList<ChannelConnectionDto>>> Handle(
        ListMyChannelConnectionsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var connections = await dbContext.Connections
            .AsNoTracking()
            .Where(c => c.HostUserId == request.HostUserId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ChannelConnectionDto> dtos = connections
            .Select(ChannelConnectionMapper.ToDto)
            .ToList();

        return Result<IReadOnlyList<ChannelConnectionDto>>.Success(dtos);
    }
}
