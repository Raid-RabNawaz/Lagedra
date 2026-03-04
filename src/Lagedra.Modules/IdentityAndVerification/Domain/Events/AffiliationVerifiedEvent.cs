using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Events;

public sealed record AffiliationVerifiedEvent(
    Guid VerificationId,
    Guid UserId,
    VerificationMethod Method,
    string? OrganizationType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
