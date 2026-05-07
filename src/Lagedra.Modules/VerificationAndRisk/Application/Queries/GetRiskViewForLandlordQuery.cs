using Lagedra.Modules.VerificationAndRisk.Application.DTOs;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.VerificationAndRisk.Application.Queries;

public sealed record GetRiskViewForLandlordQuery(Guid TenantUserId) : IRequest<Result<RiskViewDto>>;

public sealed class GetRiskViewForLandlordQueryHandler(
    RiskDbContext dbContext,
    IPartnerEndorsementProvider endorsementProvider,
    IUserInsuranceStatusProvider insuranceProvider)
    : IRequestHandler<GetRiskViewForLandlordQuery, Result<RiskViewDto>>
{
    public async Task<Result<RiskViewDto>> Handle(
        GetRiskViewForLandlordQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.RiskProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantUserId == request.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return Result<RiskViewDto>.Failure(
                new Error("Risk.NotFound", "Risk profile not found for tenant."));
        }

        var endorsements = await endorsementProvider
            .GetActiveEndorsementsAsync(request.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var insurance = await insuranceProvider
            .GetBestStatusForUserAsync(request.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var endorsedBySummaries = endorsements
            .Select(e => new EndorsementSummaryDto(
                e.EndorsementId,
                e.OrganizationId,
                e.OrganizationName,
                e.ApprovedAt,
                e.ExpiresAt))
            .ToList()
            .AsReadOnly();

        var protectionTier = ResolveProtectionTier(endorsements.Count > 0, insurance);

        return Result<RiskViewDto>.Success(new RiskViewDto(
            profile.TenantUserId,
            profile.VerificationClass,
            profile.Confidence.Level,
            profile.Confidence.Reason,
            profile.DepositBandLowCents,
            profile.DepositBandHighCents,
            profile.ComputedAt,
            protectionTier,
            endorsedBySummaries));
    }

    /// <summary>
    /// Tier resolution rules (Phase 18.10 — Option A):
    /// <list type="bullet">
    ///   <item>If the tenant has any active partner endorsement → <see cref="ProtectionTier.PartnerBacked"/>
    ///         (more informative for the landlord, even if a third-party policy also exists).</item>
    ///   <item>Else if the tenant has any active third-party insurance binding →
    ///         <see cref="ProtectionTier.ThirdPartyInsured"/>.</item>
    ///   <item>Else → <see cref="ProtectionTier.Uninsured"/>.</item>
    /// </list>
    /// The tier is a label only; the deposit band reduction comes from
    /// <c>DepositRecommendationPolicy</c> reading <see cref="InsuranceStatus.InstitutionBacked"/>,
    /// so a partner-backed-and-insured tenant does NOT receive a double discount.
    /// </summary>
    private static ProtectionTier ResolveProtectionTier(bool hasActiveEndorsement, UserInsuranceStatusDto insurance)
    {
        if (hasActiveEndorsement) return ProtectionTier.PartnerBacked;
        if (insurance.HasInstitutionBackedPolicy || insurance.HasActivePolicy)
        {
            return ProtectionTier.ThirdPartyInsured;
        }
        return ProtectionTier.Uninsured;
    }
}
