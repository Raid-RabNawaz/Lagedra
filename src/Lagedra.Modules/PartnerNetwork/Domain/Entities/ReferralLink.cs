using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.PartnerNetwork.Domain.Entities;

public sealed class ReferralLink : Entity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public int? MaxUses { get; private set; }
    public int UsageCount { get; private set; }
    public bool IsActive { get; private set; }

    private ReferralLink() { }

    public static ReferralLink Create(
        Guid organizationId,
        string code,
        Guid createdByUserId,
        DateTime? expiresAt,
        int? maxUses,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new ReferralLink
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = code,
            CreatedByUserId = createdByUserId,
            ExpiresAt = expiresAt,
            MaxUses = maxUses,
            UsageCount = 0,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Redeem(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (!IsActive)
        {
            throw new InvalidOperationException("Referral link is no longer active.");
        }

        if (ExpiresAt.HasValue && clock.UtcNow > ExpiresAt.Value)
        {
            throw new InvalidOperationException("Referral link has expired.");
        }

        if (MaxUses.HasValue && UsageCount >= MaxUses.Value)
        {
            throw new InvalidOperationException("Referral link has reached maximum uses.");
        }

        UsageCount++;
        UpdatedAt = clock.UtcNow;
    }

    public void Deactivate(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        IsActive = false;
        UpdatedAt = clock.UtcNow;
    }
}
