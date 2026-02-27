using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;

public sealed class AbuseCase : AggregateRoot<Guid>
{
    public Guid SubjectUserId { get; private set; }
    public AbuseType AbuseType { get; private set; }
    public AbuseCaseStatus Status { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private AbuseCase() { }

    public static AbuseCase Open(Guid subjectUserId, AbuseType abuseType)
    {
        return new AbuseCase
        {
            Id = Guid.NewGuid(),
            SubjectUserId = subjectUserId,
            AbuseType = abuseType,
            Status = AbuseCaseStatus.Open,
            DetectedAt = DateTime.UtcNow
        };
    }

    public void MarkUnderReview()
    {
        if (Status != AbuseCaseStatus.Open)
        {
            throw new InvalidOperationException($"Cannot review case in status '{Status}'.");
        }

        Status = AbuseCaseStatus.UnderReview;
    }

    public void Resolve()
    {
        if (Status is not (AbuseCaseStatus.Open or AbuseCaseStatus.UnderReview))
        {
            throw new InvalidOperationException($"Cannot resolve case in status '{Status}'.");
        }

        Status = AbuseCaseStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Dismiss()
    {
        if (Status is not (AbuseCaseStatus.Open or AbuseCaseStatus.UnderReview))
        {
            throw new InvalidOperationException($"Cannot dismiss case in status '{Status}'.");
        }

        Status = AbuseCaseStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
    }
}
