using Lagedra.Modules.ListingAndLocation.Application.DTOs;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.Ai;

/// <summary>
/// Optionally enriches a best-effort import draft using an LLM, filling fields
/// that public Open Graph/JSON-LD metadata could not provide (common for
/// JavaScript-rendered listing pages). Enrichment is strictly best-effort: it
/// only fills gaps, never overwrites values the structured extractor already
/// found, and any failure returns the original draft unchanged. The result is
/// still a suggestion the host reviews — nothing is persisted here.
/// </summary>
public interface IListingDraftAiEnricher
{
    Task<ImportedListingDraftDto> EnrichAsync(
        ImportedListingDraftDto draft,
        string html,
        Uri finalUrl,
        CancellationToken cancellationToken = default);
}
