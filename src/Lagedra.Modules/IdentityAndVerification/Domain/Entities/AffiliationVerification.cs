using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Entities;

public sealed class AffiliationVerification : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string? OrganizationType { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public VerificationMethod VerificationMethod { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private AffiliationVerification() { }

    public static AffiliationVerification Create(
        Guid userId,
        string? organizationType,
        Guid? organizationId,
        VerificationMethod method)
    {
        return new AffiliationVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationType = organizationType,
            OrganizationId = organizationId,
            VerificationMethod = method,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkVerified()
    {
        if (VerifiedAt.HasValue)
        {
            throw new InvalidOperationException("Affiliation is already verified.");
        }

        VerifiedAt = DateTime.UtcNow;
        _domainEvents.Add(new AffiliationVerifiedEvent(Id, UserId, VerificationMethod, OrganizationType));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
