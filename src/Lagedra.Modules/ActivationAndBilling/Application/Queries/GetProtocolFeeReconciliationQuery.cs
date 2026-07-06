using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

/// <summary>
/// Surfaces whether the configured protocol fee (shown to hosts across the app)
/// matches the Stripe subscription price they are actually charged. Consumed by
/// the admin Platform Settings screen and dashboard so operators can spot and
/// fix a drift before hosts see a wrong number.
/// </summary>
public sealed record GetProtocolFeeReconciliationQuery()
    : IRequest<Result<ProtocolFeeReconciliationDto>>;

public sealed class GetProtocolFeeReconciliationQueryHandler(
    IStripeService stripeService,
    IPlatformSettingsService settings)
    : IRequestHandler<GetProtocolFeeReconciliationQuery, Result<ProtocolFeeReconciliationDto>>
{
    public async Task<Result<ProtocolFeeReconciliationDto>> Handle(
        GetProtocolFeeReconciliationQuery request,
        CancellationToken cancellationToken)
    {
        var configuredFeeCents = await ResolveConfiguredProtocolFeeAsync(cancellationToken)
            .ConfigureAwait(false);

        var priceId = await settings
            .GetStringAsync(PlatformSettingKeys.StripePlatformFeePriceId, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(priceId))
        {
            return Result<ProtocolFeeReconciliationDto>.Success(new ProtocolFeeReconciliationDto(
                PriceConfigured: false,
                StripePriceId: null,
                ConfiguredMonthlyFeeCents: configuredFeeCents,
                StripePriceAmountCents: null,
                InSync: false,
                Issue: "price_not_configured"));
        }

        long? stripeAmountCents;
        try
        {
            stripeAmountCents = await stripeService
                .GetPriceAmountCentsAsync(priceId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Stripe.StripeException)
        {
            return Result<ProtocolFeeReconciliationDto>.Success(new ProtocolFeeReconciliationDto(
                PriceConfigured: true,
                StripePriceId: priceId,
                ConfiguredMonthlyFeeCents: configuredFeeCents,
                StripePriceAmountCents: null,
                InSync: false,
                Issue: "stripe_error"));
        }

        if (stripeAmountCents is null)
        {
            return Result<ProtocolFeeReconciliationDto>.Success(new ProtocolFeeReconciliationDto(
                PriceConfigured: true,
                StripePriceId: priceId,
                ConfiguredMonthlyFeeCents: configuredFeeCents,
                StripePriceAmountCents: null,
                InSync: false,
                Issue: "no_unit_amount"));
        }

        var inSync = stripeAmountCents.Value == configuredFeeCents;

        return Result<ProtocolFeeReconciliationDto>.Success(new ProtocolFeeReconciliationDto(
            PriceConfigured: true,
            StripePriceId: priceId,
            ConfiguredMonthlyFeeCents: configuredFeeCents,
            StripePriceAmountCents: stripeAmountCents.Value,
            InSync: inSync,
            Issue: inSync ? null : "drift"));
    }

    private async Task<long> ResolveConfiguredProtocolFeeAsync(CancellationToken ct)
    {
        var monthlyFee = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, ct)
            .ConfigureAwait(false);
        var pilotDiscount = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, ct)
            .ConfigureAwait(false);
        var isPilot = await settings
            .GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, ct)
            .ConfigureAwait(false);

        return isPilot ? monthlyFee - pilotDiscount : monthlyFee;
    }
}
