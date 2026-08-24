using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Notifications.Infrastructure.Seeding;

/// <summary>
/// Seeds the minimum set of email + SMS templates Phase 16 / Twilio SMS depend on.
///
/// Seeding is upsert-by-(TemplateId, Channel): existing rows with the
/// same template id + channel are left untouched so an admin can override
/// the body without their changes being clobbered on the next deploy.
/// </summary>
public static partial class NotificationTemplateSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            await SeedCoreAsync(scope.ServiceProvider, ct).ConfigureAwait(false);
        }
    }

    private static async Task SeedCoreAsync(IServiceProvider scopedProvider, CancellationToken ct)
    {
        var dbContext = scopedProvider.GetRequiredService<NotificationDbContext>();
        var logger = scopedProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("NotificationTemplateSeeder");

        var templates = BuildBaselineTemplates().Concat(BuildSmsTemplates()).ToList();

        var existingKeys = await dbContext.Templates
            .Select(t => new { t.TemplateId, t.Channel })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existingSet = new HashSet<(string, NotificationChannel)>(
            existingKeys.Select(k => (k.TemplateId, k.Channel)));

        var added = 0;
        foreach (var template in templates)
        {
            if (existingSet.Contains((template.TemplateId, template.Channel)))
            {
                continue;
            }

            dbContext.Templates.Add(template);
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSeeded(logger, added);
        }
    }

    private static IEnumerable<NotificationTemplate> BuildBaselineTemplates()
    {
        // Raw-string literals (no `$` prefix) so the `{placeholder}`
        // tokens reach the template renderer verbatim — they're filled in
        // by NotifyUserCommand's payload dictionary at send time.
        const string applicationSubmittedHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">New booking application</h2>
              <p>A guest has submitted a booking application for <strong>{listingTitle}</strong>.</p>
              <p style="margin: 24px 0;">
                <a href="{approveUrl}"
                   style="display: inline-block; background: #111; color: #fff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 600;">
                  Approve booking
                </a>
              </p>
              <p style="font-size: 13px; color: #666;">
                The link is valid for {approveTokenTtlHours} hours and is single-use.
                You can also review and respond from your inbox at
                <a href="{frontendUrl}/app/applications">Lagedra</a>.
              </p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                This email was sent automatically by Lagedra. If you weren't expecting it,
                you can safely ignore it.
              </p>
            </body>
            </html>
            """;

        const string applicationSubmittedText = """
            New booking application

            A guest has submitted a booking application for {listingTitle}.

            Approve here: {approveUrl}

            The link is valid for {approveTokenTtlHours} hours and is single-use.
            You can also review and respond from your inbox at {frontendUrl}/app/applications.
            """;

        yield return new NotificationTemplate(
            templateId: "application_submitted",
            channel: NotificationChannel.Email,
            subject: "New booking application — {listingTitle}",
            htmlBody: applicationSubmittedHtml,
            plainTextBody: applicationSubmittedText);

        // Phase 17: pre-booking inquiry — sent to the host the moment a
        // guest opens a conversation thread on their listing, before any
        // application has been submitted.
        const string inquiryStartedHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">New question about your listing</h2>
              <p>A guest has started a conversation about <strong>{listingTitle}</strong>.</p>
              <p style="margin: 24px 0;">
                <a href="{threadUrl}"
                   style="display: inline-block; background: #111; color: #fff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 600;">
                  Open conversation
                </a>
              </p>
              <p style="font-size: 13px; color: #666;">
                Replying quickly increases the chance the guest will book. You can also
                review and respond from <a href="{frontendUrl}/app/dashboard">your dashboard</a>.
              </p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                This email was sent automatically by Lagedra. If you weren't expecting it,
                you can safely ignore it.
              </p>
            </body>
            </html>
            """;

        const string inquiryStartedText = """
            New question about your listing

            A guest has started a conversation about {listingTitle}.

            Open the thread: {threadUrl}

            Replying quickly increases the chance the guest will book.
            You can also review and respond from {frontendUrl}/app/dashboard.
            """;

        yield return new NotificationTemplate(
            templateId: "inquiry_started",
            channel: NotificationChannel.Email,
            subject: "New question about {listingTitle}",
            htmlBody: inquiryStartedHtml,
            plainTextBody: inquiryStartedText);

        const string dealActivatedHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">Your booking is active</h2>
              <p>Deal <strong>{dealId}</strong> is now active. Your signed lease agreement PDF is attached to this email for your records.</p>
              <p style="font-size: 13px; color: #666;">
                Keep this document for your records. You can also download it later from your deal page in Lagedra.
              </p>
            </body>
            </html>
            """;

        const string dealActivatedText = """
            Your booking is active

            Deal {dealId} is now active. Your signed lease agreement PDF is attached to this email for your records.
            """;

        yield return new NotificationTemplate(
            templateId: "deal_activated",
            channel: NotificationChannel.Email,
            subject: "Booking active — lease agreement attached",
            htmlBody: dealActivatedHtml,
            plainTextBody: dealActivatedText);

        const string protocolFeeInvoiceHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">Your protocol fee invoice is ready</h2>
              <p>
                Congratulations — your booking is active. Lagedra charges hosts a monthly
                protocol fee for each active deal, and your first invoice is ready to pay.
              </p>
              <p style="margin: 24px 0;">
                <a href="{invoiceUrl}"
                   style="display: inline-block; background: #111; color: #fff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 600;">
                  View &amp; pay invoice
                </a>
              </p>
              <p style="font-size: 13px; color: #666;">
                The card you pay with is saved securely by Stripe for future monthly
                invoices. The invoice is due within 7 days; unpaid protocol fees can lead
                to account suspension.
              </p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                This email was sent automatically by Lagedra for deal {dealId}.
              </p>
            </body>
            </html>
            """;

        const string protocolFeeInvoiceText = """
            Your protocol fee invoice is ready

            Congratulations — your booking is active. Lagedra charges hosts a monthly
            protocol fee for each active deal, and your first invoice is ready to pay.

            View & pay the invoice: {invoiceUrl}

            The card you pay with is saved securely by Stripe for future monthly
            invoices. The invoice is due within 7 days; unpaid protocol fees can lead
            to account suspension.
            """;

        yield return new NotificationTemplate(
            templateId: "protocol_fee_invoice",
            channel: NotificationChannel.Email,
            subject: "Action needed — your Lagedra protocol fee invoice",
            htmlBody: protocolFeeInvoiceHtml,
            plainTextBody: protocolFeeInvoiceText);

        const string rentCheckInHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">Did you receive this month's rent?</h2>
              <p>
                A new rent period has started for one of your active deals
                (<strong>{periodLabel}</strong>). Monthly rent is paid to you directly,
                so please confirm on your deal's billing page whether it arrived.
              </p>
              <p style="font-size: 13px; color: #666;">
                Confirming keeps your deal's record accurate. Reporting a missed payment
                opens a compliance record that supports you in any later dispute or
                arbitration.
              </p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                This email was sent automatically by Lagedra for deal {dealId}.
              </p>
            </body>
            </html>
            """;

        const string rentCheckInText = """
            Did you receive this month's rent?

            A new rent period has started for one of your active deals ({periodLabel}).
            Monthly rent is paid to you directly, so please confirm on your deal's
            billing page whether it arrived.

            Confirming keeps your deal's record accurate. Reporting a missed payment
            opens a compliance record that supports you in any later dispute or
            arbitration.
            """;

        yield return new NotificationTemplate(
            templateId: "rent_checkin_due",
            channel: NotificationChannel.Email,
            subject: "Rent check-in — did this month's rent arrive?",
            htmlBody: rentCheckInHtml,
            plainTextBody: rentCheckInText);

        const string ownerConsentRequestedHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">Your consent is needed</h2>
              <p>
                A guest requested a stay of more than 30 days at
                <strong>{listingTitle}</strong>. California law requires the home
                owner's consent when a property manager lists the home.
              </p>
              <p style="margin: 24px 0;">
                <a href="{consentUrl}"
                   style="display: inline-block; background: #111; color: #fff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 600;">
                  Review and consent
                </a>
              </p>
              <p style="font-size: 13px; color: #666;">
                The link is valid for {approveTokenTtlHours} hours. You can also
                review requests at
                <a href="{frontendUrl}/app/owner-consents">Owner consent</a>.
              </p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                This email was sent automatically by Lagedra. If you weren't expecting it,
                you can safely ignore it.
              </p>
            </body>
            </html>
            """;

        const string ownerConsentRequestedText = """
            Your consent is needed

            A guest requested a stay of more than 30 days at {listingTitle}.
            California law requires the home owner's consent when a property
            manager lists the home.

            Review and consent: {consentUrl}

            The link is valid for {approveTokenTtlHours} hours.
            You can also review requests at {frontendUrl}/app/owner-consents.
            """;

        yield return new NotificationTemplate(
            templateId: "owner_consent_requested",
            channel: NotificationChannel.Email,
            subject: "Owner consent needed — {listingTitle}",
            htmlBody: ownerConsentRequestedHtml,
            plainTextBody: ownerConsentRequestedText);

        const string ownerConsentGivenHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">The owner consented</h2>
              <p>
                The home owner consented to the tenancy for
                <strong>{listingTitle}</strong>. You can now accept the booking.
              </p>
              <p style="margin: 24px 0;">
                <a href="{approveUrl}"
                   style="display: inline-block; background: #111; color: #fff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 600;">
                  Accept booking
                </a>
              </p>
              <p style="font-size: 13px; color: #666;">
                The link is valid for {approveTokenTtlHours} hours and is single-use.
                You can also accept from
                <a href="{frontendUrl}/app/applications">your inbox</a>.
              </p>
            </body>
            </html>
            """;

        const string ownerConsentGivenText = """
            The owner consented

            The home owner consented to the tenancy for {listingTitle}.
            You can now accept the booking.

            Accept here: {approveUrl}

            The link is valid for {approveTokenTtlHours} hours and is single-use.
            """;

        yield return new NotificationTemplate(
            templateId: "owner_consent_given",
            channel: NotificationChannel.Email,
            subject: "Owner consented — you can accept {listingTitle}",
            htmlBody: ownerConsentGivenHtml,
            plainTextBody: ownerConsentGivenText);

        const string ownerConsentDeclinedHtml = """
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">The owner declined this stay</h2>
              <p>
                The home owner did not consent to the tenancy for
                <strong>{listingTitle}</strong>. The booking request has been closed.
              </p>
            </body>
            </html>
            """;

        const string ownerConsentDeclinedText = """
            The owner declined this stay

            The home owner did not consent to the tenancy for {listingTitle}.
            The booking request has been closed.
            """;

        yield return new NotificationTemplate(
            templateId: "owner_consent_declined",
            channel: NotificationChannel.Email,
            subject: "Owner declined — {listingTitle}",
            htmlBody: ownerConsentDeclinedHtml,
            plainTextBody: ownerConsentDeclinedText);
    }

    private static IEnumerable<NotificationTemplate> BuildSmsTemplates()
    {
        // SMS bodies live in PlainTextBody; HtmlBody mirrors them so the
        // NotificationTemplate constructor's required htmlBody is satisfied.
        yield return Sms("application_submitted",
            "Lagedra: New booking application for {listingTitle}. Review: {approveUrl}");
        yield return Sms("owner_consent_requested",
            "Lagedra: A stay at {listingTitle} needs your owner consent. Review: {consentUrl}");
        yield return Sms("owner_consent_given",
            "Lagedra: The owner consented for {listingTitle}. You can accept: {approveUrl}");
        yield return Sms("owner_consent_declined",
            "Lagedra: The owner declined the stay at {listingTitle}.");
        yield return Sms("application_approved",
            "Lagedra: Your booking application was approved. Open the app to continue.");
        yield return Sms("application_rejected",
            "Lagedra: Your booking application was not approved.");
        yield return Sms("application_expired",
            "Lagedra: Your booking application has expired.");
        yield return Sms("application_superseded",
            "Lagedra: Your booking application was superseded by another booking.");
        yield return Sms("booking_payment_failed",
            "Lagedra: Booking payment failed ({reason}). Please update your payment method.");
        yield return Sms("booking_payment_failed_host",
            "Lagedra: A guest's booking payment failed for your listing.");
        yield return Sms("payment_confirmed",
            "Lagedra: Payment confirmed. Your booking is moving forward.");
        yield return Sms("payment_disputed",
            "Lagedra: A payment dispute was opened on deal {dealId}.");
        yield return Sms("payment_dispute_resolved",
            "Lagedra: Payment dispute on deal {dealId} resolved: {outcome}.");
        yield return Sms("deal_activated",
            "Lagedra: Your stay is confirmed and active.");
        yield return Sms("booking_cancelled",
            "Lagedra: Booking cancelled. {reason}");
        yield return Sms("damage_claim_filed",
            "Lagedra: A damage claim of ${amount} was filed on deal {dealId}.");
        yield return Sms("payment_failed",
            "Lagedra: A subscription or protocol payment failed. Please update billing.");
        yield return Sms("damage_claim_approved",
            "Lagedra: Damage claim approved for ${amount} on deal {dealId}.");
        yield return Sms("damage_claim_rejected",
            "Lagedra: Damage claim rejected on deal {dealId}.");
        yield return Sms("deposit_return_due",
            "Lagedra: Return the deposit within 21 days (or itemize deductions with a damage photo).");
        yield return Sms("review_due",
            "Lagedra: Your stay is complete — please leave a review in the app.");
        yield return Sms("review_reminder",
            "Lagedra: Reminder — you still need to leave a review for your stay.");
        yield return Sms("arbitration_case_filed",
            "Lagedra: An arbitration case was filed.");
        yield return Sms("arbitration_decision",
            "Lagedra: An arbitration decision is ready (tier {tier}).");
        yield return Sms("evidence_complete",
            "Lagedra: Evidence window closed. Decision due {decisionDueAt}.");
        yield return Sms("arbitration_case_closed",
            "Lagedra: Your arbitration case has been closed.");
        yield return Sms("arbitration_case_appealed",
            "Lagedra: An arbitration case was appealed.");
        yield return Sms("identity_verified",
            "Lagedra: Your identity has been verified.");
        yield return Sms("identity_verification_failed",
            "Lagedra: Identity verification failed: {reason}");
        yield return Sms("insurance_status_changed",
            "Lagedra: Insurance status is now {newState} for deal {dealId}.");
    }

    private static NotificationTemplate Sms(string templateId, string body) =>
        new(
            templateId: templateId,
            channel: NotificationChannel.Sms,
            subject: templateId,
            htmlBody: body,
            plainTextBody: body);

    [LoggerMessage(
        EventId = 16100,
        Level = LogLevel.Information,
        Message = "Seeded {Count} notification templates")]
    private static partial void LogSeeded(ILogger logger, int count);
}
