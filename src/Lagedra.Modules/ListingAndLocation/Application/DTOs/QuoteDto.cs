namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

/// <summary>
/// Itemised pre-flight quote for a Listing Detail page (Phase 16).
/// <see cref="TotalCents"/> sums the tenant-payable lines
/// (<see cref="RentCents"/> + <see cref="DepositCents"/> +
/// <see cref="InsuranceFeeCents"/> + <see cref="ServiceFeeCents"/>);
/// <see cref="ProtocolFeeCents"/> is the host's monthly platform fee, surfaced
/// to the tenant for transparency only and not added to the total.
/// </summary>
public sealed record QuoteDto(
    DateOnly CheckIn,
    DateOnly CheckOut,
    int StayDurationDays,
    long RentCents,
    long DepositCents,
    long InsuranceFeeCents,
    long ProtocolFeeCents,
    long ServiceFeeCents,
    long TotalCents,
    string Currency);
