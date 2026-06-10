using Lagedra.Modules.JurisdictionPacks.Application.DTOs;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.JurisdictionPacks.Application.Queries;

public sealed record JurisdictionPackSummaryDto(
    Guid PackId,
    string JurisdictionCode,
    Guid? ActiveVersionId,
    int VersionCount);

public sealed record ListJurisdictionPacksQuery : IRequest<Result<IReadOnlyList<JurisdictionPackSummaryDto>>>;

public sealed partial class ListJurisdictionPacksQueryHandler(
    JurisdictionDbContext dbContext,
    ILogger<ListJurisdictionPacksQueryHandler> logger)
    : IRequestHandler<ListJurisdictionPacksQuery, Result<IReadOnlyList<JurisdictionPackSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<JurisdictionPackSummaryDto>>> Handle(
        ListJurisdictionPacksQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var packs = await dbContext.JurisdictionPacks
                .AsNoTracking()
                .OrderBy(p => p.JurisdictionCode.Code)
                .Select(p => new JurisdictionPackSummaryDto(
                    p.Id,
                    p.JurisdictionCode.Code,
                    p.ActiveVersionId,
                    p.Versions.Count))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result<IReadOnlyList<JurisdictionPackSummaryDto>>.Success(packs);
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException)
        {
            LogQueryFailed(logger, ex.Message, ex);
            return Result<IReadOnlyList<JurisdictionPackSummaryDto>>.Failure(
                new Error("JurisdictionPack.QueryFailed", ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to list jurisdiction packs: {ErrorMessage}")]
    private static partial void LogQueryFailed(ILogger logger, string errorMessage, Exception ex);
}
