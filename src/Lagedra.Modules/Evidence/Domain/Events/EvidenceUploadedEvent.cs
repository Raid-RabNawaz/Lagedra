using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.Events;

public sealed record EvidenceUploadedEvent(
    Guid UploadId,
    Guid ManifestId,
    string OriginalFileName,
    string FileHash) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
