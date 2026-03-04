using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Entities;

public sealed class FraudFlag : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public DateTime RaisedAt { get; private set; }
    public DateTime SlaDeadline { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public bool IsEscalated { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private FraudFlag() { }

    public static FraudFlag Create(Guid userId, string reason, string source, TimeSpan slaDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var now = DateTime.UtcNow;
        var flag = new FraudFlag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = reason,
            Source = source,
            RaisedAt = now,
            SlaDeadline = now.Add(slaDuration),
            CreatedAt = now
        };

        flag._domainEvents.Add(new FraudFlagRaisedEvent(flag.Id, userId, reason, source));
        return flag;
    }

    public void Escalate()
    {
        if (IsEscalated)
        {
            return;
        }

        IsEscalated = true;
    }

    public void Resolve()
    {
        if (ResolvedAt.HasValue)
        {
            throw new InvalidOperationException("Fraud flag is already resolved.");
        }

        ResolvedAt = DateTime.UtcNow;
    }

    public bool IsPastSla() => !ResolvedAt.HasValue && DateTime.UtcNow > SlaDeadline;

    public void ClearDomainEvents() => _domainEvents.Clear();
}
