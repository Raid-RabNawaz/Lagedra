using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;

public sealed class PartnerDirectBookingService(ISender sender)
    : IPartnerDirectBookingService
{
    public async Task<Result<PartnerDirectBookingResult>> SubmitAsync(
        PartnerDirectBookingRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.Send(new SubmitPartnerDirectApplicationCommand(
            request.ListingId,
            request.TenantUserId,
            request.PartnerOrganizationId,
            request.RequestedCheckIn,
            request.RequestedCheckOut), ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<PartnerDirectBookingResult>.Failure(result.Error);
        }

        var dto = result.Value;
        return Result<PartnerDirectBookingResult>.Success(new PartnerDirectBookingResult(
            dto.ApplicationId,
            dto.ListingId,
            dto.TenantUserId,
            dto.LandlordUserId,
            dto.Status.ToString(),
            dto.RequestedCheckIn,
            dto.RequestedCheckOut,
            dto.StayDurationDays));
    }
}
