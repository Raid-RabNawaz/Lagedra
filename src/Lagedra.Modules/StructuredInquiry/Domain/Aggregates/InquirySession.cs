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
    private readonly List<InquiryOffer> _offers = [];

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

    /// <summary>
    /// Optional endorsed partner organization invited into this thread.
    /// At most one partner org per session.
    /// </summary>
    public Guid? PartnerOrganizationId { get; private set; }

    public DateTime? PartnerAddedAt { get; private set; }
    public Guid? PartnerAddedByUserId { get; private set; }

    public InquirySessionStatus Status { get; private set; }
    public DateTime? UnlockedByLandlordAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyList<InquiryQuestion> Questions => _questions.AsReadOnly();
    public IReadOnlyList<InquiryOffer> Offers => _offers.AsReadOnly();

    public InquiryOffer? AcceptedOffer =>
        _offers.FirstOrDefault(o => o.Status == InquiryOfferStatus.Accepted);

    public InquiryOffer? PendingOffer =>
        _offers.FirstOrDefault(o => o.Status == InquiryOfferStatus.Pending);

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

    /// <summary>
    /// Attach a partner organization to this pre-deal open thread.
    /// Authorization (endorsement / membership) is enforced at the command layer.
    /// </summary>
    public void AddPartner(
        Guid organizationId,
        Guid addedByUserId,
        Guid landlordUserId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization id is required.", nameof(organizationId));
        }
        if (addedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Added-by user id is required.", nameof(addedByUserId));
        }

        EnsurePartnerMutable();

        if (PartnerOrganizationId is not null)
        {
            if (PartnerOrganizationId == organizationId)
            {
                return;
            }

            throw new InvalidOperationException(
                "A partner organization is already attached to this inquiry.");
        }

        var now = DateTime.UtcNow;
        PartnerOrganizationId = organizationId;
        PartnerAddedAt = now;
        PartnerAddedByUserId = addedByUserId;

        AddDomainEvent(new InquiryPartnerAddedEvent(
            Id, ListingId, TenantUserId, organizationId, addedByUserId, landlordUserId, now));
    }

    /// <summary>
    /// Remove the attached partner while the session is still pre-deal and Open.
    /// </summary>
    public void RemovePartner(Guid removedByUserId)
    {
        if (removedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Removed-by user id is required.", nameof(removedByUserId));
        }

        EnsurePartnerMutable();

        if (PartnerOrganizationId is null)
        {
            throw new InvalidOperationException("No partner is attached to this inquiry.");
        }

        PartnerOrganizationId = null;
        PartnerAddedAt = null;
        PartnerAddedByUserId = null;
    }

    private void EnsurePartnerMutable()
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException(
                $"Cannot change partner on a session in status '{Status}'.");
        }

        if (DealId is not null)
        {
            throw new InvalidOperationException(
                "Partner cannot be changed once the inquiry is linked to a deal.");
        }
    }

    /// <summary>
    /// Propose a rent + deposit offer. Supersedes any existing Pending offer.
    /// Blocked once an offer is Accepted, once a deal is linked, or when the
    /// session is not Open.
    /// </summary>
    public InquiryOffer ProposeOffer(
        Guid proposedByUserId,
        InquiryOfferRole proposedByRole,
        long rentCents,
        long depositCents,
        long maxDepositCents,
        Guid landlordUserId,
        string? note = null)
    {
        EnsureOffersMutable();

        if (AcceptedOffer is not null)
        {
            throw new InvalidOperationException(
                "An offer has already been accepted. Withdraw it before proposing a new one.");
        }

        ValidateOfferAmounts(rentCents, depositCents, maxDepositCents);

        var now = DateTime.UtcNow;
        Guid? supersedesId = null;
        if (PendingOffer is { } pending)
        {
            pending.MarkSuperseded(now);
            supersedesId = pending.Id;
        }

        var offer = InquiryOffer.Create(
            Id, proposedByUserId, proposedByRole, rentCents, depositCents, note, supersedesId);
        _offers.Add(offer);

        AddDomainEvent(new InquiryOfferProposedEvent(
            Id, offer.Id, ListingId, TenantUserId, landlordUserId,
            proposedByUserId, rentCents, depositCents, now));

        return offer;
    }

    /// <summary>
    /// Accept a Pending offer. Only the counterparty (not the proposer) may accept.
    /// </summary>
    public InquiryOffer AcceptOffer(
        Guid offerId,
        Guid acceptedByUserId,
        Guid landlordUserId)
    {
        EnsureOffersMutable();

        var offer = _offers.FirstOrDefault(o => o.Id == offerId)
            ?? throw new InvalidOperationException($"Offer '{offerId}' not found in this session.");

        if (offer.Status != InquiryOfferStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept offer in status '{offer.Status}'.");
        }

        if (offer.ProposedByUserId == acceptedByUserId)
        {
            throw new InvalidOperationException("You cannot accept your own offer.");
        }

        if (AcceptedOffer is not null)
        {
            throw new InvalidOperationException("An offer has already been accepted on this thread.");
        }

        var now = DateTime.UtcNow;
        offer.MarkAccepted(now);

        AddDomainEvent(new InquiryOfferAcceptedEvent(
            Id, offer.Id, ListingId, TenantUserId, landlordUserId,
            acceptedByUserId, offer.RentCents, offer.DepositCents, now));

        return offer;
    }

    /// <summary>
    /// Counter a Pending offer with new numbers. Supersedes the countered offer
    /// and creates a new Pending proposal from the countering party.
    /// </summary>
    public InquiryOffer CounterOffer(
        Guid offerId,
        Guid counteredByUserId,
        InquiryOfferRole counteredByRole,
        long rentCents,
        long depositCents,
        long maxDepositCents,
        Guid landlordUserId,
        string? note = null)
    {
        EnsureOffersMutable();

        if (AcceptedOffer is not null)
        {
            throw new InvalidOperationException(
                "An offer has already been accepted. Withdraw it before countering.");
        }

        var existing = _offers.FirstOrDefault(o => o.Id == offerId)
            ?? throw new InvalidOperationException($"Offer '{offerId}' not found in this session.");

        if (existing.Status != InquiryOfferStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot counter offer in status '{existing.Status}'.");
        }

        if (existing.ProposedByUserId == counteredByUserId)
        {
            throw new InvalidOperationException("You cannot counter your own offer.");
        }

        ValidateOfferAmounts(rentCents, depositCents, maxDepositCents);

        var now = DateTime.UtcNow;
        existing.MarkSuperseded(now);

        var offer = InquiryOffer.Create(
            Id, counteredByUserId, counteredByRole, rentCents, depositCents, note, existing.Id);
        _offers.Add(offer);

        AddDomainEvent(new InquiryOfferProposedEvent(
            Id, offer.Id, ListingId, TenantUserId, landlordUserId,
            counteredByUserId, rentCents, depositCents, now));

        return offer;
    }

    /// <summary>
    /// Withdraw the Accepted offer (or a Pending offer) while the session is
    /// still pre-deal. Either party may withdraw.
    /// </summary>
    public InquiryOffer WithdrawOffer(Guid? offerId, Guid withdrawnByUserId)
    {
        EnsureOffersMutable();

        InquiryOffer offer;
        if (offerId is { } id)
        {
            offer = _offers.FirstOrDefault(o => o.Id == id)
                ?? throw new InvalidOperationException($"Offer '{id}' not found in this session.");
        }
        else
        {
            offer = AcceptedOffer
                ?? PendingOffer
                ?? throw new InvalidOperationException("No offer available to withdraw.");
        }

        if (offer.Status is not (InquiryOfferStatus.Pending or InquiryOfferStatus.Accepted))
        {
            throw new InvalidOperationException($"Cannot withdraw offer in status '{offer.Status}'.");
        }

        // Silence unused — either party may withdraw; caller auth is at command layer.
        _ = withdrawnByUserId;

        offer.MarkWithdrawn(DateTime.UtcNow);
        return offer;
    }

    private void EnsureOffersMutable()
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException(
                $"Cannot change offers on a session in status '{Status}'.");
        }

        if (DealId is not null)
        {
            throw new InvalidOperationException(
                "Offers are frozen once the inquiry is linked to a deal.");
        }
    }

    private static void ValidateOfferAmounts(long rentCents, long depositCents, long maxDepositCents)
    {
        if (rentCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rentCents), "Rent must be greater than zero.");
        }

        if (depositCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depositCents), "Deposit cannot be negative.");
        }

        if (maxDepositCents >= 0 && depositCents > maxDepositCents)
        {
            throw new ArgumentOutOfRangeException(
                nameof(depositCents),
                $"Deposit cannot exceed the listing maximum of {maxDepositCents} cents.");
        }
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
        string? openQuestionText = null,
        Guid? submittedByUserId = null,
        InquiryQuestionAuthorRole submittedByRole = InquiryQuestionAuthorRole.Tenant,
        Guid? landlordUserId = null)
    {
        if (Status != InquirySessionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot add questions to session in status '{Status}'.");
        }

        if (submittedByRole == InquiryQuestionAuthorRole.Partner
            && PartnerOrganizationId is null)
        {
            throw new InvalidOperationException(
                "Cannot submit a partner question when no partner is attached.");
        }

        var question = InquiryQuestion.Create(
            Id,
            category,
            predefinedQuestionId,
            customText,
            openQuestionText,
            submittedByUserId,
            submittedByRole);
        _questions.Add(question);

        if (submittedByRole == InquiryQuestionAuthorRole.Partner
            && PartnerOrganizationId is { } orgId
            && submittedByUserId is { } byUser
            && landlordUserId is { } landlord)
        {
            AddDomainEvent(new InquiryPartnerQuestionSubmittedEvent(
                Id, question.Id, ListingId, TenantUserId, orgId, byUser, landlord, question.SubmittedAt));
        }

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
