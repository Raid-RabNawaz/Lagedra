using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Entities;

/// <summary>
/// A rent + deposit proposal on an inquiry thread. Either party may propose;
/// the other accepts or counters. At most one Pending and one Accepted offer
/// exist per session (enforced by <see cref="Aggregates.InquirySession"/>).
/// </summary>
public sealed class InquiryOffer : Entity<Guid>
{
    public const int NoteMaxLength = 500;

    public Guid SessionId { get; private set; }
    public Guid ProposedByUserId { get; private set; }
    public InquiryOfferRole ProposedByRole { get; private set; }
    public long RentCents { get; private set; }
    public long DepositCents { get; private set; }
    public string? Note { get; private set; }
    public InquiryOfferStatus Status { get; private set; }
    public DateTime ProposedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public Guid? SupersedesOfferId { get; private set; }

    private InquiryOffer() { }

    internal static InquiryOffer Create(
        Guid sessionId,
        Guid proposedByUserId,
        InquiryOfferRole proposedByRole,
        long rentCents,
        long depositCents,
        string? note,
        Guid? supersedesOfferId = null)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }
        if (proposedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Proposer id is required.", nameof(proposedByUserId));
        }
        if (rentCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rentCents), "Rent must be greater than zero.");
        }
        if (depositCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depositCents), "Deposit cannot be negative.");
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > NoteMaxLength })
        {
            trimmedNote = trimmedNote[..NoteMaxLength];
        }

        return new InquiryOffer
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ProposedByUserId = proposedByUserId,
            ProposedByRole = proposedByRole,
            RentCents = rentCents,
            DepositCents = depositCents,
            Note = trimmedNote,
            Status = InquiryOfferStatus.Pending,
            ProposedAt = DateTime.UtcNow,
            RespondedAt = null,
            SupersedesOfferId = supersedesOfferId,
        };
    }

    internal void MarkAccepted(DateTime at)
    {
        if (Status != InquiryOfferStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept offer in status '{Status}'.");
        }

        Status = InquiryOfferStatus.Accepted;
        RespondedAt = at;
    }

    internal void MarkSuperseded(DateTime at)
    {
        if (Status != InquiryOfferStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot supersede offer in status '{Status}'.");
        }

        Status = InquiryOfferStatus.Superseded;
        RespondedAt = at;
    }

    internal void MarkWithdrawn(DateTime at)
    {
        if (Status is not (InquiryOfferStatus.Pending or InquiryOfferStatus.Accepted))
        {
            throw new InvalidOperationException($"Cannot withdraw offer in status '{Status}'.");
        }

        Status = InquiryOfferStatus.Withdrawn;
        RespondedAt = at;
    }
}
