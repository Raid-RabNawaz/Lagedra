using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.PartnerNetwork.Domain.Aggregates;

public sealed class PartnerOrganization : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public PartnerOrganizationType OrganizationType { get; private set; }
    public PartnerOrganizationStatus Status { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public string? SuspensionReason { get; private set; }

    private PartnerOrganization() { }

    public static PartnerOrganization Create(
        string name,
        PartnerOrganizationType organizationType,
        string contactEmail,
        string? taxId,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new PartnerOrganization
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationType = organizationType,
            Status = PartnerOrganizationStatus.PendingVerification,
            ContactEmail = contactEmail,
            TaxId = taxId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Verify(Guid adminUserId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PartnerOrganizationStatus.PendingVerification)
        {
            throw new InvalidOperationException(
                $"Cannot verify organization in status '{Status}'.");
        }

        Status = PartnerOrganizationStatus.Verified;
        VerifiedAt = clock.UtcNow;
        VerifiedBy = adminUserId;
        UpdatedAt = clock.UtcNow;

        AddDomainEvent(new PartnerOrganizationVerifiedEvent(Id, Name, adminUserId));
    }

    public void Suspend(string reason, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == PartnerOrganizationStatus.Suspended)
        {
            return;
        }

        Status = PartnerOrganizationStatus.Suspended;
        SuspensionReason = reason;
        UpdatedAt = clock.UtcNow;

        AddDomainEvent(new PartnerOrganizationSuspendedEvent(Id, reason));
    }
}
