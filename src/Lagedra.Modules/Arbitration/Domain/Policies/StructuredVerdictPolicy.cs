using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Domain.Policies;

public static class StructuredVerdictPolicy
{
    public static void Validate(
        bool isStructured,
        DecisionOutcome? outcome,
        DecisionSeverity? severity,
        IReadOnlyList<(Guid PartyUserId, PenaltyType Type, long? AmountCents)> penalties,
        Guid landlordUserId,
        Guid tenantUserId)
    {
        if (!isStructured)
        {
            return;
        }

        if (outcome is null || severity is null)
        {
            throw new InvalidOperationException("Structured verdicts require outcome and severity.");
        }

        var resolvedOutcome = outcome.Value;
        var resolvedSeverity = severity.Value;

        var validParties = new HashSet<Guid> { landlordUserId, tenantUserId };

        foreach (var penalty in penalties)
        {
            if (!validParties.Contains(penalty.PartyUserId))
            {
                throw new InvalidOperationException("Penalty party must be the landlord or tenant on this deal.");
            }

            if (PenaltyTypeRules.RequiresAmount(penalty.Type)
                && (!penalty.AmountCents.HasValue || penalty.AmountCents.Value <= 0))
            {
                throw new InvalidOperationException(
                    $"Penalty type '{penalty.Type}' requires a positive amount in cents.");
            }
        }

        switch (resolvedOutcome)
        {
            case DecisionOutcome.LandlordFavored:
                EnsureHasPenaltyFor(penalties, tenantUserId, resolvedSeverity);
                break;
            case DecisionOutcome.TenantFavored:
                EnsureHasPenaltyFor(penalties, landlordUserId, resolvedSeverity);
                break;
            case DecisionOutcome.SharedFault:
                if (resolvedSeverity != DecisionSeverity.Low)
                {
                    EnsureHasPenaltyFor(penalties, landlordUserId, resolvedSeverity);
                    EnsureHasPenaltyFor(penalties, tenantUserId, resolvedSeverity);
                }
                break;
            case DecisionOutcome.Dismissed:
                break;
        }
    }

    private static void EnsureHasPenaltyFor(
        IReadOnlyList<(Guid PartyUserId, PenaltyType Type, long? AmountCents)> penalties,
        Guid partyUserId,
        DecisionSeverity severity)
    {
        if (severity == DecisionSeverity.Low)
        {
            return;
        }

        if (!penalties.Any(p => p.PartyUserId == partyUserId))
        {
            throw new InvalidOperationException(
                $"Outcome requires at least one penalty for party '{partyUserId}' at {severity} severity (or lower severity).");
        }
    }
}
