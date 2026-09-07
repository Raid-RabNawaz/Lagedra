using Lagedra.Modules.Notifications.Application;
using Lagedra.Modules.Notifications.Application.DTOs;
using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Sms;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record UpdateUserPreferencesCommand(
    Guid UserId,
    Dictionary<string, bool> EventOptIns,
    bool? SmsCampaignsOptedIn = null) : IRequest<Result<NotificationPreferencesDto>>;

public sealed class UpdateUserPreferencesCommandHandler(
    NotificationDbContext dbContext,
    IUserPhoneResolver phoneResolver,
    IClock clock)
    : IRequestHandler<UpdateUserPreferencesCommand, Result<NotificationPreferencesDto>>
{
    private static readonly Error PhoneRequired = new(
        "Sms.PhoneRequired",
        "Add a mobile number to your profile before opting in to text messages.");

    public async Task<Result<NotificationPreferencesDto>> Handle(
        UpdateUserPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prefs = await dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (prefs is null)
        {
            prefs = new UserNotificationPreferences(request.UserId);
            dbContext.UserPreferences.Add(prefs);
        }

        foreach (var kvp in request.EventOptIns)
        {
            prefs.SetEventOptIn(kvp.Key, kvp.Value);
        }

        if (request.SmsCampaignsOptedIn is { } smsOptedIn)
        {
            var applied = await ApplySmsPreferenceAsync(
                    request.UserId,
                    smsOptedIn,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!applied.IsSuccess)
            {
                return Result<NotificationPreferencesDto>.Failure(applied.Error);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<NotificationPreferencesDto>.Success(
            await MapPreferencesAsync(prefs, cancellationToken).ConfigureAwait(false));
    }

    private async Task<Result> ApplySmsPreferenceAsync(
        Guid userId,
        bool smsOptedIn,
        CancellationToken cancellationToken)
    {
        var phone = await phoneResolver
            .GetPhoneAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        if (PhoneNumberE164.TryNormalize(phone, out var e164))
        {
            await SmsConsentStore.ApplyAsync(
                    dbContext,
                    e164,
                    smsOptedIn,
                    SmsConsent.SourcePreferences,
                    clock.UtcNow,
                    userId,
                    cancellationToken)
                .ConfigureAwait(false);
            return Result.Success();
        }

        if (smsOptedIn)
        {
            return Result.Failure(PhoneRequired);
        }

        await SmsConsentStore.OptOutByUserIdAsync(
                dbContext,
                userId,
                SmsConsent.SourcePreferences,
                clock.UtcNow,
                cancellationToken)
            .ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<NotificationPreferencesDto> MapPreferencesAsync(
        UserNotificationPreferences prefs,
        CancellationToken cancellationToken)
    {
        var phone = await phoneResolver
            .GetPhoneAsync(prefs.UserId, cancellationToken)
            .ConfigureAwait(false);
        var normalized = PhoneNumberE164.TryNormalize(phone, out var e164) ? e164 : null;
        var smsOptedIn = normalized is not null
            && await SmsConsentStore
                .IsOptedInAsync(dbContext, normalized, cancellationToken)
                .ConfigureAwait(false);

        return new NotificationPreferencesDto(
            prefs.UserId,
            prefs.EventOptIns,
            prefs.TransactionalAlwaysSent,
            smsOptedIn,
            normalized);
    }
}
