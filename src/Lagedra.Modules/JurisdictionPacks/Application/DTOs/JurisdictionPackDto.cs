using Lagedra.Modules.JurisdictionPacks.Domain.Enums;

namespace Lagedra.Modules.JurisdictionPacks.Application.DTOs;

public sealed record JurisdictionPackDto(
    Guid PackId,
    string JurisdictionCode,
    Guid? ActiveVersionId,
    IReadOnlyList<PackVersionSummaryDto> Versions);

public sealed record PackVersionSummaryDto(
    Guid VersionId,
    int VersionNumber,
    PackVersionStatus Status,
    DateTime? EffectiveDate,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    Guid? SecondApproverId);
