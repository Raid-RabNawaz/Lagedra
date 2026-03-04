using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;

public sealed class CollusionPattern : Entity<Guid>
{
    public Guid AbuseCaseId { get; private set; }
    public Guid PartyAUserId { get; private set; }
    public Guid PartyBUserId { get; private set; }
    public int RepeatedDealCount { get; private set; }
    public DateTime FirstOccurrence { get; private set; }
    public DateTime LatestOccurrence { get; private set; }

    private CollusionPattern() { }

    public static CollusionPattern Create(
        Guid abuseCaseId,
        Guid partyAUserId,
        Guid partyBUserId,
        int repeatedDealCount,
        DateTime firstOccurrence,
        DateTime latestOccurrence)
    {
        return new CollusionPattern
        {
            Id = Guid.NewGuid(),
            AbuseCaseId = abuseCaseId,
            PartyAUserId = partyAUserId,
            PartyBUserId = partyBUserId,
            RepeatedDealCount = repeatedDealCount,
            FirstOccurrence = firstOccurrence,
            LatestOccurrence = latestOccurrence
        };
    }
}
