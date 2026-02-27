using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AttachEvidenceCommand(
    Guid CaseId,
    string SlotType,
    Guid SubmittedBy,
    string FileReference) : IRequest<Result>;

public sealed class AttachEvidenceCommandHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<AttachEvidenceCommand, Result>
{
    public async Task<Result> Handle(AttachEvidenceCommand request, CancellationToken cancellationToken)
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

        arbitrationCase.AttachEvidence(request.SlotType, request.SubmittedBy, request.FileReference);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
