using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;

public sealed class FraudFlag : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public FraudFlagType FlagType { get; private set; }
    public Severity Severity { get; private set; }
    public DateTime FlaggedAt { get; private set; }

    private FraudFlag() { }

    public static FraudFlag Create(Guid userId, FraudFlagType flagType, Severity severity)
    {
        return new FraudFlag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FlagType = flagType,
            Severity = severity,
            FlaggedAt = DateTime.UtcNow
        };
    }
}
