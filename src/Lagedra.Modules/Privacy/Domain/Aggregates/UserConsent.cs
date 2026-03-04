using Lagedra.Modules.Privacy.Domain.Entities;
using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Aggregates;

public sealed class UserConsent : AggregateRoot<Guid>
{
    private readonly List<ConsentRecord> _consentRecords = [];

    public Guid UserId { get; private set; }
    public IReadOnlyList<ConsentRecord> ConsentRecords => _consentRecords.AsReadOnly();

    private UserConsent() { }

    public static UserConsent Create(Guid userId)
    {
        return new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
    }

    public ConsentRecord RecordConsent(ConsentType type, string ipAddress, string userAgent)
    {
        var existing = _consentRecords
            .FirstOrDefault(r => r.ConsentType == type && r.WithdrawnAt is null);

        if (existing is not null)
        {
            throw new InvalidOperationException($"Active consent of type '{type}' already exists.");
        }

        var record = ConsentRecord.Create(Id, type, ipAddress, userAgent);
        _consentRecords.Add(record);

        AddDomainEvent(new ConsentRecordedEvent(UserId, type, record.GrantedAt));

        return record;
    }

    public void WithdrawConsent(ConsentType type)
    {
        var record = _consentRecords
            .FirstOrDefault(r => r.ConsentType == type && r.WithdrawnAt is null);

        if (record is null)
        {
            throw new InvalidOperationException($"No active consent of type '{type}' to withdraw.");
        }

        record.Withdraw();
    }
}
