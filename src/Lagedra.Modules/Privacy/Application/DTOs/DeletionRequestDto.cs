using Lagedra.Modules.Privacy.Domain.Enums;

namespace Lagedra.Modules.Privacy.Application.DTOs;

public sealed record DeletionRequestDto(
    Guid Id,
    Guid UserId,
    DeletionStatus Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    string? BlockingReason);
