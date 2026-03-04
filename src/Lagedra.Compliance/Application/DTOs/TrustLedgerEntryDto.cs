using Lagedra.Compliance.Domain;

namespace Lagedra.Compliance.Application.DTOs;

public sealed record TrustLedgerEntryDto(
    Guid Id,
    Guid UserId,
    TrustLedgerEntryType EntryType,
    Guid? ReferenceId,
    string? Description,
    DateTime OccurredAt,
    bool IsPublic);
