using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;

/// <summary>
/// A lease agreement uploaded by the host, held in the private lease-documents
/// bucket. Only the pointer and provenance live here; the bytes are copied into
/// the deal's lease document at confirmation time so a later edit or removal
/// cannot alter an agreement the parties already saw.
/// </summary>
public sealed class CustomLeaseDocument : ValueObject
{
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public DateTime UploadedAtUtc { get; private set; }

    private CustomLeaseDocument() { }

    public static CustomLeaseDocument Create(
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        string contentHash,
        DateTime uploadedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Document size must be positive.");
        }

        return new CustomLeaseDocument
        {
            StorageKey = Truncate(storageKey, 1000),
            FileName = Truncate(fileName, 300),
            ContentType = Truncate(contentType, 200),
            SizeBytes = sizeBytes,
            ContentHash = contentHash,
            UploadedAtUtc = uploadedAtUtc
        };
    }

    private static string Truncate(string value, int max) =>
        value.Length > max ? value[..max] : value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StorageKey;
        yield return FileName;
        yield return ContentType;
        yield return SizeBytes;
        yield return ContentHash;
        yield return UploadedAtUtc;
    }
}
