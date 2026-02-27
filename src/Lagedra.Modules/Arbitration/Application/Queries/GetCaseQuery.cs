using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Queries;

public sealed record GetCaseQuery(Guid CaseId) : IRequest<Result<CaseDto>>;

public sealed class GetCaseQueryHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<GetCaseQuery, Result<CaseDto>>
{
    public async Task<Result<CaseDto>> Handle(GetCaseQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arbitrationCase = await dbContext.ArbitrationCases
            .AsNoTracking()
            .Include(c => c.EvidenceSlots)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result<CaseDto>.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        return Result<CaseDto>.Success(MapToDto(arbitrationCase));
    }

    internal static CaseDto MapToDto(ArbitrationCase c) =>
        new(c.Id, c.DealId, c.FiledByUserId, c.Tier, c.Category, c.Status,
            c.FilingFeeCents, c.FiledAt, c.EvidenceCompleteAt, c.DecisionDueAt,
            c.EvidenceSlots.Count,
            c.DecidedAt.HasValue
                ? new DecisionDto(c.DecisionSummary!, c.AwardAmount, c.DecidedAt.Value)
                : null);
}
