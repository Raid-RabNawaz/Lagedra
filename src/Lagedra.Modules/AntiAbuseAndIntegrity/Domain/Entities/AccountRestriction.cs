using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;

public sealed class AccountRestriction : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public RestrictionLevel RestrictionLevel { get; private set; }
    public DateTime AppliedAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    private AccountRestriction() { }

    public static AccountRestriction Apply(
        Guid userId,
        RestrictionLevel restrictionLevel,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new AccountRestriction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestrictionLevel = restrictionLevel,
            AppliedAt = DateTime.UtcNow,
            Reason = reason
        };
    }
}
