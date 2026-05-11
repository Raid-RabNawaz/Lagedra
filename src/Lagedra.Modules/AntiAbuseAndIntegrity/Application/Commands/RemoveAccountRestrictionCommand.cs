using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;

public sealed record RemoveAccountRestrictionCommand(Guid RestrictionId) : IRequest<Result>;

public sealed class RemoveAccountRestrictionCommandHandler(IntegrityDbContext dbContext)
    : IRequestHandler<RemoveAccountRestrictionCommand, Result>
{
    public async Task<Result> Handle(
        RemoveAccountRestrictionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restriction = await dbContext.AccountRestrictions
            .FirstOrDefaultAsync(r => r.Id == request.RestrictionId, cancellationToken)
            .ConfigureAwait(false);

        if (restriction is null)
            return Result.Failure(new Error("Restriction.NotFound", "Account restriction not found."));

        dbContext.AccountRestrictions.Remove(restriction);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
