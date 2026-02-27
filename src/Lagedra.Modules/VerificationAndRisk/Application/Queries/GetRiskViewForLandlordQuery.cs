using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.VerificationAndRisk.Application.DTOs;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;

namespace Lagedra.Modules.VerificationAndRisk.Application.Queries;

public sealed record GetRiskViewForLandlordQuery(Guid TenantUserId) : IRequest<Result<RiskViewDto>>;

public sealed class GetRiskViewForLandlordQueryHandler(RiskDbContext dbContext)
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

        return Result<RiskViewDto>.Success(new RiskViewDto(
            profile.TenantUserId,
            profile.VerificationClass,
            profile.Confidence.Level,
            profile.Confidence.Reason,
            profile.DepositBandLowCents,
            profile.DepositBandHighCents,
            profile.ComputedAt));
    }
}
