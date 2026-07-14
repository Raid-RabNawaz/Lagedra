using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.SharedKernel.Integration;

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
    string? JurisdictionWarning,
    DealApplicationSource Source = DealApplicationSource.TenantSelfApply,
    // New fields appended after Source so partner-direct callers that
    // still construct the DTO positionally don't have to adjust their
    // call sites for two unrelated additions.
    int GuestCount = 1,
    string? Message = null,
    // Predetermined-deposit snapshot + Truth Surface consent surfacing.
    long? ServiceFeeCents = null,
    long? TotalPayableSnapshotCents = null,
    TenantVerificationTier? TenantVerificationTier = null,
    string? DepositReason = null,
    Guid? TruthSurfaceSnapshotId = null,
    bool TenantConsentGiven = false,
    bool HostConsentGiven = false,
    // Listing context for inbox cards (tenant "my applications" view in
    // particular, which can't read the host-owned listing summary itself).
    // Populated by list queries that enrich from IListingProvider; left null
    // by command results where it isn't needed.
    string? ListingTitle = null,
    Uri? ListingCoverPhotoUri = null,
    string? ListingCity = null,
    ApplicationPayerType PayerType = ApplicationPayerType.Tenant,
    Guid? PayerUserId = null,
    bool HasPaymentMethod = false,
    bool IsPaymentReady = false,
    string? PartnerOrganizationName = null);
