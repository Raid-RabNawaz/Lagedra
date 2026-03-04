using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;

public sealed class VerificationCase : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string? ExternalInquiryId { get; private set; }
    public VerificationStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private VerificationCase() { }

    public static VerificationCase Create(Guid userId, string? externalInquiryId = null)
    {
        return new VerificationCase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExternalInquiryId = externalInquiryId,
            Status = VerificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignInquiry(string externalInquiryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalInquiryId);
        ExternalInquiryId = externalInquiryId;
    }

    public void MarkCompleted()
    {
        if (Status != VerificationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot complete case from status '{Status}'.");
        }

        Status = VerificationStatus.Verified;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status != VerificationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot fail case from status '{Status}'.");
        }

        Status = VerificationStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}
