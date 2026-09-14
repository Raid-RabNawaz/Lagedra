using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record ResolveIdentityFraudFlagCommand(Guid FlagId) : IRequest<Result>;

public sealed class ResolveIdentityFraudFlagCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<ResolveIdentityFraudFlagCommand, Result>
{
    public async Task<Result> Handle(
        ResolveIdentityFraudFlagCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flag = await dbContext.FraudFlags
            .FirstOrDefaultAsync(f => f.Id == request.FlagId, cancellationToken)
            .ConfigureAwait(false);

        if (flag is null)
        {
            return Result.Failure(new Error("FraudFlag.NotFound", "Flag not found."));
        }

        if (flag.ResolvedAt is not null)
        {
            return Result.Success();
        }

        flag.Resolve();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
