using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Policies;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record GetProrationQuoteQuery(
    Guid DealId,
    Guid CallerUserId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsAdmin = false) : IRequest<Result<ProrationQuoteDto>>;

public sealed class GetProrationQuoteQueryHandler(
    BillingDbContext dbContext,
    IPlatformSettingsService settings)
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

        // Quotes are deal-scoped and reveal financial info, so require the caller
        // to participate in the deal (or be a platform admin). The role merge
        // means we cannot rely on a "Landlord"/"Tenant" role split.
        if (!request.IsAdmin)
        {
            var application = await dbContext.DealApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
                .ConfigureAwait(false);

            if (application is null
                || (application.TenantUserId != request.CallerUserId
                    && application.LandlordUserId != request.CallerUserId))
            {
                return Result<ProrationQuoteDto>.Failure(
                    new Error("Proration.Forbidden",
                        "You do not have access to this deal's proration quote."));
            }
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
