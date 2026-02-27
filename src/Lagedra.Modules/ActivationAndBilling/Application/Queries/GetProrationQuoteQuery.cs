using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Policies;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record GetProrationQuoteQuery(
    Guid DealId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<Result<ProrationQuoteDto>>;

public sealed class GetProrationQuoteQueryHandler(IPlatformSettingsService settings)
    : IRequestHandler<GetProrationQuoteQuery, Result<ProrationQuoteDto>>
{
    public async Task<Result<ProrationQuoteDto>> Handle(
        GetProrationQuoteQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.EndDate <= request.StartDate)
        {
            return Result<ProrationQuoteDto>.Failure(
                new Error("Proration.InvalidDates", "End date must be after start date."));
        }

        var monthlyFee = await settings.GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, cancellationToken).ConfigureAwait(false);
        var pilotDiscount = await settings.GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, cancellationToken).ConfigureAwait(false);
        var isPilot = await settings.GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, cancellationToken).ConfigureAwait(false);
        var effectiveFee = isPilot ? monthlyFee - pilotDiscount : monthlyFee;

        var window = BillingPolicy.ComputeProration(request.StartDate, request.EndDate, effectiveFee);

        var dto = new ProrationQuoteDto(
            window.StartDate,
            window.EndDate,
            window.TotalDays,
            window.ProratedAmountCents,
            effectiveFee,
            "USD");

        return Result<ProrationQuoteDto>.Success(dto);
    }
}
