using Lagedra.Modules.ActivationAndBilling.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record DealApplicationDto(
    Guid ApplicationId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    DealApplicationStatus Status,
    Guid? DealId,
    DateTime SubmittedAt,
    DateTime? DecidedAt,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    int StayDurationDays,
    long? DepositAmountCents,
    long? InsuranceFeeCents,
    long? FirstMonthRentCents,
    Guid? PartnerOrganizationId,
    bool IsPartnerReferred,
    string? JurisdictionWarning);
