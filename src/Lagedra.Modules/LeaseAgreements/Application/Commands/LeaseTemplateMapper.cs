using Lagedra.Modules.LeaseAgreements.Application.DTOs;
using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

internal static class LeaseTemplateMapper
{
    public static LeaseAgreementTemplateDto ToDto(LeaseAgreementTemplate template) =>
        new(
            template.Id,
            template.JurisdictionCode.Code,
            template.Title,
            template.ActiveVersionId,
            template.Versions.Select(v => ToSummary(template, v)).ToList());

    public static LeaseTemplateVersionSummaryDto ToSummary(
        LeaseAgreementTemplate template,
        LeaseTemplateVersion version) =>
        new(
            template.Id,
            template.JurisdictionCode.Code,
            version.Id,
            version.VersionNumber,
            version.Status,
            version.EffectiveDate,
            version.ApprovedAt,
            version.ApprovedBy,
            version.SecondApproverId);

    public static LeaseTemplateVersionDetailsDto ToDetails(
        LeaseAgreementTemplate template,
        LeaseTemplateVersion version) =>
        new(
            template.Id,
            template.JurisdictionCode.Code,
            template.Title,
            version.Id,
            version.VersionNumber,
            version.Status,
            version.EffectiveDate,
            version.ApprovedAt,
            version.ApprovedBy,
            version.SecondApproverId,
            version.BodyHtml);
}
