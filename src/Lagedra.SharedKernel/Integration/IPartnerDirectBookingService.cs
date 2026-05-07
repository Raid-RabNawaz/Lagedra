using Lagedra.SharedKernel.Results;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module hook implemented by <c>Lagedra.Modules.ActivationAndBilling</c>.
/// Submits a <c>DealApplication</c> on a tenant's behalf as part of the partner
/// direct-reservation flow (Phase 18.7).
///
/// Returns a transport-friendly snapshot of the new application (not the full
/// <c>DealApplicationDto</c>, to keep the SharedKernel boundary minimal). The
/// PartnerNetwork module only needs the application id + status to link the
/// <c>DirectReservation</c> row and surface a deep-link to the Truth Surface.
/// </summary>
public interface IPartnerDirectBookingService
{
    Task<Result<PartnerDirectBookingResult>> SubmitAsync(
        PartnerDirectBookingRequest request,
        CancellationToken ct = default);
}

public sealed record PartnerDirectBookingRequest(
    Guid ListingId,
    Guid TenantUserId,
    Guid PartnerOrganizationId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut);

public sealed record PartnerDirectBookingResult(
    Guid ApplicationId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    string Status,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    int StayDurationDays);
