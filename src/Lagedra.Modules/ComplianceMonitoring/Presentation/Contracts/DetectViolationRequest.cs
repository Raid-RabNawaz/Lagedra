using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;

namespace Lagedra.Modules.ComplianceMonitoring.Presentation.Contracts;

public sealed record DetectViolationRequest(
    MonitoredViolationCategory Category,
    DateTime? CureDeadline);
