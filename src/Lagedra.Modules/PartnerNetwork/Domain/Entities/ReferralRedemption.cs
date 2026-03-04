using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.PartnerNetwork.Domain.Entities;

public sealed class ReferralRedemption : Entity<Guid>
{
    public Guid ReferralLinkId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid RedeemedByUserId { get; private set; }
    public DateTime RedeemedAt { get; private set; }

    private ReferralRedemption() { }

    public static ReferralRedemption Create(
        Guid referralLinkId,
        Guid organizationId,
        Guid redeemedByUserId,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new ReferralRedemption
        {
            Id = Guid.NewGuid(),
            ReferralLinkId = referralLinkId,
            OrganizationId = organizationId,
            RedeemedByUserId = redeemedByUserId,
            RedeemedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
