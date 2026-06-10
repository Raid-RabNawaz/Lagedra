using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Services;

public sealed class ArbitrationCaseAccessEvaluator(
    ArbitrationDbContext dbContext,
    IDealApplicationStatusProvider dealProvider)
{
    public static readonly Error Forbidden = new(
        "Arbitration.Forbidden",
        "You do not have access to this arbitration case.");

    public static readonly Error CaseNotFound = new(
        "Arbitration.CaseNotFound",
        "Case not found.");

    public async Task<Result<(ArbitrationCase Case, DealParticipantsDto Participants)>> RequireAsync(
        Guid caseId,
        ArbitrationUserContext caller,
        CaseAccessLevel level,
        CancellationToken cancellationToken)
    {
        var arbitrationCase = await dbContext.ArbitrationCases
            .Include(c => c.ArbitratorAssignments)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result<(ArbitrationCase, DealParticipantsDto)>.Failure(CaseNotFound);
        }

        var participants = await dealProvider
            .GetParticipantsAsync(arbitrationCase.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (participants is null)
        {
            return Result<(ArbitrationCase, DealParticipantsDto)>.Failure(CaseNotFound);
        }

        if (!ArbitrationCaseAccess.Allows(level, caller, arbitrationCase, participants))
        {
            return Result<(ArbitrationCase, DealParticipantsDto)>.Failure(Forbidden);
        }

        return Result<(ArbitrationCase, DealParticipantsDto)>.Success((arbitrationCase, participants));
    }
}
