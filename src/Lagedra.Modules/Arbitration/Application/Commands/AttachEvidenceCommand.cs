using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AttachEvidenceCommand(
    Guid CaseId,
    string SlotType,
    Guid SubmittedBy,
    Guid EvidenceManifestId) : IRequest<Result>;

public sealed class AttachEvidenceCommandHandler(
    ArbitrationDbContext dbContext,
    IEvidenceManifestProvider evidenceProvider)
    : IRequestHandler<AttachEvidenceCommand, Result>
{
    public async Task<Result> Handle(AttachEvidenceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var isSealed = await evidenceProvider
            .ExistsAndIsSealedAsync(request.EvidenceManifestId, cancellationToken)
            .ConfigureAwait(false);

        if (!isSealed)
        {
            return Result.Failure(new Error(
                "Arbitration.ManifestNotSealed",
                "Evidence manifest must exist and be sealed before attaching to an arbitration case."));
        }

        var arbitrationCase = await dbContext.ArbitrationCases
            .Include(c => c.EvidenceSlots)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result.Failure(new Error("Arbitration.CaseNotFound", "Case not found."));
        }

        arbitrationCase.AttachEvidence(request.SlotType, request.SubmittedBy, request.EvidenceManifestId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
