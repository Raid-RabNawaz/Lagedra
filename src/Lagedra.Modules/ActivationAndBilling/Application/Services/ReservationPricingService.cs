using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;

namespace Lagedra.Modules.ActivationAndBilling.Application.Services;

/// <summary>
/// Computes the predetermined-deposit reservation price for a tenant + listing:
/// resolves the verification tier, selects the tier's deposit (with reason),
/// quotes insurance + platform service fee, and totals the tenant payable. Used
/// by both the reservation-preview query and the submit command so the figure
/// the tenant sees is exactly what gets snapshotted and charged.
/// </summary>
public interface IReservationPricingService
{
    Task<ReservationPricing> ComputeAsync(
        ListingDetailsDto listing,
        Guid tenantUserId,
        int stayDurationDays,
        TenantVerificationTier? forcedTier = null,
        Guid? forcedPartnerOrganizationId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of reservation pricing. <see cref="TotalPayableCents"/> is the
/// tenant's charge (deposit + first month rent + insurance + service fee).
/// <see cref="MonthlyProtocolFeeCents"/> is a host-side platform fee included
/// for display only — it is NOT part of the tenant total.
/// </summary>
public sealed record ReservationPricing(
    TenantVerificationTier Tier,
    Guid? PartnerOrganizationId,
    long DepositCents,
    string DepositReason,
    long FirstMonthRentCents,
    long InsuranceFeeCents,
    long ServiceFeeCents,
    long MonthlyProtocolFeeCents,
    long TotalPayableCents)
{
    public ReservationDepositSnapshot ToSnapshot() => new(
        Tier,
        DepositCents,
        FirstMonthRentCents,
        InsuranceFeeCents,
        ServiceFeeCents,
        DepositReason);
}

public sealed class ReservationPricingService(
    ITenantVerificationTierResolver tierResolver,
    IInsuranceFeeCalculator insuranceFeeCalculator,
    IPlatformSettingsService settings)
    : IReservationPricingService
{
    public async Task<ReservationPricing> ComputeAsync(
        ListingDetailsDto listing,
        Guid tenantUserId,
        int stayDurationDays,
        TenantVerificationTier? forcedTier = null,
        Guid? forcedPartnerOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);

        TenantVerificationTier tier;
        Guid? partnerOrganizationId;

        if (forcedTier is { } forced)
        {
            tier = forced;
            partnerOrganizationId = forcedPartnerOrganizationId;
        }
        else
        {
            var resolved = await tierResolver
                .ResolveAsync(tenantUserId, cancellationToken)
                .ConfigureAwait(false);
            tier = resolved.Tier;
            partnerOrganizationId = resolved.PartnerOrganizationId;
        }

        var depositSelection = DepositSelectionService.Select(
            tier,
            listing.MaxDepositCents,
            listing.DepositUnverifiedCents,
            listing.DepositBackgroundVerifiedCents,
            listing.DepositPartnerGuaranteedCents);

        var insuranceQuote = await insuranceFeeCalculator
            .CalculateFeeAsync(listing.MonthlyRentCents, stayDurationDays, cancellationToken)
            .ConfigureAwait(false);

        var serviceFee = await ResolveServiceFeeAsync(listing.MonthlyRentCents, cancellationToken)
            .ConfigureAwait(false);

        var protocolFee = await ResolveMonthlyProtocolFeeAsync(cancellationToken)
            .ConfigureAwait(false);

        var total = depositSelection.AmountCents
            + listing.MonthlyRentCents
            + insuranceQuote.FeeCents
            + serviceFee;

        return new ReservationPricing(
            tier,
            partnerOrganizationId,
            depositSelection.AmountCents,
            depositSelection.Reason,
            listing.MonthlyRentCents,
            insuranceQuote.FeeCents,
            serviceFee,
            protocolFee,
            total);
    }

    private async Task<long> ResolveServiceFeeAsync(long rentBaseCents, CancellationToken ct)
    {
        var useFlat = await settings
            .GetBoolAsync(PlatformSettingKeys.TenantServiceFeeUseFlat, false, ct).ConfigureAwait(false);
        var flatCents = await settings
            .GetLongAsync(PlatformSettingKeys.TenantServiceFeeFlatCents, 0, ct).ConfigureAwait(false);
        var bps = await settings
            .GetLongAsync(PlatformSettingKeys.TenantServiceFeeBps, 0, ct).ConfigureAwait(false);

        return TenantServiceFee.Compute(rentBaseCents, useFlat, flatCents, bps);
    }

    private async Task<long> ResolveMonthlyProtocolFeeAsync(CancellationToken ct)
    {
        var monthly = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, ct).ConfigureAwait(false);
        var pilotDiscount = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, ct).ConfigureAwait(false);
        var isPilot = await settings
            .GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, ct).ConfigureAwait(false);

        return isPilot ? monthly - pilotDiscount : monthly;
    }
}
