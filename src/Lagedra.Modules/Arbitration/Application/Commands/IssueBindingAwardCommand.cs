using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record IssueBindingAwardCommand(
    Guid CaseId,
    string DecisionSummary,
    decimal AwardAmount) : IRequest<Result<DecisionDto>>;

public sealed class IssueBindingAwardCommandHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<IssueBindingAwardCommand, Result<DecisionDto>>
{
    public async Task<Result<DecisionDto>> Handle(IssueBindingAwardCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arbitrationCase = await dbContext.ArbitrationCases
            .Include(c => c.ArbitratorAssignments)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result<DecisionDto>.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        arbitrationCase.IssueDecision(request.DecisionSummary, request.AwardAmount);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DecisionDto>.Success(new DecisionDto(
            arbitrationCase.DecisionSummary!,
            arbitrationCase.AwardAmount,
            arbitrationCase.DecidedAt!.Value));
    }
}
