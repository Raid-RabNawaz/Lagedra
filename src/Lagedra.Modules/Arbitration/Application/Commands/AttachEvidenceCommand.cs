using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Domain.Policies;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record AttachEvidenceCommand(
    Guid CaseId,
    ArbitrationUserContext Caller,
    string SlotType,
    Guid EvidenceManifestId) : IRequest<Result>;

public sealed class AttachEvidenceCommandHandler(
    ArbitrationDbContext dbContext,
    ArbitrationCaseAccessEvaluator accessEvaluator,
    IEvidenceManifestProvider evidenceProvider)
    : IRequestHandler<AttachEvidenceCommand, Result>
{
    public async Task<Result> Handle(AttachEvidenceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await accessEvaluator
            .RequireAsync(request.CaseId, request.Caller, CaseAccessLevel.AttachEvidence, cancellationToken)
            .ConfigureAwait(false);

        if (!access.IsSuccess)
        {
            return Result.Failure(access.Error);
        }

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
            .FirstAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        var partyAlreadySubmitted = arbitrationCase.EvidenceSlots
            .Any(s => s.SubmittedBy == request.Caller.UserId);

        var allowAppealResubmission =
            arbitrationCase.Status == ArbitrationStatus.Appealed && partyAlreadySubmitted;

        if (!partyAlreadySubmitted || allowAppealResubmission)
        {
            arbitrationCase.AttachEvidence(request.SlotType, request.Caller.UserId, request.EvidenceManifestId);
            var newSlot = arbitrationCase.EvidenceSlots[^1];
            dbContext.Entry(newSlot).State = EntityState.Added;
        }
        else if (partyAlreadySubmitted)
        {
            return Result.Failure(new Error(
                "Arbitration.EvidenceAlreadySubmitted",
                "You have already submitted evidence for this case."));
        }

        if (arbitrationCase.Status is ArbitrationStatus.Filed or ArbitrationStatus.EvidencePending
            && EvidenceMinimumThresholdPolicy.IsSatisfied(
                arbitrationCase.Category,
                arbitrationCase.EvidenceSlots.Count))
        {
            try
            {
                arbitrationCase.MarkEvidenceComplete();
            }
            catch (InvalidOperationException)
            {
                // Threshold may have been satisfied before save; ignore race.
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
