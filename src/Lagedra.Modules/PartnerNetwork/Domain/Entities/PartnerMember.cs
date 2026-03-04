using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.PartnerNetwork.Domain.Entities;

public sealed class PartnerMember : Entity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public PartnerMemberRole MemberRole { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public Guid? InvitedBy { get; private set; }

    private PartnerMember() { }

    public static PartnerMember Create(
        Guid organizationId,
        Guid userId,
        PartnerMemberRole role,
        Guid? invitedBy,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new PartnerMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            MemberRole = role,
            JoinedAt = now,
            InvitedBy = invitedBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
