using Lagedra.SharedKernel.Results;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Queries;

public sealed record ArbitrationBacklogItemDto(
    Guid CaseId,
    Guid DealId,
    Guid? ArbitratorUserId,
    string? ArbitratorEmail,
    string Status,
    string Category,
    string Tier,
    DateTime FiledAt,
    DateTime? DecisionDueAt,
    bool IsOverdue);

public sealed record GetArbitrationBacklogQuery : IRequest<Result<IReadOnlyList<ArbitrationBacklogItemDto>>>;

public sealed class GetArbitrationBacklogQueryHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<GetArbitrationBacklogQuery, Result<IReadOnlyList<ArbitrationBacklogItemDto>>>
{
    public async Task<Result<IReadOnlyList<ArbitrationBacklogItemDto>>> Handle(
        GetArbitrationBacklogQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var utcNow = DateTime.UtcNow;

        var cases = await dbContext.ArbitrationCases
            .AsNoTracking()
            .Include(c => c.ArbitratorAssignments)
            .Where(c => c.Status != Domain.Enums.ArbitrationStatus.Closed
                     && c.Status != Domain.Enums.ArbitrationStatus.Decided)
            .OrderBy(c => c.DecisionDueAt ?? DateTime.MaxValue)
            .ThenBy(c => c.FiledAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = cases.Select(c =>
        {
            var latestAssignment = c.ArbitratorAssignments
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefault();

            return new ArbitrationBacklogItemDto(
                c.Id,
                c.DealId,
                latestAssignment?.ArbitratorUserId,
                null,
                c.Status.ToString(),
                c.Category.ToString(),
                c.Tier.ToString(),
                c.FiledAt,
                c.DecisionDueAt,
                c.DecisionDueAt.HasValue && c.DecisionDueAt.Value < utcNow);
        }).ToList();

        return Result<IReadOnlyList<ArbitrationBacklogItemDto>>.Success(items);
    }
}
