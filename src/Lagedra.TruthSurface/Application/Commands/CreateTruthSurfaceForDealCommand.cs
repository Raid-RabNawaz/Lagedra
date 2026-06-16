using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Commands;

public sealed record CreateTruthSurfaceForDealCommand(
    Guid DealId,
    Guid RequestedByUserId) : IRequest<Result<TruthSurfaceDto>>;

public sealed class CreateTruthSurfaceForDealCommandHandler(
    TruthSurfaceDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider,
    IListingProvider listingProvider,
    IHostProfileProvider hostProfileProvider,
    IVerificationSignalProvider verificationSignalProvider,
    IJurisdictionPackProvider jurisdictionPackProvider,
    IPartnerEndorsementProvider partnerEndorsementProvider,
    IUserInsuranceStatusProvider insuranceStatusProvider,
    IPlatformSettingsService settings)
    : IRequestHandler<CreateTruthSurfaceForDealCommand, Result<TruthSurfaceDto>>
{
    private const string ProtocolVersion = "1.0";

    /// <summary>
    /// Canonical content schema version. Bumped to "2.1" in Phase 18.10 to add
    /// <c>parties.tenant.protectionTier</c> and <c>parties.tenant.partnerEndorsement</c>
    /// (additive only — existing v2 snapshot hashes remain valid because they don't
    /// contain those keys, and new v2.1 snapshots include them in stable sorted order).
    /// </summary>
    private const string CanonicalSchemaVersion = "2.1";

    private const string DefaultJurisdictionCode = "US-DEFAULT";
    private const string DefaultJurisdictionPackVersion = "default-v0";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<Result<TruthSurfaceDto>> Handle(
        CreateTruthSurfaceForDealCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var deal = await dealStatusProvider
            .GetDealDetailsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (deal is null)
        {
            return Result<TruthSurfaceDto>.Failure(
                new Error("TruthSurface.DealNotApproved",
                    "No approved deal found for this ID."));
        }

        if (deal.LandlordUserId != request.RequestedByUserId)
        {
            return Result<TruthSurfaceDto>.Failure(
                new Error("TruthSurface.Unauthorized",
                    "Only the landlord can create the truth surface for a deal."));
        }

        var existingSnapshot = await dbContext.Snapshots
            .AsNoTracking()
            .AnyAsync(s => s.DealId == request.DealId
                           && s.Status != TruthSurfaceStatus.Superseded, cancellationToken)
            .ConfigureAwait(false);

        if (existingSnapshot)
        {
            return Result<TruthSurfaceDto>.Failure(
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

        var snapshotId = Guid.NewGuid();

        var canonicalContent = BuildCanonicalContent(
            snapshotId,
            jurisdictionCode,
            pack,
            jurisdictionPackVersion,
            monthlyProtocolFeeCents,
            deal,
            listing,
            listingSummary,
            landlordProfile,
            landlordSignals,
            tenantProfile,
            tenantSignals,
            tenantProtectionTier,
            tenantEndorsements);

        var snapshot = TruthSnapshot.CreateDraftWithId(
            snapshotId,
            request.DealId,
            ProtocolVersion,
            jurisdictionPackVersion,
            canonicalContent);

        snapshot.SubmitForConfirmation();

        dbContext.Snapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TruthSurfaceDto>.Success(new TruthSurfaceDto(
            snapshot.Id, snapshot.DealId, snapshot.Status,
            snapshot.ProtocolVersion, snapshot.JurisdictionPackVersion,
            snapshot.CanonicalContent,
            snapshot.InquiryClosed, snapshot.LandlordConfirmed, snapshot.TenantConfirmed,
            snapshot.CreatedAt, snapshot.SealedAt, null));
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
        DealApplicationDetailsDto deal,
        ListingDetailsDto? listing,
        ListingSummaryInfoDto? listingSummary,
        HostProfileDto? landlordProfile,
        VerificationSignalDto? landlordSignals,
        HostProfileDto? tenantProfile,
        VerificationSignalDto? tenantSignals,
        ProtectionTierKind tenantProtectionTier,
        IReadOnlyList<ActiveEndorsementInfo> tenantActiveEndorsements)
    {
        var firstMonth = deal.FirstMonthRentCents ?? 0;
        var deposit = deal.DepositAmountCents ?? 0;
        var insurance = deal.InsuranceFeeCents ?? 0;
        var totalDue = firstMonth + deposit + insurance;

        // Keys are written in a stable order so the SHA-256 hash is reproducible
        // across machines and library versions.
        var content = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = CanonicalSchemaVersion,
            ["protocolVersion"] = ProtocolVersion,
            ["snapshotId"] = snapshotId.ToString(),
            ["dealId"] = deal.DealId.ToString(),
            ["applicationId"] = deal.ApplicationId.ToString(),

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
                ["stayDurationDays"] = deal.StayDurationDays
            },

            ["financials"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["currency"] = "USD",
                ["firstMonthRentCents"] = firstMonth,
                ["depositAmountCents"] = deposit,
                ["insuranceFeeCents"] = insurance,
                ["monthlyProtocolFeeCents"] = monthlyProtocolFeeCents,
                ["totalDueAtCheckoutCents"] = totalDue
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
            }
        };

        return JsonSerializer.Serialize(content, s_jsonOptions);
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

    /// <summary>
    /// Builds the tenant party block plus the v2.1 schema additions:
    /// <c>protectionTier</c> (canonical token via <see cref="PartnerEndorsementCopy.ToToken"/>) and
    /// <c>partnerEndorsements</c> (sorted-by-organizationId array of active endorsements).
    /// Both keys are only emitted in v2.1+ snapshots; older snapshots' canonical content
    /// remains unchanged because they were sealed without these keys.
    /// </summary>
    private static SortedDictionary<string, object?> BuildTenantPartyBlock(
        Guid userId,
        HostProfileDto? profile,
        VerificationSignalDto? signals,
        ProtectionTierKind protectionTier,
        IReadOnlyList<ActiveEndorsementInfo> activeEndorsements)
    {
        var block = BuildPartyBlock(userId, profile, signals);

        block["protectionTier"] = PartnerEndorsementCopy.ToToken(protectionTier);

        // Stable order: sort by organization id so the canonical hash is deterministic
        // across processes that may receive the endorsement list in different DB orderings.
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
