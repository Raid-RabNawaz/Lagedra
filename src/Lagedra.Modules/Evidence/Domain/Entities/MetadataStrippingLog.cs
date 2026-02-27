using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.Entities;

public sealed class MetadataStrippingLog : Entity<Guid>
{
    public Guid UploadId { get; private set; }
    public DateTime StrippedAt { get; private set; }
    public string RemovedFields { get; private set; } = string.Empty;

    private MetadataStrippingLog() { }

    public static MetadataStrippingLog Create(Guid uploadId, DateTime strippedAt, string removedFieldsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(removedFieldsJson);

        return new MetadataStrippingLog
        {
            Id = Guid.NewGuid(),
            UploadId = uploadId,
            StrippedAt = strippedAt,
            RemovedFields = removedFieldsJson
        };
    }
}
