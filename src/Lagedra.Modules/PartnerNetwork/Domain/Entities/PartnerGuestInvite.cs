using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.PartnerNetwork.Domain.Entities;

/// <summary>
/// Append-only audit row recording one partner-driven guest invitation. One row is
/// written per call to <c>InvitePartnerGuestCommand</c>, regardless of whether the
/// underlying account was newly created or matched an existing user.
///
/// Implements <see cref="IAppendOnly"/> so it cannot be soft-deleted; this gives
/// support / compliance a permanent answer to "who created this account?".
/// </summary>
public sealed class PartnerGuestInvite : Entity<Guid>, IAppendOnly
{
    public Guid OrganizationId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public Guid InvitedUserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool WasUserJustCreated { get; private set; }
    public Guid? EndorsementId { get; private set; }
    public Guid? ListingId { get; private set; }
    public DateTime InvitedAt { get; private set; }

    private PartnerGuestInvite() { }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Email local-parts and domains are stored in canonical lowercase form (RFC 5321 §2.4 case-insensitive); ToUpperInvariant would invert the convention.")]
    public static PartnerGuestInvite Create(
        Guid organizationId,
        Guid invitedByUserId,
        Guid invitedUserId,
        string email,
        string fullName,
        bool wasUserJustCreated,
        Guid? endorsementId,
        Guid? listingId,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new PartnerGuestInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            InvitedByUserId = invitedByUserId,
            InvitedUserId = invitedUserId,
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            WasUserJustCreated = wasUserJustCreated,
            EndorsementId = endorsementId,
            ListingId = listingId,
            InvitedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
