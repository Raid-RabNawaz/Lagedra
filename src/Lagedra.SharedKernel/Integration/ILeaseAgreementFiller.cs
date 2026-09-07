namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Fills a published lease template body with deal/host/tenant/listing values.
/// </summary>
public interface ILeaseAgreementFiller
{
    Task<FilledLeaseAgreement> FillForDealAsync(Guid dealId, CancellationToken ct = default);

    /// <summary>
    /// Builds a blank specimen of the listing's lease for a prospective tenant
    /// to read before booking. The listing's own terms are filled in; party
    /// names, dates and the street address are left as fill-in rules, matching
    /// what the public listing page already discloses.
    /// </summary>
    Task<FilledLeaseAgreement> FillPreviewForListingAsync(Guid listingId, CancellationToken ct = default);
}

public sealed record FilledLeaseAgreement(
    Guid TemplateId,
    Guid TemplateVersionId,
    int VersionNumber,
    string JurisdictionCode,
    string Title,
    string FilledHtml,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<string> MissingRequiredPlaceholders);
