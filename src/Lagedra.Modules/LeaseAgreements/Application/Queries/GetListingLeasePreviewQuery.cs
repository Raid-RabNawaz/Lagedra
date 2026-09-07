using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.LeaseAgreements.Application.Queries;

/// <summary>
/// The lease a prospective tenant can read before requesting a booking: either
/// the host's own uploaded document, or a blank specimen of Lagedra's template
/// for the listing's jurisdiction with the listing's terms filled in.
/// </summary>
public sealed record GetListingLeasePreviewQuery(Guid ListingId)
    : IRequest<Result<ListingLeasePreview>>;

[SuppressMessage(
    "Performance", "CA1819:Properties should not return arrays",
    Justification = "Preview bytes are a fixed binary payload streamed straight to the response.")]
public sealed record ListingLeasePreview(
    string FileName,
    string ContentType,
    byte[] Content,
    bool IsHostProvided);

public sealed class GetListingLeasePreviewQueryHandler(
    IListingProvider listingProvider,
    ILeaseAgreementFiller filler,
    ILeasePdfGenerator pdfGenerator,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    IMemoryCache cache) : IRequestHandler<GetListingLeasePreviewQuery, Result<ListingLeasePreview>>
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);
    private readonly string _leaseDocumentsBucket = storageOptions.Value.LeaseDocumentsBucket;

    public async Task<Result<ListingLeasePreview>> Handle(
        GetListingLeasePreviewQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await listingProvider
            .GetListingDetailsAsync(request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Failure("Listing.NotFound", "Listing not found.");
        }

        var usesHostDocument =
            string.Equals(listing.LeaseAgreementSource, "HostProvided", StringComparison.Ordinal)
            && listing.CustomLeaseDocument is not null;

        return usesHostDocument
            ? await ReadHostDocumentAsync(listing.CustomLeaseDocument!, cancellationToken).ConfigureAwait(false)
            : await BuildSpecimenAsync(request.ListingId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<ListingLeasePreview>> ReadHostDocumentAsync(
        ListingCustomLeaseDocumentDto document,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = await storageService
                .GetObjectStreamAsync(_leaseDocumentsBucket, document.StorageKey, cancellationToken)
                .ConfigureAwait(false);

            await using (source.ConfigureAwait(false))
            {
                using var buffer = new MemoryStream();
                await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

                return Result<ListingLeasePreview>.Success(new ListingLeasePreview(
                    document.FileName,
                    document.ContentType,
                    buffer.ToArray(),
                    IsHostProvided: true));
            }
        }
        catch (InvalidOperationException ex)
        {
            return Failure(
                "LeaseAgreement.PreviewUnavailable",
                $"The host's lease agreement could not be read: {ex.Message}");
        }
    }

    private async Task<Result<ListingLeasePreview>> BuildSpecimenAsync(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        FilledLeaseAgreement filled;
        try
        {
            filled = await filler.FillPreviewForListingAsync(listingId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Failure("LeaseAgreement.PreviewUnavailable", ex.Message);
        }

        // Filling is cheap; rendering is not. Keying the cache on the filled
        // HTML means a host editing their lease terms invalidates it for free,
        // with no dependency on a listing timestamp.
        var cacheKey = "lease-preview:" + Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(filled.FilledHtml)));

        var pdf = cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;
            return pdfGenerator.Generate(filled.Title, filled.FilledHtml);
        })!;

        return Result<ListingLeasePreview>.Success(new ListingLeasePreview(
            $"lease-preview-{filled.JurisdictionCode}-{listingId:N}.pdf",
            "application/pdf",
            pdf,
            IsHostProvided: false));
    }

    private static Result<ListingLeasePreview> Failure(string code, string description) =>
        Result<ListingLeasePreview>.Failure(new Error(code, description));
}
