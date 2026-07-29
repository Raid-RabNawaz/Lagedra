namespace Lagedra.Modules.LeaseAgreements.Presentation.Contracts;

public sealed record CreateLeaseTemplateRequest(string JurisdictionCode, string Title);

public sealed record UpdateLeaseTemplateDraftRequest(
    string BodyHtml,
    DateTime? EffectiveDate,
    string? Title);
