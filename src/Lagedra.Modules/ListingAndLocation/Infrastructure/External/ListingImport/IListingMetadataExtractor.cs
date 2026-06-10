using Lagedra.Modules.ListingAndLocation.Application.DTOs;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;

/// <summary>
/// Transforms raw listing HTML into a best-effort <see cref="ImportedListingDraftDto"/>.
/// Implementations are pure (no network access) so they are easy to unit test
/// against fixture HTML.
/// </summary>
public interface IListingMetadataExtractor
{
    ImportedListingDraftDto Extract(string html, Uri finalUrl);
}
