using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Notifications.Infrastructure.Seeding;

/// <summary>
/// Seeds the minimum set of email templates Phase 16 depends on.
///
/// History: prior to Phase 16 the Notifications module assumed templates
/// would be created via an admin UI / seed migration that never landed,
/// so every email path silently 404'd at the template lookup in
/// <c>SendEmailNotificationCommandHandler</c>. The 16.10 one-tap approve
/// flow makes that gap user-visible (the host's email never arrives), so
/// we ship a baseline set here and only the <c>application_submitted</c>
/// template is strictly required — others can be added incrementally.
///
/// Seeding is upsert-by-(TemplateId, Channel): existing rows with the
/// same template id are left untouched so an admin can override the body
/// without their changes being clobbered on the next deploy.
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

        var templates = BuildBaselineTemplates();

        var existingIds = await dbContext.Templates
            .Where(t => t.Channel == NotificationChannel.Email)
            .Select(t => t.TemplateId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existingSet = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var template in templates)
        {
            if (existingSet.Contains(template.TemplateId))
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
    }

    [LoggerMessage(
        EventId = 16100,
        Level = LogLevel.Information,
        Message = "Seeded {Count} notification email templates")]
    private static partial void LogSeeded(ILogger logger, int count);
}
