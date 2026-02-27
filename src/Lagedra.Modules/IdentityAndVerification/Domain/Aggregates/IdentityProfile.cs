using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;

public sealed class IdentityProfile : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public VerificationStatus Status { get; private set; }
    public VerificationClass VerificationClass { get; private set; }

    private IdentityProfile() { }

    public static IdentityProfile Create(Guid userId, string? firstName, string? lastName, DateTime? dateOfBirth)
    {
        return new IdentityProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Status = VerificationStatus.NotStarted,
            VerificationClass = VerificationClass.Low,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void StartVerification()
    {
        if (Status != VerificationStatus.NotStarted && Status != VerificationStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot start verification from status '{Status}'.");
        }

        Status = VerificationStatus.Pending;
    }

    public void Complete()
    {
        if (Status != VerificationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot complete verification from status '{Status}'.");
        }

        Status = VerificationStatus.Verified;
        AddDomainEvent(new IdentityVerifiedEvent(Id, UserId, DateTime.UtcNow));
    }

    public void Fail(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != VerificationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot fail verification from status '{Status}'.");
        }

        Status = VerificationStatus.Failed;
        AddDomainEvent(new IdentityVerificationFailedEvent(Id, UserId, reason));
    }

    public void RequireManualReview()
    {
        if (Status != VerificationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot require manual review from status '{Status}'.");
        }

        Status = VerificationStatus.ManualReviewRequired;
    }

    public void ChangeVerificationClass(VerificationClass newClass)
    {
        if (VerificationClass == newClass)
        {
            return;
        }

        var oldClass = VerificationClass;
        VerificationClass = newClass;
        AddDomainEvent(new VerificationClassChangedEvent(Id, UserId, oldClass, newClass));
    }

    public void UpdatePersonalInfo(string? firstName, string? lastName, DateTime? dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
    }
}
