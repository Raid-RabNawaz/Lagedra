namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns the published lease agreement template for a jurisdiction.
/// Implemented by LeaseAgreements; consumed by TruthSurface when sealing a deal.
/// </summary>
public interface ILeaseAgreementTemplateProvider
{
    Task<LeaseAgreementTemplateInfo?> GetActiveTemplateAsync(
        string jurisdictionCode,
        CancellationToken ct = default);
}

public sealed record LeaseAgreementTemplateInfo(
    Guid TemplateId,
    string JurisdictionCode,
    string Title,
    Guid ActiveVersionId,
    int VersionNumber,
    DateTime? EffectiveDate,
    string BodyHtml);
