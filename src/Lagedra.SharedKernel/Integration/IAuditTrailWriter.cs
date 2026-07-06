namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module hook for appending to the immutable audit trail without taking
/// a hard dependency on the AuditLog module. Implementations are resilient:
/// a failure to persist an audit entry is logged and swallowed so it can never
/// break the business operation it's recording.
/// </summary>
public interface IAuditTrailWriter
{
    /// <summary>
    /// Append an audit entry. Best-effort — never throws.
    /// </summary>
    /// <param name="userId">Actor user id, if known.</param>
    /// <param name="eventType">Dotted event name, e.g. "booking.approved".</param>
    /// <param name="entityType">The primary entity type, e.g. "Deal".</param>
    /// <param name="entityId">The primary entity id.</param>
    /// <param name="details">Optional JSON payload with extra context.</param>
    /// <param name="ipAddress">Optional originating IP for consent/approval events.</param>
    Task RecordAsync(
        Guid? userId,
        string eventType,
        string entityType,
        string entityId,
        string? details = null,
        string? ipAddress = null,
        CancellationToken ct = default);
}
