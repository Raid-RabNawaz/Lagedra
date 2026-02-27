using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.VerificationAndRisk.Application.DTOs;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;

namespace Lagedra.Modules.VerificationAndRisk.Application.Commands;

public sealed record ComputeDepositBandCommand(
    Guid TenantUserId,
    InsuranceStatus InsuranceStatus,
    long JurisdictionCapCents) : IRequest<Result<DepositBandDto>>;

public sealed class ComputeDepositBandCommandHandler(
    RiskDbContext dbContext)
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

        profile.UpdateDepositBand(request.InsuranceStatus, request.JurisdictionCapCents);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DepositBandDto>.Success(
            new DepositBandDto(profile.DepositBandLowCents, profile.DepositBandHighCents));
    }
}
