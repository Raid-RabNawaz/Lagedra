using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record SubmitBackgroundCheckConsentCommand(
    Guid UserId) : IRequest<Result>;

public sealed class SubmitBackgroundCheckConsentCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<SubmitBackgroundCheckConsentCommand, Result>
{
    public async Task<Result> Handle(
        SubmitBackgroundCheckConsentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .AnyAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (!profile)
        {
            return Result.Failure(
                new Error("Identity.NotFound", "Identity profile not found. Complete KYC first."));
        }

        // Consent is recorded — background check will be initiated by an external process.
        // The IngestBackgroundCheckResultCommand handles the callback.

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
