using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;

namespace Lagedra.Modules.ComplianceMonitoring.Application.DTOs;

public sealed record MonitoredViolationDto(
    Guid ViolationId,
    Guid DealId,
    MonitoredViolationCategory Category,
    MonitoredViolationStatus Status,
    DateTime DetectedAt,
    DateTime? CureDeadline);
