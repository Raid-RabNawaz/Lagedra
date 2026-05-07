using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record CancellationPolicyDto(
    CancellationPolicyType Type,
    int FreeCancellationDays,
    int? PartialRefundPercent,
    int? PartialRefundDays,
    string? CustomTerms);
