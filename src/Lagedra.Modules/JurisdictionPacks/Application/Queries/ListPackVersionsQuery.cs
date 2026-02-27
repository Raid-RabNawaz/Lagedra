using Lagedra.Modules.JurisdictionPacks.Application.DTOs;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Application.Queries;

public sealed record ListPackVersionsQuery(Guid PackId) : IRequest<Result<IReadOnlyList<PackVersionSummaryDto>>>;

public sealed class ListPackVersionsQueryHandler(JurisdictionDbContext dbContext)
    : IRequestHandler<ListPackVersionsQuery, Result<IReadOnlyList<PackVersionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<PackVersionSummaryDto>>> Handle(ListPackVersionsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pack = await dbContext.JurisdictionPacks
            .AsNoTracking()
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == request.PackId, cancellationToken)
            .ConfigureAwait(false);

        if (pack is null)
        {
            return Result<IReadOnlyList<PackVersionSummaryDto>>.Failure(
                new Error("JurisdictionPack.NotFound", $"Pack '{request.PackId}' not found."));
        }

        var versions = pack.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new PackVersionSummaryDto(
                v.Id, v.VersionNumber, v.Status,
                v.EffectiveDate, v.ApprovedAt,
                v.ApprovedBy, v.SecondApproverId))
            .ToList();

        return Result<IReadOnlyList<PackVersionSummaryDto>>.Success(versions);
    }
}
