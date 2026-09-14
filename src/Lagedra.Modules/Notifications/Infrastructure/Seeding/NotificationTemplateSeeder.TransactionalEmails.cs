using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Domain.Enums;

namespace Lagedra.Modules.Notifications.Infrastructure.Seeding;

public static partial class NotificationTemplateSeeder
{
    private readonly record struct EmailSpec(
        string TemplateId,
        string Subject,
        string Heading,
        string Paragraph,
        string? CtaUrlKey = null,
        string? CtaLabel = null);

    /// <summary>
    /// Email rows for every NotifyUserCommand template that is not already
    /// in <see cref="BuildBaselineTemplates"/>. Existing admin edits are
    /// left untouched (upsert-by-TemplateId+Channel).
    /// </summary>
    private static IEnumerable<NotificationTemplate> BuildTransactionalEmailTemplates()
    {
        foreach (var spec in TransactionalEmailSpecs)
        {
            yield return SimpleEmail(spec);
        }
    }

    private static readonly EmailSpec[] TransactionalEmailSpecs =
    [
        new("application_approved",
            "Your booking was approved",
            "Your booking was approved",
            "Your host approved the booking. Open Lagedra to review the deal and complete the next steps."),
        new("application_rejected",
            "Your booking request was not accepted",
            "Your booking request was not accepted",
            "The host did not accept this booking request. You can send a new request or look at other listings."),
        new("application_expired",
            "Your booking request expired",
            "Your booking request expired",
            "The host did not respond in time, so this request expired. You can send a new request or explore other listings."),
        new("application_superseded",
            "Those dates are no longer available",
            "Those dates are no longer available",
            "The host confirmed another booking that overlaps your dates, so this request was closed. You can request different dates on the same listing."),
        new("booking_payment_failed",
            "Payment failed — action needed",
            "Payment failed — action needed",
            "Your host accepted the request, but we could not complete the payment ({reason}). Update your payment method in Lagedra to confirm the booking."),
        new("booking_payment_failed_host",
            "Booking is not active yet",
            "Booking is not active yet",
            "You accepted a request, but the guest's payment failed so the booking is not active yet. We asked them to update their payment method."),
        new("payment_confirmed",
            "Payment confirmed",
            "Payment confirmed",
            "Payment for this booking was confirmed. Insurance activation is the next step."),
        new("payment_disputed",
            "A payment was disputed",
            "A payment was disputed",
            "A payment dispute was opened on this booking: {reason}."),
        new("payment_dispute_resolved",
            "Payment dispute resolved",
            "Payment dispute resolved",
            "The payment dispute on this booking was resolved: {outcome}."),
        new("booking_cancelled",
            "Booking cancelled",
            "Booking cancelled",
            "{reason} {refundInfo}"),
        new("damage_claim_filed",
            "A damage claim was filed",
            "A damage claim was filed",
            "A damage claim of ${amount} was filed on this booking."),
        new("damage_claim_approved",
            "Damage claim approved",
            "Damage claim approved",
            "A damage claim of ${amount} was approved on this booking."),
        new("damage_claim_rejected",
            "Damage claim rejected",
            "Damage claim rejected",
            "A damage claim on this booking was rejected after review."),
        new("deposit_return_due",
            "Return the deposit",
            "Return the deposit",
            "Return the guest's deposit within 21 days, or itemize deductions with a damage photo."),
        new("deposit_receipt_due",
            "Confirm your deposit return",
            "Confirm your deposit return",
            "Please confirm that you received your deposit back, or report a problem from the deal page."),
        new("review_due",
            "Please leave a review",
            "Please leave a review",
            "Your stay is complete. Leaving a review helps the next guest and host."),
        new("review_reminder",
            "Reminder: leave a review",
            "Reminder: leave a review",
            "You still have a review waiting. It only takes a minute."),
        new("truth_surface_initiated",
            "Deal terms are ready to review",
            "Deal terms are ready to review",
            "The deal terms snapshot is ready. Open Lagedra to review and confirm."),
        new("truth_surface_confirmed",
            "Deal terms confirmed",
            "Deal terms confirmed",
            "Both parties confirmed the deal terms. The snapshot is now sealed."),
        new("truth_surface_superseded",
            "Deal terms were updated",
            "Deal terms were updated",
            "The deal terms were updated ({reason}). Please review and confirm the new snapshot."),
        new("inquiry_offer_accepted",
            "Offer accepted — {listingTitle}",
            "Offer accepted",
            "Terms were agreed for {listingTitle}. Those amounts will be used when the guest applies.",
            "threadUrl",
            "Open conversation"),
        new("inquiry_offer_proposed",
            "New offer on {listingTitle}",
            "A new offer is waiting",
            "There is a new offer on {listingTitle}. Review it in the conversation thread.",
            "threadUrl",
            "Open conversation"),
        new("inquiry_partner_added",
            "You were added to a conversation",
            "You were added to a conversation",
            "You were added to a listing conversation. Open Lagedra to review the thread."),
        new("inquiry_partner_question",
            "New question on a conversation",
            "New question on a conversation",
            "A new question was posted on a listing conversation you are part of."),
        new("listing_submitted_for_review",
            "Listing submitted for review",
            "Listing submitted for review",
            "Your listing was submitted and is waiting for review."),
        new("listing_published",
            "Your listing is live",
            "Your listing is live",
            "Your listing was published and is now visible to guests."),
        new("listing_denied",
            "Listing was not published",
            "Listing was not published",
            "Your listing was not published. Open Lagedra to see what to fix."),
        new("identity_verified",
            "Your identity is verified",
            "Your identity is verified",
            "Identity verification succeeded. You can continue booking or hosting."),
        new("identity_verification_failed",
            "Identity verification failed",
            "Identity verification failed",
            "Identity verification did not succeed: {reason}. You can try again from your profile."),
        new("verification_class_changed",
            "Your verification status changed",
            "Your verification status changed",
            "Your verification class was updated. Open Lagedra to see what that means for bookings."),
        new("insurance_status_changed",
            "Insurance status updated",
            "Insurance status updated",
            "Insurance status is now {newState} for this booking."),
        new("welcome",
            "Welcome to Lagedra",
            "Welcome to Lagedra",
            "Your account is ready. You can search stays, list a home, or finish verification from the app."),
        new("role_changed",
            "Your Lagedra role changed",
            "Your Lagedra role changed",
            "Your account role was updated. Sign in again if a page does not match what you expect."),
        new("account_restricted",
            "Your account was restricted",
            "Your account was restricted",
            "Your Lagedra account has a restriction. Open the app or contact support if you need details."),
        new("compliance_violation_created",
            "A compliance record was opened",
            "A compliance record was opened",
            "A compliance record was opened on your account. Open Lagedra to review it."),
        new("compliance_violation_resolved",
            "A compliance record was resolved",
            "A compliance record was resolved",
            "A compliance record on your account was marked resolved."),
        new("compliance_violation_escalated",
            "A compliance record was escalated",
            "A compliance record was escalated",
            "A compliance record on your account was escalated. Open Lagedra for next steps."),
        new("arbitration_case_filed",
            "An arbitration case was filed",
            "An arbitration case was filed",
            "An arbitration case was filed. Open Lagedra to review the case and add evidence."),
        new("arbitration_decision",
            "An arbitration decision is ready",
            "An arbitration decision is ready",
            "An arbitration decision is ready (tier {tier}). Open Lagedra to read it."),
        new("evidence_complete",
            "Evidence window closed",
            "Evidence window closed",
            "The evidence window closed. A decision is due {decisionDueAt}."),
        new("arbitration_case_closed",
            "Arbitration case closed",
            "Arbitration case closed",
            "Your arbitration case has been closed."),
        new("arbitration_case_appealed",
            "Arbitration case appealed",
            "Arbitration case appealed",
            "An arbitration case was appealed. Open Lagedra to review the appeal."),
        new("payment_failed",
            "A billing payment failed",
            "A billing payment failed",
            "A subscription or protocol payment failed. Update your billing details in Lagedra."),
    ];

    private static NotificationTemplate SimpleEmail(EmailSpec spec)
    {
        var ctaHtml = string.Empty;
        var ctaText = string.Empty;
        if (!string.IsNullOrWhiteSpace(spec.CtaUrlKey))
        {
            var href = "{" + spec.CtaUrlKey + "}";
            var label = spec.CtaLabel ?? "Open Lagedra";
            ctaHtml =
                "<p style=\"margin: 24px 0;\">"
                + "<a href=\"" + href + "\" "
                + "style=\"display: inline-block; background: #4141E5; color: #fff; padding: 12px 20px; border-radius: 10px; text-decoration: none; font-weight: 600;\">"
                + label
                + "</a></p>";
            ctaText = label + ": " + href + "\n\n";
        }

        var html =
            "<h2 style=\"margin-top: 0;\">" + spec.Heading + "</h2>"
            + "<p>" + spec.Paragraph + "</p>"
            + ctaHtml;

        var text = spec.Heading + "\n\n" + spec.Paragraph + "\n\n" + ctaText;

        return new NotificationTemplate(
            spec.TemplateId,
            NotificationChannel.Email,
            spec.Subject,
            html,
            text);
    }
}
