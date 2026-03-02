using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record CloseCaseCommand(Guid CaseId) : IRequest<Result>;

public sealed class CloseCaseCommandHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<CloseCaseCommand, Result>
{
    public async Task<Result> Handle(CloseCaseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arbitrationCase = await dbContext.ArbitrationCases
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        arbitrationCase.CloseCase();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
