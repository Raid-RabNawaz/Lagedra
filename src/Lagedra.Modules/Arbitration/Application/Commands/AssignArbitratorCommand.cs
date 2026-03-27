using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AssignArbitratorCommand(
    Guid CaseId,
    Guid ArbitratorUserId,
    int ConcurrentCaseCount) : IRequest<Result>;

public sealed class AssignArbitratorCommandHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<AssignArbitratorCommand, Result>
{
    public async Task<Result> Handle(AssignArbitratorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arbitrationCase = await dbContext.ArbitrationCases
            .Include(c => c.ArbitratorAssignments)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        arbitrationCase.AssignArbitrator(request.ArbitratorUserId, request.ConcurrentCaseCount);

        var newAssignment = arbitrationCase.ArbitratorAssignments[^1];
        dbContext.Entry(newAssignment).State = EntityState.Added;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
