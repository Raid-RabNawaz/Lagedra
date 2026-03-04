using Lagedra.Modules.ComplianceMonitoring.Application.DTOs;
using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ComplianceMonitoring.Application.Commands;

public sealed record DetectViolationCommand(
    Guid DealId,
    MonitoredViolationCategory Category,
    DateTime? CureDeadline) : IRequest<Result<MonitoredViolationDto>>;

public sealed class DetectViolationCommandHandler(
    ComplianceMonitoringDbContext dbContext)
    : IRequestHandler<DetectViolationCommand, Result<MonitoredViolationDto>>
{
    public async Task<Result<MonitoredViolationDto>> Handle(
        DetectViolationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violation = MonitoredViolation.Create(
            request.DealId,
            request.Category,
            DateTime.UtcNow,
            request.CureDeadline);

        dbContext.Violations.Add(violation);
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
