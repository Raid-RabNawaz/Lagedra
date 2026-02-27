using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record MarkEvidenceCompleteCommand(Guid CaseId) : IRequest<Result>;

public sealed class MarkEvidenceCompleteCommandHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<MarkEvidenceCompleteCommand, Result>
{
    public async Task<Result> Handle(MarkEvidenceCompleteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arbitrationCase = await dbContext.ArbitrationCases
            .Include(c => c.EvidenceSlots)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        arbitrationCase.MarkEvidenceComplete();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
