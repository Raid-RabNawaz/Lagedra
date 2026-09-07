using System.Security.Cryptography;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record GenerateDealLeasePdfCommand(Guid DealId, Guid? SnapshotId = null)
    : IRequest<Result<DealLeaseDocument>>;

public sealed class GenerateDealLeasePdfCommandHandler(
    ILeaseAgreementFiller filler,
    ILeasePdfGenerator pdfGenerator,
    IDealLeaseDocumentStore documentStore,
    IDealApplicationStatusProvider dealProvider,
    IListingProvider listingProvider,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    IClock clock) : IRequestHandler<GenerateDealLeasePdfCommand, Result<DealLeaseDocument>>
{
    private readonly string _leaseDocumentsBucket = storageOptions.Value.LeaseDocumentsBucket;

    public async Task<Result<DealLeaseDocument>> Handle(
        GenerateDealLeasePdfCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await documentStore.GetByDealIdAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        var hostDocument = await ResolveHostDocumentAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (hostDocument is not null)
        {
            return await AttachHostDocumentAsync(request, existing, hostDocument, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            var filled = await filler.FillForDealAsync(request.DealId, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null && existing.TemplateVersionId == filled.TemplateVersionId)
            {
                return Result<DealLeaseDocument>.Success(existing);
            }

            if (filled.MissingRequiredPlaceholders.Count > 0)
            {
                return Result<DealLeaseDocument>.Failure(new Error(
                    "LeaseTemplate.MissingFields",
                    FormatMissingFieldsMessage(filled.MissingRequiredPlaceholders)));
            }

            var pdf = pdfGenerator.Generate(filled.Title, filled.FilledHtml);
            var hash = Convert.ToHexString(SHA256.HashData(pdf));
            var doc = new DealLeaseDocument(
                request.DealId,
                request.SnapshotId,
                filled.TemplateId,
                filled.TemplateVersionId,
                $"lease-{filled.JurisdictionCode}-deal-{request.DealId:N}.pdf",
                "application/pdf",
                pdf,
                hash,
                clock.UtcNow);

            await documentStore.SaveAsync(doc, cancellationToken).ConfigureAwait(false);
            return Result<DealLeaseDocument>.Success(doc);
        }
        catch (InvalidOperationException ex)
        {
            return Result<DealLeaseDocument>.Failure(new Error("LeaseTemplate.FillFailed", ex.Message));
        }
    }

    /// <summary>
    /// Returns the host's own lease for the deal's listing, or null when the
    /// listing uses Lagedra's template.
    /// </summary>
    private async Task<ListingCustomLeaseDocumentDto?> ResolveHostDocumentAsync(
        Guid dealId,
        CancellationToken cancellationToken)
    {
        var deal = await dealProvider.GetDealDetailsAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (deal is null)
        {
            return null;
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(deal.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (!string.Equals(listing?.LeaseAgreementSource, "HostProvided", StringComparison.Ordinal))
        {
            return null;
        }

        return listing!.CustomLeaseDocument;
    }

    /// <summary>
    /// Copies the host's uploaded file into the deal's lease document. The bytes
    /// are stored rather than referenced so that editing or deleting the
    /// listing's lease later cannot change an agreement already bound to a deal.
    /// </summary>
    private async Task<Result<DealLeaseDocument>> AttachHostDocumentAsync(
        GenerateDealLeasePdfCommand request,
        DealLeaseDocument? existing,
        ListingCustomLeaseDocumentDto hostDocument,
        CancellationToken cancellationToken)
    {
        // Host documents have no template version to compare, so identity is the
        // content hash instead.
        if (existing is not null
            && existing.Source == DealLeaseDocumentSource.HostProvided
            && string.Equals(existing.ContentHash, hostDocument.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            return Result<DealLeaseDocument>.Success(existing);
        }

        byte[] content;
        try
        {
            var source = await storageService
                .GetObjectStreamAsync(_leaseDocumentsBucket, hostDocument.StorageKey, cancellationToken)
                .ConfigureAwait(false);

            await using (source.ConfigureAwait(false))
            {
                using var buffer = new MemoryStream();
                await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                content = buffer.ToArray();
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result<DealLeaseDocument>.Failure(new Error(
                "LeaseAgreement.HostDocumentUnavailable",
                $"The host's lease agreement could not be read: {ex.Message}"));
        }

        if (content.Length == 0)
        {
            return Result<DealLeaseDocument>.Failure(new Error(
                "LeaseAgreement.HostDocumentUnavailable",
                "The host's lease agreement could not be read."));
        }

        var doc = new DealLeaseDocument(
            request.DealId,
            request.SnapshotId,
            TemplateId: null,
            TemplateVersionId: null,
            hostDocument.FileName,
            hostDocument.ContentType,
            content,
            Convert.ToHexString(SHA256.HashData(content)),
            clock.UtcNow,
            DealLeaseDocumentSource.HostProvided);

        await documentStore.SaveAsync(doc, cancellationToken).ConfigureAwait(false);
        return Result<DealLeaseDocument>.Success(doc);
    }

    private static string FormatMissingFieldsMessage(IReadOnlyList<string> missingKeys)
    {
        var labels = missingKeys
            .Select(key =>
            {
                var def = LeasePlaceholderCatalog.All
                    .FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
                return def is null ? key : def.Label;
            })
            .ToList();

        var list = string.Join(", ", labels);
        if (missingKeys.Any(k => string.Equals(k, "listing.fullAddress", StringComparison.OrdinalIgnoreCase)))
        {
            return "Cannot generate the lease PDF because the listing has no locked property address. "
                + "Open the listing, lock the full street address, then try downloading again. "
                + $"Missing: {list}.";
        }

        return $"Cannot generate the lease PDF — missing required information: {list}.";
    }
}
