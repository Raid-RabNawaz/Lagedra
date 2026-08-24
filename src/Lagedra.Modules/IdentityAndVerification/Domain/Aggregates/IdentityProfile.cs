using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration.Events;

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
            DateOfBirth = AsUtcDate(dateOfBirth),
            Status = VerificationStatus.NotStarted,
            VerificationClass = VerificationClass.Low,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// A date of birth is a calendar date: request bodies deserialize it with
    /// Kind=Unspecified, which Npgsql refuses to write to a timestamptz
    /// column ("only UTC is supported") — that 500'd every manual-KYC submit
    /// that included a DOB. Anchor the date at midnight UTC regardless of
    /// the incoming Kind.
    /// </summary>
    private static DateTime? AsUtcDate(DateTime? value) =>
        value is { } v ? DateTime.SpecifyKind(v.Date, DateTimeKind.Utc) : null;

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

    public void CompleteManualVerification(bool approved)
    {
        if (Status != VerificationStatus.ManualReviewRequired)
        {
            throw new InvalidOperationException(
                $"Cannot complete manual verification from status '{Status}'.");
        }

        if (approved)
        {
            Status = VerificationStatus.Verified;
            AddDomainEvent(new IdentityVerifiedEvent(Id, UserId, DateTime.UtcNow));
        }
        else
        {
            Status = VerificationStatus.Failed;
            AddDomainEvent(new IdentityVerificationFailedEvent(Id, UserId, "Manual verification rejected"));
        }
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
        DateOfBirth = AsUtcDate(dateOfBirth);
    }
}
