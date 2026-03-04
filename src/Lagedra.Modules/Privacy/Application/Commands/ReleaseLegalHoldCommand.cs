using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Application.Commands;

public sealed record ReleaseLegalHoldCommand(Guid LegalHoldId) : IRequest<Result>;

public sealed class ReleaseLegalHoldCommandHandler(PrivacyDbContext dbContext)
    : IRequestHandler<ReleaseLegalHoldCommand, Result>
{
    public async Task<Result> Handle(
        ReleaseLegalHoldCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var legalHold = await dbContext.LegalHolds
            .FirstOrDefaultAsync(h => h.Id == request.LegalHoldId, cancellationToken)
            .ConfigureAwait(false);

        if (legalHold is null)
        {
            return Result.Failure(new Error("Privacy.LegalHold.NotFound", "Legal hold not found."));
        }

        legalHold.Release();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
