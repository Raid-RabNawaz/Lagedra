using System.Diagnostics.CodeAnalysis;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.LeaseAgreements.Domain.Entities;

public sealed class DealLeaseDocumentEntity : Entity<Guid>
{
    public Guid DealId { get; private set; }
    public Guid? SnapshotId { get; private set; }

    /// <summary>
    /// Null when <see cref="Source"/> is
    /// <see cref="DealLeaseDocumentSource.HostProvided"/> — a host upload has no
    /// Lagedra template version behind it.
    /// </summary>
    public Guid? TemplateId { get; private set; }
    public Guid? TemplateVersionId { get; private set; }
    public DealLeaseDocumentSource Source { get; private set; } = DealLeaseDocumentSource.LagedraTemplate;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/pdf";

    [SuppressMessage(
        "Performance", "CA1819:Properties should not return arrays",
        Justification = "EF Core maps PDF content as bytea.")]
    public byte[] Content { get; private set; } = [];

    public string ContentHash { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }

    private DealLeaseDocumentEntity() { }

    public static DealLeaseDocumentEntity Create(
        Guid dealId,
        Guid? snapshotId,
        Guid? templateId,
        Guid? templateVersionId,
        string fileName,
        string contentType,
        byte[] content,
        string contentHash,
        DateTime generatedAtUtc,
        DealLeaseDocumentSource source = DealLeaseDocumentSource.LagedraTemplate)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        return new DealLeaseDocumentEntity
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            SnapshotId = snapshotId,
            TemplateId = templateId,
            TemplateVersionId = templateVersionId,
            Source = source,
            FileName = fileName,
            ContentType = contentType,
            Content = content,
            ContentHash = contentHash,
            GeneratedAtUtc = generatedAtUtc
        };
    }

    public void ReplaceContent(
        Guid? snapshotId,
        Guid? templateId,
        Guid? templateVersionId,
        string fileName,
        string contentType,
        byte[] content,
        string contentHash,
        DateTime generatedAtUtc,
        DealLeaseDocumentSource source = DealLeaseDocumentSource.LagedraTemplate)
    {
        ArgumentNullException.ThrowIfNull(content);
        SnapshotId = snapshotId;
        TemplateId = templateId;
        TemplateVersionId = templateVersionId;
        Source = source;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
        ContentHash = contentHash;
        GeneratedAtUtc = generatedAtUtc;
    }
}
