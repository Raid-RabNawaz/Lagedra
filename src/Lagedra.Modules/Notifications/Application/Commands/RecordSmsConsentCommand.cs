using Lagedra.Modules.Notifications.Application.DTOs;
using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Sms;
using Lagedra.SharedKernel.Time;
using MediatR;

namespace Lagedra.Modules.Notifications.Application.Commands;

/// <summary>
/// Records an A2P web-form or in-app SMS campaign opt-in / opt-out.
/// Opt-in requires an explicit <c>Consent</c> flag so a pre-checked box
/// cannot silently subscribe someone.
/// </summary>
public sealed record RecordSmsConsentCommand(
    string PhoneNumber,
    bool Consent,
    bool OptedIn,
    string Source,
    Guid? UserId) : IRequest<Result<SmsConsentDto>>;

public sealed class RecordSmsConsentCommandHandler(
    NotificationDbContext dbContext,
    IClock clock)
    : IRequestHandler<RecordSmsConsentCommand, Result<SmsConsentDto>>
{
    private static readonly Error PhoneInvalid = new(
        "Sms.PhoneInvalid",
        "Enter a valid mobile number, e.g. (555) 123-4567 or +15551234567.");

    private static readonly Error ConsentRequired = new(
        "Sms.ConsentRequired",
        "Check the box to confirm you want automated text messages. Consent is not required to use Lagedra.");

    public async Task<Result<SmsConsentDto>> Handle(
        RecordSmsConsentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!PhoneNumberE164.TryNormalize(request.PhoneNumber, out var phone))
        {
            return Result<SmsConsentDto>.Failure(PhoneInvalid);
        }

        if (request.OptedIn && !request.Consent)
        {
            return Result<SmsConsentDto>.Failure(ConsentRequired);
        }

        var consent = await SmsConsentStore.ApplyAsync(
            dbContext,
            phone,
            request.OptedIn,
            string.IsNullOrWhiteSpace(request.Source)
                ? SmsConsent.SourceWebForm
                : request.Source,
            clock.UtcNow,
            request.UserId,
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<SmsConsentDto>.Success(
            new SmsConsentDto(consent.PhoneE164, consent.OptedIn, consent.OptedInAt, consent.OptedOutAt));
    }
}
