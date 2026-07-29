using System.Globalization;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Reviews.Application.Commands;
using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.Reviews.Infrastructure.Jobs;

/// <summary>
/// Publishes expired stay-review windows and repeatedly nudges host/guest
/// parties who have not submitted a review while the window is open.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class PublishExpiredStayReviewsJob(
    ReviewsDbContext dbContext,
    IMediator mediator,
    IClock clock,
    IPlatformSettingsService settings,
    ILogger<PublishExpiredStayReviewsJob> logger) : IJob
{
    private const int DefaultReminderIntervalDays = 3;

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;
        var now = clock.UtcNow;

        var due = await dbContext.StayReviewWindows
            .Where(w => !w.IsPublished && w.ClosesAt <= now)
            .Select(w => w.DealId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var dealId in due)
        {
            await mediator.Send(new PublishStayReviewWindowCommand(dealId), ct)
                .ConfigureAwait(false);
        }

        if (due.Count > 0)
        {
            LogPublished(logger, due.Count);
        }

        // Repair: windows already marked published while a review stayed
        // Submitted (historical race when the second party submitted).
        var orphaned = await dbContext.StayReviews
            .Where(r => r.Status == StayReviewStatus.Submitted)
            .Join(
                dbContext.StayReviewWindows.Where(w => w.IsPublished),
                r => r.DealId,
                w => w.DealId,
                (r, _) => r)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (orphaned.Count > 0)
        {
            foreach (var review in orphaned)
            {
                review.Publish(clock);
            }

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogOrphansPublished(logger, orphaned.Count);
        }

        var reminderIntervalDays = (int)await settings
            .GetLongAsync(
                PlatformSettingKeys.ReviewReminderIntervalDays,
                DefaultReminderIntervalDays,
                ct)
            .ConfigureAwait(false);

        // All open, unpublished windows — recurring reminders for parties who
        // still owe a review.
        var openWindows = await dbContext.StayReviewWindows
            .Where(w => !w.IsPublished && w.ClosesAt > now)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reminded = 0;
        foreach (var window in openWindows)
        {
            if (!window.ReminderDue(clock, reminderIntervalDays))
            {
                continue;
            }

            var daysLeft = Math.Max(
                1,
                (int)Math.Ceiling((window.ClosesAt - now).TotalDays));
            var closesLabel = window.ClosesAt.ToString("d", CultureInfo.InvariantCulture);
            var body =
                $"Your stay is complete but you haven't left a review yet. "
                + $"You have about {daysLeft} day{(daysLeft == 1 ? "" : "s")} left "
                + $"(until {closesLabel}). Reviews stay private until both sides submit.";

            var sentAny = false;

            if (window.NeedsGuestReminder())
            {
                await mediator.Send(new NotifyUserCommand(
                    window.TenantUserId,
                    "review_reminder",
                    "Reminder: leave a review for your host",
                    body,
                    new()
                    {
                        ["dealId"] = window.DealId.ToString(),
                        ["daysLeft"] = daysLeft.ToString(CultureInfo.InvariantCulture),
                    },
                    ReviewJobChannels.EmailInAppAndSms,
                    window.DealId,
                    "Deal"), ct).ConfigureAwait(false);
                sentAny = true;
            }

            if (window.NeedsHostReminder())
            {
                await mediator.Send(new NotifyUserCommand(
                    window.LandlordUserId,
                    "review_reminder",
                    "Reminder: leave a review for your guest",
                    body,
                    new()
                    {
                        ["dealId"] = window.DealId.ToString(),
                        ["daysLeft"] = daysLeft.ToString(CultureInfo.InvariantCulture),
                    },
                    ReviewJobChannels.EmailInAppAndSms,
                    window.DealId,
                    "Deal"), ct).ConfigureAwait(false);
                sentAny = true;
            }

            if (sentAny)
            {
                window.MarkReminderSent(clock);
                reminded++;
            }
        }

        if (reminded > 0)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogReminded(logger, reminded);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Count} expired stay-review windows")]
    private static partial void LogPublished(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Count} orphaned stay reviews left Submitted on published windows")]
    private static partial void LogOrphansPublished(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent review reminders for {Count} windows")]
    private static partial void LogReminded(ILogger logger, int count);
}

file static class ReviewJobChannels
{
    internal static readonly Lagedra.Modules.Notifications.Domain.Enums.NotificationChannel[] EmailInAppAndSms =
    [
        Lagedra.Modules.Notifications.Domain.Enums.NotificationChannel.Email,
        Lagedra.Modules.Notifications.Domain.Enums.NotificationChannel.InApp,
        Lagedra.Modules.Notifications.Domain.Enums.NotificationChannel.Sms
    ];
}
