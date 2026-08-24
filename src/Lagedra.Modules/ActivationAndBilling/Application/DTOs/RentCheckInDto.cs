using Lagedra.Modules.ActivationAndBilling.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record RentCheckInDto(
    Guid Id,
    Guid DealId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    RentCheckInStatus Status,
    DateTime? RespondedAt,
    string? Note);
