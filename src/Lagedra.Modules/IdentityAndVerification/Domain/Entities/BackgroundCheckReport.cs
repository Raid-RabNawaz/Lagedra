using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Entities;

public sealed class BackgroundCheckReport : Entity<Guid>
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7 * 365);

    public Guid UserId { get; private set; }
    public string? ExternalReportId { get; private set; }
    public BackgroundCheckResult Result { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private BackgroundCheckReport() { }

    public static BackgroundCheckReport Create(
        Guid userId,
        string? externalReportId,
        BackgroundCheckResult result)
    {
        var now = DateTime.UtcNow;
        var report = new BackgroundCheckReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExternalReportId = externalReportId,
            Result = result,
            ReceivedAt = now,
            ExpiresAt = now.Add(RetentionPeriod),
            CreatedAt = now
        };

        report._domainEvents.Add(new BackgroundCheckReceivedEvent(report.Id, userId, result));
        return report;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
