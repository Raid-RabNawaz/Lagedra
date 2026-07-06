using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Services;

/// <summary>
/// Consent captured for both parties, embedded into the canonical content so it
/// becomes part of the signed proof. Tenant consent is gathered at request time,
/// host consent at approval time.
/// </summary>
public sealed record SnapshotConsentInput(
    Guid TenantUserId,
    DateTime TenantAt,
    string? TenantIp,
    string? TenantUserAgent,
    string TenantVersion,
    Guid HostUserId,
    DateTime HostAt,
    string? HostIp,
    string? HostUserAgent,
    string HostVersion);

/// <summary>
/// Shared builder that gathers all cross-module data for a deal and produces a
/// draft <see cref="TruthSnapshot"/> (submitted for confirmation, not yet
/// sealed) with deterministic canonical content. Used by both the create-only
/// path (legacy/partner) and the atomic create-and-seal path (host approval).
/// </summary>
public interface ITruthSurfaceSnapshotBuilder
{
    Task<Result<TruthSnapshot>> BuildDraftAsync(
        Guid dealId,
        Guid requestedByUserId,
        SnapshotConsentInput? consent,
        CancellationToken cancellationToken);
}

public sealed class TruthSurfaceSnapshotBuilder(
    TruthSurfaceDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider,
    IListingProvider listingProvider,
    IHostProfileProvider hostProfileProvider,
    IVerificationSignalProvider verificationSignalProvider,
    IJurisdictionPackProvider jurisdictionPackProvider,
    IPartnerEndorsementProvider partnerEndorsementProvider,
    IUserInsuranceStatusProvider insuranceStatusProvider,
    IPlatformSettingsService settings)
    : ITruthSurfaceSnapshotBuilder
{
    public const string ProtocolVersion = "1.0";

    /// <summary>
    /// Canonical content schema version. Bumped to "2.4": adds the
    /// <c>depositReturnTerms</c> block that seals the non-custodial deposit
    /// contract into the signed agreement — the deposit is paid to and held by
    /// the host, returned by the host directly after move-out (less any agreed
    /// or arbitrated deductions), and the deal only completes once both parties
    /// confirm the return/receipt. "2.3" removed the <c>consent</c> IP/User-Agent
    /// fingerprints from the hashed payload (they remain on the
    /// <see cref="TruthSnapshot"/> columns for arbitration/admin use); only
    /// <c>userId</c>, <c>consentedAt</c>, and <c>consentVersion</c> stay in the
    /// hashed consent block. Changes only affect new snapshots; older hashes
    /// are unaffected.
    /// </summary>
    private const string CanonicalSchemaVersion = "2.4";

    /// <summary>Fallback deposit-return window (days after move-out) when the platform setting is unset.</summary>
    private const int DefaultDepositReturnWindowDays = 14;

    private const string DefaultJurisdictionCode = "US-DEFAULT";
    private const string DefaultJurisdictionPackVersion = "default-v0";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<Result<TruthSnapshot>> BuildDraftAsync(
        Guid dealId,
        Guid requestedByUserId,
        SnapshotConsentInput? consent,
        CancellationToken cancellationToken)
    {
        var deal = await dealStatusProvider
            .GetDealDetailsAsync(dealId, cancellationToken)
            .ConfigureAwait(false);

        if (deal is null)
        {
            return Result<TruthSnapshot>.Failure(
                new Error("TruthSurface.DealNotApproved", "No approved deal found for this ID."));
        }

        if (deal.LandlordUserId != requestedByUserId)
        {
            return Result<TruthSnapshot>.Failure(
                new Error("TruthSurface.Unauthorized",
                    "Only the landlord can create the truth surface for a deal."));
        }

        var existingSnapshot = await dbContext.Snapshots
            .AsNoTracking()
            .AnyAsync(s => s.DealId == dealId
                           && s.Status != TruthSurfaceStatus.Superseded
                           && s.Status != TruthSurfaceStatus.Voided, cancellationToken)
            .ConfigureAwait(false);

        if (existingSnapshot)
        {
            return Result<TruthSnapshot>.Failure(
                new Error("TruthSurface.AlreadyExists",
                    "A truth surface snapshot already exists for this deal."));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(deal.ListingId, cancellationToken)
            .ConfigureAwait(false);

        var listingSummaries = await listingProvider
            .GetListingSummariesAsync([deal.ListingId], cancellationToken)
            .ConfigureAwait(false);

        var listingSummary = listingSummaries.Count > 0 ? listingSummaries[0] : null;

        var landlordProfile = await hostProfileProvider
            .GetProfileAsync(deal.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);

        var landlordSignals = await verificationSignalProvider
            .GetSignalsAsync(deal.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);

        var tenantProfile = await hostProfileProvider
            .GetProfileAsync(deal.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var tenantSignals = await verificationSignalProvider
            .GetSignalsAsync(deal.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var tenantEndorsements = await partnerEndorsementProvider
            .GetActiveEndorsementsAsync(deal.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var tenantInsurance = await insuranceStatusProvider
            .GetBestStatusForUserAsync(deal.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var tenantProtectionTier = ResolveProtectionTier(
            hasActiveEndorsement: tenantEndorsements.Count > 0,
            insurance: tenantInsurance);

        var jurisdictionCode = !string.IsNullOrWhiteSpace(listing?.JurisdictionCode)
            ? listing!.JurisdictionCode!
            : DefaultJurisdictionCode;

        var pack = await jurisdictionPackProvider
            .GetActivePackAsync(jurisdictionCode, cancellationToken)
            .ConfigureAwait(false);

        var jurisdictionPackVersion = pack is not null
            ? $"{jurisdictionCode}@v{pack.VersionNumber}"
            : DefaultJurisdictionPackVersion;

        var monthlyProtocolFeeCents = await ResolveProtocolFeeAsync(cancellationToken)
            .ConfigureAwait(false);

        var depositReturnWindowDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.DamageClaimFilingDeadlineDays,
                DefaultDepositReturnWindowDays, cancellationToken)
            .ConfigureAwait(false);

        var snapshotId = Guid.NewGuid();

        var canonicalContent = BuildCanonicalContent(
            snapshotId,
            jurisdictionCode,
            pack,
            jurisdictionPackVersion,
            monthlyProtocolFeeCents,
            depositReturnWindowDays,
            deal,
            listing,
            listingSummary,
            landlordProfile,
            landlordSignals,
            tenantProfile,
            tenantSignals,
            tenantProtectionTier,
            tenantEndorsements,
            consent);

        var snapshot = TruthSnapshot.CreateDraftWithId(
            snapshotId,
            dealId,
            ProtocolVersion,
            jurisdictionPackVersion,
            canonicalContent);

        snapshot.SubmitForConfirmation();

        return Result<TruthSnapshot>.Success(snapshot);
    }

    private async Task<long> ResolveProtocolFeeAsync(CancellationToken ct)
    {
        var monthlyFee = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, ct).ConfigureAwait(false);
        var pilotDiscount = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, ct).ConfigureAwait(false);
        var pilotActive = await settings
            .GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, ct).ConfigureAwait(false);

        return pilotActive ? monthlyFee - pilotDiscount : monthlyFee;
    }

    private static ProtectionTierKind ResolveProtectionTier(
        bool hasActiveEndorsement,
        UserInsuranceStatusDto insurance)
    {
        if (hasActiveEndorsement) return ProtectionTierKind.PartnerBacked;
        if (insurance.HasInstitutionBackedPolicy || insurance.HasActivePolicy)
        {
            return ProtectionTierKind.ThirdPartyInsured;
        }
        return ProtectionTierKind.Uninsured;
    }

    private static string BuildCanonicalContent(
        Guid snapshotId,
        string jurisdictionCode,
        JurisdictionPackInfo? pack,
        string jurisdictionPackVersion,
        long monthlyProtocolFeeCents,
        int depositReturnWindowDays,
        DealApplicationDetailsDto deal,
        ListingDetailsDto? listing,
        ListingSummaryInfoDto? listingSummary,
        HostProfileDto? landlordProfile,
        VerificationSignalDto? landlordSignals,
        HostProfileDto? tenantProfile,
        VerificationSignalDto? tenantSignals,
        ProtectionTierKind tenantProtectionTier,
        IReadOnlyList<ActiveEndorsementInfo> tenantActiveEndorsements,
        SnapshotConsentInput? consent)
    {
        var firstMonth = deal.FirstMonthRentCents ?? 0;
        var deposit = deal.DepositAmountCents ?? 0;
        var insurance = deal.InsuranceFeeCents ?? 0;
        var serviceFee = deal.ServiceFeeCents ?? 0;
        var totalDue = deal.TotalPayableSnapshotCents ?? (firstMonth + deposit + insurance + serviceFee);

        var content = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = CanonicalSchemaVersion,
            ["protocolVersion"] = ProtocolVersion,
            ["snapshotId"] = snapshotId.ToString(),
            ["dealId"] = deal.DealId.ToString(),
            ["applicationId"] = deal.ApplicationId.ToString(),
            ["tenantMessage"] = deal.Message,

            ["parties"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["landlord"] = BuildPartyBlock(deal.LandlordUserId, landlordProfile, landlordSignals),
                ["tenant"] = BuildTenantPartyBlock(
                    deal.TenantUserId,
                    tenantProfile,
                    tenantSignals,
                    tenantProtectionTier,
                    tenantActiveEndorsements)
            },

            ["listing"] = BuildListingBlock(deal.ListingId, listing, listingSummary),

            ["dates"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["checkIn"] = deal.RequestedCheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["checkOut"] = deal.RequestedCheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["stayDurationDays"] = deal.StayDurationDays,
                ["guestCount"] = deal.GuestCount
            },

            ["financials"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["currency"] = "USD",
                ["firstMonthRentCents"] = firstMonth,
                ["depositAmountCents"] = deposit,
                ["depositReason"] = deal.DepositReason,
                ["tenantVerificationTier"] = deal.TenantVerificationTier?.ToString(),
                ["insuranceFeeCents"] = insurance,
                ["serviceFeeCents"] = serviceFee,
                ["monthlyProtocolFeeCents"] = monthlyProtocolFeeCents,
                ["totalDueAtCheckoutCents"] = totalDue
            },

            // Non-custodial deposit contract, sealed at booking: Lagedra never
            // holds the deposit. The tenant pays it directly to the host, the
            // host holds it for the stay and returns it directly after move-out
            // (less any agreed or arbitrated deductions), and the deal only
            // completes once BOTH parties confirm the return and receipt.
            // A shortfall or dispute is resolved through arbitration.
            ["depositReturnTerms"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["custodian"] = "host",
                ["returnedBy"] = "host",
                ["returnMethod"] = "direct",
                ["returnWindowDays"] = depositReturnWindowDays,
                ["requiresBilateralConfirmation"] = true,
                ["disputeResolution"] = "arbitration"
            },

            ["cancellationPolicy"] = listing?.CancellationPolicy is not null
                ? new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["type"] = listing.CancellationPolicy.Type.ToString(),
                    ["freeCancellationDays"] = listing.CancellationPolicy.FreeCancellationDays,
                    ["partialRefundPercent"] = listing.CancellationPolicy.PartialRefundPercent,
                    ["partialRefundDays"] = listing.CancellationPolicy.PartialRefundDays,
                    ["customTerms"] = listing.CancellationPolicy.CustomTerms
                }
                : null,

            ["houseRules"] = listing?.HouseRules is not null
                ? new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["checkInTime"] = listing.HouseRules.CheckInTime,
                    ["checkOutTime"] = listing.HouseRules.CheckOutTime,
                    ["maxGuests"] = listing.HouseRules.MaxGuests,
                    ["petsAllowed"] = listing.HouseRules.PetsAllowed,
                    ["petsNotes"] = listing.HouseRules.PetsNotes,
                    ["smokingAllowed"] = listing.HouseRules.SmokingAllowed,
                    ["partiesAllowed"] = listing.HouseRules.PartiesAllowed,
                    ["quietHoursStart"] = listing.HouseRules.QuietHoursStart,
                    ["quietHoursEnd"] = listing.HouseRules.QuietHoursEnd,
                    ["leavingInstructions"] = listing.HouseRules.LeavingInstructions,
                    ["additionalRules"] = listing.HouseRules.AdditionalRules
                }
                : null,

            ["jurisdiction"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["code"] = jurisdictionCode,
                ["packVersion"] = jurisdictionPackVersion,
                ["packId"] = pack?.PackId.ToString(),
                ["packVersionId"] = pack?.ActiveVersionId.ToString(),
                ["packVersionNumber"] = pack?.VersionNumber,
                ["packEffectiveDate"] = pack?.EffectiveDate?.ToString("o", CultureInfo.InvariantCulture),
                ["warning"] = deal.JurisdictionWarning
            },

            ["consent"] = consent is null
                ? null
                : new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["tenant"] = BuildConsentBlock(
                        consent.TenantUserId, consent.TenantAt, consent.TenantVersion),
                    ["host"] = BuildConsentBlock(
                        consent.HostUserId, consent.HostAt, consent.HostVersion)
                }
        };

        return JsonSerializer.Serialize(content, s_jsonOptions);
    }

    // The IP address and User-Agent captured at consent time are deliberately
    // NOT written into the signed canonical content: it is readable by both
    // parties, so embedding one side's device fingerprint would expose it to
    // the other. They remain on the TruthSnapshot consent columns for
    // arbitration/admin auditing only.
    private static SortedDictionary<string, object?> BuildConsentBlock(
        Guid userId,
        DateTime at,
        string version)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["userId"] = userId.ToString(),
            ["consentedAt"] = at.ToString("o", CultureInfo.InvariantCulture),
            ["consentVersion"] = version
        };
    }

    private static SortedDictionary<string, object?> BuildPartyBlock(
        Guid userId,
        HostProfileDto? profile,
        VerificationSignalDto? signals)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["userId"] = userId.ToString(),
            ["displayName"] = profile?.DisplayName,
            ["isGovernmentIdVerified"] = profile?.IsGovernmentIdVerified ?? false,
            ["isPhoneVerified"] = profile?.IsPhoneVerified ?? false,
            ["isIdentityVerified"] = signals?.IsIdentityVerified ?? false,
            ["isBackgroundCheckPassed"] = signals?.IsBackgroundCheckPassed ?? false,
            ["memberSince"] = profile?.MemberSince.ToString("o", CultureInfo.InvariantCulture)
        };
    }

    private static SortedDictionary<string, object?> BuildTenantPartyBlock(
        Guid userId,
        HostProfileDto? profile,
        VerificationSignalDto? signals,
        ProtectionTierKind protectionTier,
        IReadOnlyList<ActiveEndorsementInfo> activeEndorsements)
    {
        var block = BuildPartyBlock(userId, profile, signals);

        block["protectionTier"] = PartnerEndorsementCopy.ToToken(protectionTier);

        var endorsementBlocks = activeEndorsements
            .OrderBy(e => e.OrganizationId)
            .Select(e => (object)new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["organizationId"] = e.OrganizationId.ToString(),
                ["organizationName"] = e.OrganizationName,
                ["approvedAt"] = e.ApprovedAt.ToString("o", CultureInfo.InvariantCulture),
                ["expiresAt"] = e.ExpiresAt.ToString("o", CultureInfo.InvariantCulture)
            })
            .ToList();

        block["partnerEndorsements"] = endorsementBlocks;

        return block;
    }

    private static SortedDictionary<string, object?> BuildListingBlock(
        Guid listingId,
        ListingDetailsDto? listing,
        ListingSummaryInfoDto? listingSummary)
    {
        var block = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = listingId.ToString(),
            ["title"] = listing?.Title ?? listingSummary?.Title,
            ["propertyType"] = listing?.PropertyType,
            ["bedrooms"] = listing?.Bedrooms,
            ["bathrooms"] = listing?.Bathrooms,
            ["squareFootage"] = listing?.SquareFootage,
            ["monthlyRentCents"] = listing?.MonthlyRentCents,
            ["maxDepositCents"] = listing?.MaxDepositCents,
            ["minStayDays"] = listing?.MinStayDays,
            ["maxStayDays"] = listing?.MaxStayDays,
            ["virtualTourUrl"] = listing?.VirtualTourUrl?.OriginalString,
            ["amenities"] = listing?.AmenityNames ?? Array.Empty<string>(),
            ["safetyDevices"] = listing?.SafetyDeviceNames ?? Array.Empty<string>(),
            ["considerations"] = listing?.ConsiderationNames ?? Array.Empty<string>()
        };

        if (listing?.PreciseAddress is not null)
        {
            block["address"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["street"] = listing.PreciseAddress.Street,
                ["city"] = listing.PreciseAddress.City,
                ["state"] = listing.PreciseAddress.State,
                ["zipCode"] = listing.PreciseAddress.ZipCode,
                ["country"] = listing.PreciseAddress.Country
            };
        }
        else
        {
            block["address"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["city"] = listingSummary?.City
            };
        }

        return block;
    }
}
