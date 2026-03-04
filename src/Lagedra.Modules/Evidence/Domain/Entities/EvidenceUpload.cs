using Lagedra.Modules.Evidence.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.Entities;

public sealed class EvidenceUpload : Entity<Guid>
{
    public Guid ManifestId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public FileHash? FileHash { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }
    public string? TimestampMetadata { get; private set; }

    private EvidenceUpload() { }

    public static EvidenceUpload Create(
        Guid manifestId,
        string originalFileName,
        string storageKey,
        string mimeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        return new EvidenceUpload
        {
            Id = Guid.NewGuid(),
            ManifestId = manifestId,
            OriginalFileName = originalFileName,
            StorageKey = storageKey,
            MimeType = mimeType,
            UploadedAt = DateTime.UtcNow
        };
    }

    public void SetFileHash(FileHash hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        FileHash = hash;
    }

    public void SetTimestampMetadata(string? metadata)
    {
        TimestampMetadata = metadata;
    }
}
