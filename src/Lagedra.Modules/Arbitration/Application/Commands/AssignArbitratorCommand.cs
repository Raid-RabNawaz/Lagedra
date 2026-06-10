using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Domain.Policies;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AssignArbitratorCommand(
    Guid CaseId,
    Guid ArbitratorUserId,
    int? ConcurrentCaseCount) : IRequest<Result>;

public sealed class AssignArbitratorCommandHandler(
    ArbitrationDbContext dbContext,
    ArbitratorAssignmentSelector assignmentSelector,
    IArbitratorPanelProvider panelProvider)
    : IRequestHandler<AssignArbitratorCommand, Result>
{
    public async Task<Result> Handle(AssignArbitratorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var panel = await panelProvider.GetPanelMembersAsync(cancellationToken).ConfigureAwait(false);
        if (!panel.Any(m => m.UserId == request.ArbitratorUserId))
        {
            return Result.Failure(new Error("Arbitration.InvalidArbitrator", "User is not on the arbitrator panel."));
        }

        var caseloads = await assignmentSelector.GetActiveCaseloadsAsync(cancellationToken).ConfigureAwait(false);
        var activeCount = caseloads.GetValueOrDefault(request.ArbitratorUserId, 0);
        if (ArbitratorCaseloadPolicy.IsAtHardCap(activeCount))
        {
            return Result.Failure(new Error(
                "Arbitration.CaseloadHardCap",
                $"Arbitrator is at the hard cap ({ArbitratorCaseloadPolicy.HardCap} active cases)."));
        }

        var arbitrationCase = await dbContext.ArbitrationCases
            .Include(c => c.ArbitratorAssignments)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        if (arbitrationCase.ArbitratorAssignments.Count > 0)
        {
            return Result.Failure(new Error(
                "Arbitration.AlreadyAssigned",
                "This case already has an assigned arbitrator."));
        }

        if (arbitrationCase.Status is Domain.Enums.ArbitrationStatus.Decided
            or Domain.Enums.ArbitrationStatus.Closed)
        {
            return Result.Failure(new Error(
                "Arbitration.InvalidStatus",
                $"Cannot assign an arbitrator while the case is in status '{arbitrationCase.Status}'."));
        }

        var concurrent = request.ConcurrentCaseCount ?? activeCount + 1;
        arbitrationCase.AssignArbitrator(request.ArbitratorUserId, concurrent);

        var newAssignment = arbitrationCase.ArbitratorAssignments[^1];
        dbContext.Entry(newAssignment).State = EntityState.Added;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
