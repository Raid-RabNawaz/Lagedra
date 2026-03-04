using Lagedra.Modules.JurisdictionPacks.Application.Queries;
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
    long JurisdictionCapCents,
    string? JurisdictionCode = null,
    long? MonthlyRentCents = null) : IRequest<Result<DepositBandDto>>;

public sealed class ComputeDepositBandCommandHandler(
    RiskDbContext dbContext,
    IMediator mediator)
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

        var capCents = request.JurisdictionCapCents;

        if (!string.IsNullOrWhiteSpace(request.JurisdictionCode) && request.MonthlyRentCents.HasValue)
        {
            var capResult = await mediator
                .Send(new GetDepositCapQuery(request.JurisdictionCode, request.MonthlyRentCents.Value), cancellationToken)
                .ConfigureAwait(false);

            if (capResult.IsSuccess)
            {
                capCents = capResult.Value.MaxDepositCents;
            }
        }

        profile.UpdateDepositBand(request.InsuranceStatus, capCents);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DepositBandDto>.Success(
            new DepositBandDto(profile.DepositBandLowCents, profile.DepositBandHighCents));
    }
}
