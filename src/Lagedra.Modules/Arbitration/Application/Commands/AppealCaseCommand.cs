using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AppealCaseCommand(
    Guid CaseId,
    Guid AppealedByUserId,
    string Reason) : IRequest<Result>;

public sealed class AppealCaseCommandHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<AppealCaseCommand, Result>
{
    public async Task<Result> Handle(AppealCaseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arbitrationCase = await dbContext.ArbitrationCases
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        arbitrationCase.Appeal(request.AppealedByUserId, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
