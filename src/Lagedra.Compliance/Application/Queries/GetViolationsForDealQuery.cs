using Lagedra.Compliance.Application.DTOs;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Queries;

public sealed record GetViolationsForDealQuery(Guid DealId) : IRequest<Result<IReadOnlyList<ViolationDto>>>;

public sealed class GetViolationsForDealQueryHandler(ComplianceDbContext dbContext)
    : IRequestHandler<GetViolationsForDealQuery, Result<IReadOnlyList<ViolationDto>>>
{
    public async Task<Result<IReadOnlyList<ViolationDto>>> Handle(
        GetViolationsForDealQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await dbContext.Violations
            .AsNoTracking()
            .Where(v => v.DealId == request.DealId)
            .OrderByDescending(v => v.DetectedAt)
            .Select(v => new ViolationDto(
                v.Id, v.DealId, v.ReportedByUserId, v.TargetUserId, v.Category, v.Status,
                v.Description, v.EvidenceReference, v.DetectedAt, v.ResolvedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ViolationDto>>.Success(violations);
    }
}
