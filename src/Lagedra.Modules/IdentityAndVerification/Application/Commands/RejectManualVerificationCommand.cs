using Lagedra.SharedKernel.Results;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record RejectManualVerificationCommand(Guid ProfileId) : IRequest<Result>;

public sealed class RejectManualVerificationCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<RejectManualVerificationCommand, Result>
{
    public async Task<Result> Handle(RejectManualVerificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
            return Result.Failure(new Error("Identity.NotFound", "Profile not found."));

        profile.CompleteManualVerification(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
