using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ComplianceMonitoring.Application.Commands;

public sealed record CloseComplianceWindowCommand(
    Guid DealId) : IRequest<Result>;

public sealed class CloseComplianceWindowCommandHandler(
    ComplianceMonitoringDbContext dbContext)
    : IRequestHandler<CloseComplianceWindowCommand, Result>
{
    public async Task<Result> Handle(
        CloseComplianceWindowCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var openViolations = await dbContext.Violations
            .Where(v => v.DealId == request.DealId && v.Status == MonitoredViolationStatus.Open)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (openViolations.Count == 0)
        {
            return Result.Failure(new Error(
                "Lagedra.Modules.ComplianceMonitoring.NoOpenViolations",
                "No open violations found for this deal."));
        }

        foreach (var violation in openViolations)
        {
            violation.Escalate();
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
