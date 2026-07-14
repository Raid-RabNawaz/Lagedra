using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

/// <summary>
/// Single source of truth for projecting a <see cref="DealApplication"/> to its
/// API DTO, including the predetermined-deposit snapshot + Truth Surface consent
/// fields. Keeps every query/command in sync instead of repeating positional
/// constructor calls that silently drift.
/// </summary>
internal static class DealApplicationDtoMapper
{
    public static DealApplicationDto ToDto(
        DealApplication a,
        string? partnerOrganizationName = null)
    {
        ArgumentNullException.ThrowIfNull(a);

        return new DealApplicationDto(
            a.Id, a.ListingId, a.TenantUserId, a.LandlordUserId,
            a.Status, a.DealId, a.SubmittedAt, a.DecidedAt,
            a.RequestedCheckIn, a.RequestedCheckOut, a.StayDurationDays,
            a.DepositAmountCents, a.InsuranceFeeCents, a.FirstMonthRentCents,
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning, a.Source,
            a.GuestCount, a.Message,
            a.ServiceFeeCents, a.TotalPayableSnapshotCents,
            a.TenantVerificationTierAtRequest, a.DepositReason,
            a.TruthSurfaceSnapshotId,
            a.TenantTruthSurfaceConsentGiven, a.HostTruthSurfaceConsentGiven,
            ListingTitle: null,
            ListingCoverPhotoUri: null,
            ListingCity: null,
            a.PayerType,
            a.PayerUserId,
            HasPaymentMethod: !string.IsNullOrWhiteSpace(a.StripePaymentMethodId),
            IsPaymentReady: a.IsPaymentReady,
            PartnerOrganizationName: partnerOrganizationName);
    }
}
