namespace Lagedra.Modules.JurisdictionPacks.Application.DTOs;

public sealed record EvidenceScheduleDto(
    Guid Id,
    string Category,
    string MinimumRequirements);
