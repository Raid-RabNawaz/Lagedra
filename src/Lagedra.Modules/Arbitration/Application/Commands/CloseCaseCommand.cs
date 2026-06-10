using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record CloseCaseCommand(Guid CaseId, ArbitrationUserContext Caller) : IRequest<Result>;

public sealed class CloseCaseCommandHandler(
    ArbitrationDbContext dbContext,
    ArbitrationCaseAccessEvaluator accessEvaluator)
    : IRequestHandler<CloseCaseCommand, Result>
{
    public async Task<Result> Handle(CloseCaseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await accessEvaluator
            .RequireAsync(request.CaseId, request.Caller, CaseAccessLevel.DecideOrClose, cancellationToken)
            .ConfigureAwait(false);

        if (!access.IsSuccess)
        {
            return Result.Failure(access.Error);
        }

        var arbitrationCase = await dbContext.ArbitrationCases
            .FirstAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        arbitrationCase.CloseCase();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
