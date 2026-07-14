using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public sealed class DealLeaseDocumentStore(LeaseAgreementDbContext db) : IDealLeaseDocumentStore
{
    public async Task SaveAsync(DealLeaseDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existing = await db.DealDocuments
            .FirstOrDefaultAsync(d => d.DealId == document.DealId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.DealDocuments.Add(DealLeaseDocumentEntity.Create(
                document.DealId,
                document.SnapshotId,
                document.TemplateId,
                document.TemplateVersionId,
                document.FileName,
                document.ContentType,
                document.Content,
                document.ContentHash,
                document.GeneratedAtUtc));
        }
        else
        {
            existing.ReplaceContent(
                document.SnapshotId,
                document.TemplateId,
                document.TemplateVersionId,
                document.FileName,
                document.ContentType,
                document.Content,
                document.ContentHash,
                document.GeneratedAtUtc);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<DealLeaseDocument?> GetByDealIdAsync(Guid dealId, CancellationToken ct = default)
    {
        var entity = await db.DealDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DealId == dealId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        return new DealLeaseDocument(
            entity.DealId,
            entity.SnapshotId,
            entity.TemplateId,
            entity.TemplateVersionId,
            entity.FileName,
            entity.ContentType,
            entity.Content,
            entity.ContentHash,
            entity.GeneratedAtUtc);
    }
}
