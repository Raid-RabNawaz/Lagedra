using Lagedra.Modules.Privacy.Application.DTOs;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Application.Queries;

public sealed record ListActiveLegalHoldsQuery : IRequest<Result<IReadOnlyList<LegalHoldDto>>>;

public sealed class ListActiveLegalHoldsQueryHandler(PrivacyDbContext dbContext)
    : IRequestHandler<ListActiveLegalHoldsQuery, Result<IReadOnlyList<LegalHoldDto>>>
{
    public async Task<Result<IReadOnlyList<LegalHoldDto>>> Handle(
        ListActiveLegalHoldsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var holds = await dbContext.LegalHolds
            .AsNoTracking()
            .Where(h => h.ReleasedAt == null)
            .OrderByDescending(h => h.AppliedAt)
            .Select(h => new LegalHoldDto(h.Id, h.UserId, h.Reason, h.AppliedAt, h.ReleasedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<LegalHoldDto>>.Success(holds);
    }
}
