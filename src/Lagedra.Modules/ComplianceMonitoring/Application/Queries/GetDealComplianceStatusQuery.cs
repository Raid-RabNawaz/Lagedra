using Lagedra.Modules.ComplianceMonitoring.Application.DTOs;
using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ComplianceMonitoring.Application.Queries;

public sealed record GetDealComplianceStatusQuery(
    Guid DealId) : IRequest<Result<ComplianceStatusDto>>;

public sealed class GetDealComplianceStatusQueryHandler(
    ComplianceMonitoringDbContext dbContext)
    : IRequestHandler<GetDealComplianceStatusQuery, Result<ComplianceStatusDto>>
{
    public async Task<Result<ComplianceStatusDto>> Handle(
        GetDealComplianceStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await dbContext.Violations
            .AsNoTracking()
            .Where(v => v.DealId == request.DealId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var signals = await dbContext.Signals
            .AsNoTracking()
            .Where(s => s.DealId == request.DealId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var open = violations.Count(v => v.Status == MonitoredViolationStatus.Open);
        var cured = violations.Count(v => v.Status == MonitoredViolationStatus.Cured);
        var escalated = violations.Count(v => v.Status == MonitoredViolationStatus.Escalated);
        var unprocessed = signals.Count(s => s.ProcessedAt is null);

        return Result<ComplianceStatusDto>.Success(new ComplianceStatusDto(
            request.DealId,
            open,
            cured,
            escalated,
            signals.Count,
            unprocessed,
            IsCompliant: open == 0 && escalated == 0));
    }
}
