using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Domain.Policies;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Services;

public sealed class ArbitratorAssignmentSelector(
    ArbitrationDbContext dbContext,
    IArbitratorPanelProvider panelProvider,
    IDealApplicationStatusProvider dealProvider)
{
    private static readonly HashSet<ArbitrationStatus> ActiveStatuses =
    [
        ArbitrationStatus.Filed,
        ArbitrationStatus.EvidencePending,
        ArbitrationStatus.EvidenceComplete,
        ArbitrationStatus.UnderReview,
        ArbitrationStatus.Appealed
    ];

    public async Task<(Guid ArbitratorUserId, int ActiveCaseCount)?> SelectForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var arbitrationCase = await dbContext.ArbitrationCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return null;
        }

        if (arbitrationCase.Status is ArbitrationStatus.Decided or ArbitrationStatus.Closed)
        {
            return null;
        }

        var alreadyAssigned = await dbContext.ArbitratorAssignments
            .AsNoTracking()
            .AnyAsync(a => a.CaseId == caseId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyAssigned)
        {
            return null;
        }

        var participants = await dealProvider
            .GetParticipantsAsync(arbitrationCase.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (participants is null)
        {
            return null;
        }

        var panel = await panelProvider.GetPanelMembersAsync(cancellationToken).ConfigureAwait(false);
        if (panel.Count == 0)
        {
            return null;
        }

        var caseloads = await GetActiveCaseloadsAsync(cancellationToken).ConfigureAwait(false);

        // Direct party conflict only (arbitrator must not be host or guest on this deal).
        var partyConflict = new HashSet<Guid>
        {
            participants.LandlordUserId,
            participants.TenantUserId
        };

        var candidates = panel
            .Select(m => new
            {
                m.UserId,
                ActiveCount = caseloads.GetValueOrDefault(m.UserId, 0)
            })
            .Where(x => !ArbitratorCaseloadPolicy.IsAtHardCap(x.ActiveCount))
            .Where(x => !partyConflict.Contains(x.UserId))
            .OrderBy(x => x.ActiveCount)
            .ThenBy(x => x.UserId)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var underSoft = candidates.Where(x => !ArbitratorCaseloadPolicy.IsOverSoftCap(x.ActiveCount)).ToList();
        var pick = (underSoft.Count > 0 ? underSoft : candidates)[0];
        return (pick.UserId, pick.ActiveCount);
    }

    public async Task<Dictionary<Guid, int>> GetActiveCaseloadsAsync(CancellationToken cancellationToken)
    {
        var rows = await (
            from a in dbContext.ArbitratorAssignments.AsNoTracking()
            join c in dbContext.ArbitrationCases.AsNoTracking() on a.CaseId equals c.Id
            where ActiveStatuses.Contains(c.Status)
            group a by a.ArbitratorUserId into g
            select new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(r => r.Key, r => r.Count);
    }
}
