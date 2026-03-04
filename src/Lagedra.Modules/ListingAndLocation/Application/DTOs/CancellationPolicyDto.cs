using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record CancellationPolicyDto(
    CancellationPolicyType Type,
    int FreeCancellationDays,
    int? PartialRefundPercent,
    int? PartialRefundDays,
    string? CustomTerms);
