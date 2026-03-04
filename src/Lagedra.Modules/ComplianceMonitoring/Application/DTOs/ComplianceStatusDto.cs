namespace Lagedra.Modules.ComplianceMonitoring.Application.DTOs;

public sealed record ComplianceStatusDto(
    Guid DealId,
    int OpenViolations,
    int CuredViolations,
    int EscalatedViolations,
    int TotalSignals,
    int UnprocessedSignals,
    bool IsCompliant);
