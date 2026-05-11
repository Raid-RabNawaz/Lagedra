using Lagedra.SharedKernel.Results;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;

public sealed record ResolveFraudFlagCommand(Guid FlagId) : IRequest<Result>;

public sealed class ResolveFraudFlagCommandHandler(IntegrityDbContext dbContext)
    : IRequestHandler<ResolveFraudFlagCommand, Result>
{
    public async Task<Result> Handle(ResolveFraudFlagCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flag = await dbContext.FraudFlags
            .FirstOrDefaultAsync(f => f.Id == request.FlagId, cancellationToken)
            .ConfigureAwait(false);

        if (flag is null)
            return Result.Failure(new Error("FraudFlag.NotFound", "Flag not found."));

        flag.IsDeleted = true;
        flag.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
