using Lagedra.Modules.LeaseAgreements.Domain.Enums;

namespace Lagedra.Modules.LeaseAgreements.Application.DTOs;

public sealed record LeaseTemplateSummaryDto(
    Guid TemplateId,
    string JurisdictionCode,
    string Title,
    Guid? ActiveVersionId,
    int VersionCount);

public sealed record LeaseTemplateVersionSummaryDto(
    Guid TemplateId,
    string JurisdictionCode,
    Guid VersionId,
    int VersionNumber,
    LeaseTemplateVersionStatus Status,
    DateTime? EffectiveDate,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    Guid? SecondApproverId);

public sealed record LeaseTemplateVersionDetailsDto(
    Guid TemplateId,
    string JurisdictionCode,
    string Title,
    Guid VersionId,
    int VersionNumber,
    LeaseTemplateVersionStatus Status,
    DateTime? EffectiveDate,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    Guid? SecondApproverId,
    string BodyHtml);

public sealed record LeaseAgreementTemplateDto(
    Guid TemplateId,
    string JurisdictionCode,
    string Title,
    Guid? ActiveVersionId,
    IReadOnlyList<LeaseTemplateVersionSummaryDto> Versions);

public sealed record LeasePlaceholderDto(
    string Key,
    string Group,
    string Label,
    string Description,
    string Example,
    bool Required,
    string Token);

public sealed record LeasePlaceholderCatalogDto(
    IReadOnlyList<LeasePlaceholderDto> Placeholders,
    string UsageExampleHtml,
    string UsageHint);

public sealed record PendingLeaseApprovalDto(
    Guid TemplateId,
    string JurisdictionCode,
    string Title,
    Guid VersionId,
    int VersionNumber,
    DateTime? EffectiveDate,
    Guid? FirstApproverId);
