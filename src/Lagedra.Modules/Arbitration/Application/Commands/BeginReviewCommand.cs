using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record BeginReviewCommand(Guid CaseId, ArbitrationUserContext Caller) : IRequest<Result>;

public sealed class BeginReviewCommandHandler(
    ArbitrationDbContext dbContext,
    ArbitrationCaseAccessEvaluator accessEvaluator)
    : IRequestHandler<BeginReviewCommand, Result>
{
    public async Task<Result> Handle(BeginReviewCommand request, CancellationToken cancellationToken)
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
            .Include(c => c.ArbitratorAssignments)
            .FirstAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            arbitrationCase.BeginReview();
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("Arbitration.InvalidStatus", ex.Message));
        }
    }
}
