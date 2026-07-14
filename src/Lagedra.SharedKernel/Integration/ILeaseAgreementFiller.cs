namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Fills a published lease template body with deal/host/tenant/listing values.
/// </summary>
public interface ILeaseAgreementFiller
{
    Task<FilledLeaseAgreement> FillForDealAsync(Guid dealId, CancellationToken ct = default);
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
