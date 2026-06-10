using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AppealCaseCommand(
    Guid CaseId,
    ArbitrationUserContext Caller,
    string Reason) : IRequest<Result>;

public sealed class AppealCaseCommandHandler(
    ArbitrationDbContext dbContext,
    ArbitrationCaseAccessEvaluator accessEvaluator)
    : IRequestHandler<AppealCaseCommand, Result>
{
    public async Task<Result> Handle(AppealCaseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await accessEvaluator
            .RequireAsync(request.CaseId, request.Caller, CaseAccessLevel.Appeal, cancellationToken)
            .ConfigureAwait(false);

        if (!access.IsSuccess)
        {
            return Result.Failure(access.Error);
        }

        var arbitrationCase = await dbContext.ArbitrationCases
            .FirstAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        arbitrationCase.Appeal(request.Caller.UserId, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
