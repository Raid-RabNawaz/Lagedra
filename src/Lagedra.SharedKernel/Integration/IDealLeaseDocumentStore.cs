using System.Diagnostics.CodeAnalysis;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Persists and retrieves the generated lease PDF for a deal.
/// </summary>
public interface IDealLeaseDocumentStore
{
    Task SaveAsync(DealLeaseDocument document, CancellationToken ct = default);

    Task<DealLeaseDocument?> GetByDealIdAsync(Guid dealId, CancellationToken ct = default);
}

[SuppressMessage(
    "Performance", "CA1819:Properties should not return arrays",
    Justification = "Lease PDF bytes are a fixed binary payload for email attachment and download.")]
public sealed record DealLeaseDocument(
    Guid DealId,
    Guid? SnapshotId,
    Guid TemplateId,
    Guid TemplateVersionId,
    string FileName,
    string ContentType,
    byte[] Content,
    string ContentHash,
    DateTime GeneratedAtUtc);
