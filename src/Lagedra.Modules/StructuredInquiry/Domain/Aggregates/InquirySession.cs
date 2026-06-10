using Lagedra.Modules.StructuredInquiry.Domain.Entities;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Aggregates;

/// <summary>
/// Phase 17 — an inquiry now exists independently of a deal. A tenant
/// browsing a listing can start a thread (<see cref="CreateForListing"/>)
/// before they ever apply, and once they do apply, the same thread is
/// linked to the resulting deal via <see cref="LinkToDeal"/>. The legacy
/// <see cref="Create"/> factory remains for in-deal Q&amp;A surfaces and
/// for paths that still create a deal-scoped session up front.
/// </summary>
public sealed class InquirySession : AggregateRoot<Guid>
{
    private readonly List<InquiryQuestion> _questions = [];

    /// <summary>
    /// Always populated — every inquiry is anchored to a listing. Pre-Phase 17
    /// rows have this back-filled from <see cref="DealId"/> via the EF migration.
    /// </summary>
    public Guid ListingId { get; private set; }

    /// <summary>
    /// The tenant who owns this thread. Required for both pre-deal and deal-
    /// scoped sessions so authorization can be answered without a deal lookup.
    /// </summary>
    public Guid TenantUserId { get; private set; }

    /// <summary>
    /// The deal once the tenant has applied. Null while the inquiry is purely
    /// pre-booking — the <see cref="LinkToDeal"/> call is what attaches it.
    /// </summary>
    public Guid? DealId { get; private set; }

    public InquirySessionStatus Status { get; private set; }
    public DateTime? UnlockedByLandlordAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyList<InquiryQuestion> Questions => _questions.AsReadOnly();

    private InquirySession() { }

    /// <summary>
    /// Phase 17 — create a pre-booking inquiry session for a tenant who is
    /// asking questions about a listing without (yet) applying. Always opens
    /// the session: there's no deal to lock against, and the host opt-in lock
    /// flow only makes sense once a deal exists.
    /// </summary>
    public static InquirySession CreateForListing(
        Guid listingId,
        Guid tenantUserId,
        Guid landlordUserId)
    {
        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("Listing id is required.", nameof(listingId));
        }
        if (tenantUserId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantUserId));
        }
        if (landlordUserId == Guid.Empty)
        {
            throw new ArgumentException("Landlord id is required.", nameof(landlordUserId));
        }

        var session = new InquirySession
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            TenantUserId = tenantUserId,
            DealId = null,
            Status = InquirySessionStatus.Open,
            UnlockedByLandlordAt = DateTime.UtcNow,
        };

        session.AddDomainEvent(new Events.ListingInquiryStartedEvent(
            session.Id, listingId, landlordUserId, tenantUserId, DateTime.UtcNow));

        return session;
    }

    /// <summary>
    /// Create a new inquiry session bound to an existing deal. Defaults to
    /// <see cref="InquirySessionStatus.Open"/> as part of Phase 16
    /// (BookingFlow.V2): inquiries are open-by-default and the host may opt
    /// in to lock a thread via <see cref="Lock"/>. Pass an explicit
    /// <paramref name="initialStatus"/> to preserve legacy locked-first
    /// behaviour (e.g. when the V2 flag is off).
    /// </summary>
    public static InquirySession Create(
        Guid dealId,
        Guid listingId,
        Guid tenantUserId,
        InquirySessionStatus initialStatus = InquirySessionStatus.Open)
    {
        if (dealId == Guid.Empty)
        {
            throw new ArgumentException("Deal id is required.", nameof(dealId));
        }
        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("Listing id is required.", nameof(listingId));
        }
        if (tenantUserId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantUserId));
        }

        return new InquirySession
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            ListingId = listingId,
            TenantUserId = tenantUserId,
            Status = initialStatus,
            UnlockedByLandlordAt =
                initialStatus == InquirySessionStatus.Open ? DateTime.UtcNow : null,
        };
    }

    /// <summary>
    /// Phase 17 — attach a previously listing-scoped inquiry to the deal
    /// that was just created from it. Idempotent if the deal id matches;
    /// throws if the session is already linked to a different deal.
    /// </summary>
    public void LinkToDeal(Guid dealId)
    {
        if (dealId == Guid.Empty)
        {
            throw new ArgumentException("Deal id is required.", nameof(dealId));
        }

        if (DealId is not null)
        {
            if (DealId == dealId)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Inquiry is already linked to deal '{DealId}'; cannot relink to '{dealId}'.");
        }

        DealId = dealId;
    }

    public void Unlock()
    {
        if (Status != InquirySessionStatus.Locked)
        {
            throw new InvalidOperationException($"Cannot unlock session in status '{Status}'.");
        }

        Status = InquirySessionStatus.Open;
        UnlockedByLandlordAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Host opt-in: re-lock an open inquiry thread. Used when the host wants
    /// to gate further questions behind an explicit unlock approval flow.
    /// </summary>
    public void Lock()
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot lock session in status '{Status}'.");
        }

        Status = InquirySessionStatus.Locked;
        UnlockedByLandlordAt = null;
    }

    public InquiryQuestion AddQuestion(
        InquiryCategory category,
        Guid? predefinedQuestionId,
        string? customText = null,
        string? openQuestionText = null)
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot add questions to session in status '{Status}'.");
        }

        var question = InquiryQuestion.Create(
            Id, category, predefinedQuestionId, customText, openQuestionText);
        _questions.Add(question);
        return question;
    }

    public InquiryAnswer AddAnswer(Guid questionId, ResponseType responseType, string answerValue)
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot add answers to session in status '{Status}'.");
        }

        var question = _questions.FirstOrDefault(q => q.Id == questionId)
            ?? throw new InvalidOperationException($"Question '{questionId}' not found in this session.");

        var answer = InquiryAnswer.Create(questionId, responseType, answerValue);
        question.SetAnswer(answer);
        return answer;
    }

    public void Close()
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot close session in status '{Status}'.");
        }

        Status = InquirySessionStatus.Closed;
        ClosedAt = DateTime.UtcNow;

        // The InquiryClosedEvent's DealId is nullable as of Phase 17 — pre-
        // booking inquiries that get closed (e.g. tenant lost interest) still
        // raise the event; downstream handlers must tolerate a null DealId.
        AddDomainEvent(new InquiryClosedEvent(Id, DealId, ClosedAt.Value));
    }
}
