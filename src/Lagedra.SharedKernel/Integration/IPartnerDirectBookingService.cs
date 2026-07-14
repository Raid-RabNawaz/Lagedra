using Lagedra.SharedKernel.Results;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module hook implemented by <c>Lagedra.Modules.ActivationAndBilling</c>.
/// Submits a <c>DealApplication</c> on a tenant's behalf as part of the partner
/// direct-reservation flow.
/// </summary>
public interface IPartnerDirectBookingService
{
    Task<Result<PartnerDirectBookingResult>> SubmitAsync(
        PartnerDirectBookingRequest request,
        CancellationToken ct = default);
}

public enum PartnerDirectBookingPayerType
{
    Tenant = 0,
    PartnerOrganization = 1,
}

public sealed record PartnerDirectBookingRequest(
    Guid ListingId,
    Guid TenantUserId,
    Guid PartnerOrganizationId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    PartnerDirectBookingPayerType PayerType = PartnerDirectBookingPayerType.Tenant,
    Guid? PayerUserId = null,
    string? StripePaymentMethodId = null);

public sealed record PartnerDirectBookingResult(
    Guid ApplicationId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    string Status,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    int StayDurationDays);
