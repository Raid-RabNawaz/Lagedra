using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Entities;

public sealed class DecisionPenalty : Entity<Guid>
{
    public Guid CaseId { get; private set; }
    public Guid PartyUserId { get; private set; }
    public PenaltyType PenaltyType { get; private set; }
    public long? AmountCents { get; private set; }
    public string? Description { get; private set; }

    private DecisionPenalty() { }

    internal static DecisionPenalty Create(
        Guid caseId,
        Guid partyUserId,
        PenaltyType penaltyType,
        long? amountCents,
        string? description)
    {
        if (amountCents is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountCents));
        }

        return new DecisionPenalty
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            PartyUserId = partyUserId,
            PenaltyType = penaltyType,
            AmountCents = amountCents,
            Description = description?.Trim()
        };
    }
}
