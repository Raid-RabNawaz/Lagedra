using Lagedra.Modules.ComplianceMonitoring.Application.DTOs;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ComplianceMonitoring.Application.Queries;

public sealed record ListViolationsQuery(
    Guid DealId) : IRequest<Result<IReadOnlyList<MonitoredViolationDto>>>;

public sealed class ListViolationsQueryHandler(
    ComplianceMonitoringDbContext dbContext)
    : IRequestHandler<ListViolationsQuery, Result<IReadOnlyList<MonitoredViolationDto>>>
{
    public async Task<Result<IReadOnlyList<MonitoredViolationDto>>> Handle(
        ListViolationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await dbContext.Violations
            .AsNoTracking()
            .Where(v => v.DealId == request.DealId)
            .OrderByDescending(v => v.DetectedAt)
            .Select(v => new MonitoredViolationDto(
                v.Id,
                v.DealId,
                v.Category,
                v.Status,
                v.DetectedAt,
                v.CureDeadline))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<MonitoredViolationDto>>.Success(violations);
    }
}
