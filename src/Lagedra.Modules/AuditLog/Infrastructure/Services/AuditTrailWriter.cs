using Lagedra.Modules.AuditLog.Domain.Entities;
using Lagedra.Modules.AuditLog.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.AuditLog.Infrastructure.Services;

/// <summary>
/// Default <see cref="IAuditTrailWriter"/> backed by <see cref="AuditDbContext"/>.
/// Resilient by design: any persistence failure (e.g. the audit schema not yet
/// migrated) is logged and swallowed so audit logging can never break the
/// booking/listing/payment flow it observes.
/// </summary>
public sealed partial class AuditTrailWriter(
    AuditDbContext dbContext,
    ILogger<AuditTrailWriter> logger)
    : IAuditTrailWriter
{
    public async Task RecordAsync(
        Guid? userId,
        string eventType,
        string entityType,
        string entityId,
        string? details = null,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        try
        {
            var auditEvent = AuditEvent.Create(
                userId, eventType, entityType, entityId, details, ipAddress);

            dbContext.AuditEvents.Add(auditEvent);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            LogAuditWriteFailed(logger, eventType, entityId, ex);
        }
        catch (InvalidOperationException ex)
        {
            LogAuditWriteFailed(logger, eventType, entityId, ex);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to write audit event {EventType} for entity {EntityId}; continuing.")]
    private static partial void LogAuditWriteFailed(
        ILogger logger, string eventType, string entityId, Exception exception);
}
