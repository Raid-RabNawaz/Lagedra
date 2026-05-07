using Lagedra.SharedKernel.Results;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record ApproveManualVerificationCommand(Guid ProfileId) : IRequest<Result>;

public sealed class ApproveManualVerificationCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<ApproveManualVerificationCommand, Result>
{
    public async Task<Result> Handle(ApproveManualVerificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
            return Result.Failure(new Error("Identity.NotFound", "Profile not found."));

        profile.CompleteManualVerification(true);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
