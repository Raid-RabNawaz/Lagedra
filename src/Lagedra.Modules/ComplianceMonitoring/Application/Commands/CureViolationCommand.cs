using Lagedra.Modules.ComplianceMonitoring.Application.DTOs;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ComplianceMonitoring.Application.Commands;

public sealed record CureViolationCommand(
    Guid DealId,
    Guid ViolationId) : IRequest<Result<MonitoredViolationDto>>;

public sealed class CureViolationCommandHandler(
    ComplianceMonitoringDbContext dbContext)
    : IRequestHandler<CureViolationCommand, Result<MonitoredViolationDto>>
{
    public async Task<Result<MonitoredViolationDto>> Handle(
        CureViolationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violation = await dbContext.Violations
            .FirstOrDefaultAsync(
                v => v.Id == request.ViolationId && v.DealId == request.DealId,
                cancellationToken)
            .ConfigureAwait(false);

        if (violation is null)
        {
            return Result<MonitoredViolationDto>.Failure(
                new Error("ComplianceMonitoring.ViolationNotFound",
                    "Violation not found for this deal."));
        }

        try
        {
            violation.Cure();
        }
        catch (InvalidOperationException ex)
        {
            return Result<MonitoredViolationDto>.Failure(
                new Error("ComplianceMonitoring.InvalidOperation", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MonitoredViolationDto>.Success(new MonitoredViolationDto(
            violation.Id,
            violation.DealId,
            violation.Category,
            violation.Status,
            violation.DetectedAt,
            violation.CureDeadline));
    }
}
