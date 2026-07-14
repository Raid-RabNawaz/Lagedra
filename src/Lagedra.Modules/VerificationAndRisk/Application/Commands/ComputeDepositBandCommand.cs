using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Lagedra.Modules.VerificationAndRisk.Application.DTOs;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;

namespace Lagedra.Modules.VerificationAndRisk.Application.Commands;

public sealed record ComputeDepositBandCommand(
    Guid TenantUserId,
    InsuranceStatus InsuranceStatus,
    long JurisdictionCapCents,
    string? JurisdictionCode = null,
    long? MonthlyRentCents = null) : IRequest<Result<DepositBandDto>>;

public sealed class ComputeDepositBandCommandHandler(
    RiskDbContext dbContext,
    IServiceProvider serviceProvider)
    : IRequestHandler<ComputeDepositBandCommand, Result<DepositBandDto>>
{
    public async Task<Result<DepositBandDto>> Handle(
        ComputeDepositBandCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.RiskProfiles
            .FirstOrDefaultAsync(r => r.TenantUserId == request.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return Result<DepositBandDto>.Failure(
                new Error("Risk.NotFound", "Risk profile not found for tenant."));
        }

        // Jurisdiction deposit-cap packs were replaced by lease agreement templates.
        // Deposit band uses the caller-supplied jurisdiction cap (typically listing MaxDepositCents).
        var capCents = request.JurisdictionCapCents;

        double? reputationAverage = null;
        var reputationReviewCount = 0;
        var reputationProvider = serviceProvider.GetService<IReviewReputationProvider>();
        if (reputationProvider is not null)
        {
            var reputation = await reputationProvider
                .GetUserReputationAsync(request.TenantUserId, cancellationToken)
                .ConfigureAwait(false);
            if (reputation is not null)
            {
                reputationAverage = reputation.AverageOverall;
                reputationReviewCount = reputation.ReviewCount;
            }
        }

        profile.UpdateDepositBand(
            request.InsuranceStatus,
            capCents,
            reputationAverage,
            reputationReviewCount);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DepositBandDto>.Success(
            new DepositBandDto(profile.DepositBandLowCents, profile.DepositBandHighCents));
    }
}
