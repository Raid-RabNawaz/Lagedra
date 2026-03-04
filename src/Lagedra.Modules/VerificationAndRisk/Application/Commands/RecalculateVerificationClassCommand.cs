using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.VerificationAndRisk.Domain.Aggregates;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.Modules.VerificationAndRisk.Domain.ValueObjects;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;

namespace Lagedra.Modules.VerificationAndRisk.Application.Commands;

public sealed record RecalculateVerificationClassCommand(
    Guid TenantUserId,
    IdentityVerificationStatus IdentityStatus,
    BackgroundCheckStatus BackgroundStatus,
    InsuranceStatus InsuranceStatus,
    int ViolationCount) : IRequest<Result>;

public sealed class RecalculateVerificationClassCommandHandler(
    RiskDbContext dbContext)
    : IRequestHandler<RecalculateVerificationClassCommand, Result>
{
    public async Task<Result> Handle(
        RecalculateVerificationClassCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.RiskProfiles
            .FirstOrDefaultAsync(r => r.TenantUserId == request.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            profile = RiskProfile.Create(request.TenantUserId);
            dbContext.RiskProfiles.Add(profile);
        }

        var input = new VerificationInput(
            request.IdentityStatus,
            request.BackgroundStatus,
            request.InsuranceStatus,
            request.ViolationCount);

        profile.Recompute(input);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
