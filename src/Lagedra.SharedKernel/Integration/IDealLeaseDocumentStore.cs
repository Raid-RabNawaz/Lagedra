using System.Diagnostics.CodeAnalysis;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Persists and retrieves the lease document bound to a deal, whether it was
/// generated from a Lagedra template or supplied by the host.
/// </summary>
public interface IDealLeaseDocumentStore
{
    Task SaveAsync(DealLeaseDocument document, CancellationToken ct = default);

    Task<DealLeaseDocument?> GetByDealIdAsync(Guid dealId, CancellationToken ct = default);
}

/// <summary>
/// Where a deal's lease document came from. Recorded on the stored document so
/// a signed agreement's provenance stays explicit long after the listing that
/// produced it has changed.
/// </summary>
public enum DealLeaseDocumentSource
{
    LagedraTemplate,
    HostProvided
}

/// <summary>
/// Template identifiers are null for <see cref="DealLeaseDocumentSource.HostProvided"/>
/// documents, which have no Lagedra template behind them.
/// </summary>
[SuppressMessage(
    "Performance", "CA1819:Properties should not return arrays",
    Justification = "Lease PDF bytes are a fixed binary payload for email attachment and download.")]
public sealed record DealLeaseDocument(
    Guid DealId,
    Guid? SnapshotId,
    Guid? TemplateId,
    Guid? TemplateVersionId,
    string FileName,
    string ContentType,
    byte[] Content,
    string ContentHash,
    DateTime GeneratedAtUtc,
    DealLeaseDocumentSource Source = DealLeaseDocumentSource.LagedraTemplate);
