using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Entities;

/// <summary>
/// A document uploaded for manual KYC review (ID front/back, live selfie).
/// Stored in the private object-storage bucket; only platform admins get
/// short-lived presigned URLs to view it during review.
/// </summary>
public sealed class KycDocument : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public KycDocumentType DocumentType { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private KycDocument() { }

    public static KycDocument Create(
        Guid userId,
        KycDocumentType documentType,
        string storageKey,
        string fileName,
        string mimeType,
        long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);

        return new KycDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentType = documentType,
            StorageKey = storageKey,
            FileName = fileName,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
