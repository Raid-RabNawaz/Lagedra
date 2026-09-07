using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Sms;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application;

internal static class SmsConsentStore
{
    public static async Task<SmsConsent?> FindAsync(
        NotificationDbContext dbContext,
        string phoneE164,
        CancellationToken cancellationToken)
    {
        return await dbContext.SmsConsents
            .FirstOrDefaultAsync(c => c.PhoneE164 == phoneE164, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<bool> IsOptedInAsync(
        NotificationDbContext dbContext,
        string? phone,
        CancellationToken cancellationToken)
    {
        if (!PhoneNumberE164.TryNormalize(phone, out var normalized))
        {
            return false;
        }

        var consent = await FindAsync(dbContext, normalized, cancellationToken)
            .ConfigureAwait(false);
        return consent?.OptedIn == true;
    }

    public static async Task<SmsConsent> ApplyAsync(
        NotificationDbContext dbContext,
        string phone,
        bool optedIn,
        string source,
        DateTime utcNow,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (!PhoneNumberE164.TryNormalize(phone, out var normalized))
        {
            throw new ArgumentException("Enter a valid mobile number.", nameof(phone));
        }

        var consent = await FindAsync(dbContext, normalized, cancellationToken)
            .ConfigureAwait(false);
        if (consent is null)
        {
            consent = SmsConsent.Create(normalized);
            dbContext.SmsConsents.Add(consent);
        }

        if (optedIn)
        {
            consent.OptIn(source, utcNow, userId);
        }
        else
        {
            consent.OptOut(source, utcNow, userId);
        }

        return consent;
    }

    public static async Task OptOutByUserIdAsync(
        NotificationDbContext dbContext,
        Guid userId,
        string source,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.SmsConsents
            .Where(c => c.UserId == userId && c.OptedIn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var row in rows)
        {
            row.OptOut(source, utcNow, userId);
        }
    }
}
