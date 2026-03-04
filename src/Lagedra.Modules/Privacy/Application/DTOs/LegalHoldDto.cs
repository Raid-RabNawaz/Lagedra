namespace Lagedra.Modules.Privacy.Application.DTOs;

public sealed record LegalHoldDto(
    Guid Id,
    Guid UserId,
    string Reason,
    DateTime AppliedAt,
    DateTime? ReleasedAt);
