using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.Events;

public sealed record EvidenceScannedEvent(
    Guid UploadId,
    string ScanStatus) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
