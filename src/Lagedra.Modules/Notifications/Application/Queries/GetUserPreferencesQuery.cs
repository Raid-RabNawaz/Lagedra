using Lagedra.Modules.Notifications.Application;
using Lagedra.Modules.Notifications.Application.DTOs;
using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Sms;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Queries;

public sealed record GetUserPreferencesQuery(Guid UserId) : IRequest<Result<NotificationPreferencesDto>>;

public sealed class GetUserPreferencesQueryHandler(
    NotificationDbContext dbContext,
    IUserPhoneResolver phoneResolver)
    : IRequestHandler<GetUserPreferencesQuery, Result<NotificationPreferencesDto>>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        GetUserPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prefs = await dbContext.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (prefs is null)
        {
            prefs = new UserNotificationPreferences(request.UserId);
        }

        var phone = await phoneResolver
            .GetPhoneAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);
        var normalized = PhoneNumberE164.TryNormalize(phone, out var e164) ? e164 : null;
        var smsOptedIn = normalized is not null
            && await SmsConsentStore
                .IsOptedInAsync(dbContext, normalized, cancellationToken)
                .ConfigureAwait(false);

        return Result<NotificationPreferencesDto>.Success(
            new NotificationPreferencesDto(
                prefs.UserId,
                prefs.EventOptIns,
                prefs.TransactionalAlwaysSent,
                smsOptedIn,
                normalized));
    }
}
