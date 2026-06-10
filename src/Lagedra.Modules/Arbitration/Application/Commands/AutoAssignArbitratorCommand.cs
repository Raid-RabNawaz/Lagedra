using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AutoAssignArbitratorCommand(Guid CaseId) : IRequest<Result<Guid>>;

public sealed class AutoAssignArbitratorCommandHandler(
    ArbitratorAssignmentSelector assignmentSelector,
    IMediator mediator)
    : IRequestHandler<AutoAssignArbitratorCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AutoAssignArbitratorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var selection = await assignmentSelector
            .SelectForCaseAsync(request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (selection is null)
        {
            return Result<Guid>.Failure(new Error(
                "Arbitration.NoArbitratorAvailable",
                "No eligible arbitrator found. Ensure at least one active Arbitrator user exists, the case is not already assigned, caps are not exceeded, and the arbitrator is not a party on the deal."));
        }

        var assignResult = await mediator.Send(
            new AssignArbitratorCommand(
                request.CaseId,
                selection.Value.ArbitratorUserId,
                selection.Value.ActiveCaseCount + 1),
            cancellationToken).ConfigureAwait(false);

        return assignResult.IsSuccess
            ? Result<Guid>.Success(selection.Value.ArbitratorUserId)
            : Result<Guid>.Failure(assignResult.Error);
    }
}
