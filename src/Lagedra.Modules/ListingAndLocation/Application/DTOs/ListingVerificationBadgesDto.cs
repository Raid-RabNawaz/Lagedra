namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record ListingVerificationBadgesDto(
    bool IsHostVerified,
    bool IsHostKycComplete,
    bool? IsInsuranceActive);
