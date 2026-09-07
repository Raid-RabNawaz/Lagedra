using Lagedra.Modules.Notifications.Domain;
using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Sms;
using Lagedra.SharedKernel.Time;
using MediatR;

namespace Lagedra.Modules.Notifications.Application.Commands;

/// <summary>
/// Applies STOP / START keywords from a Twilio inbound SMS and returns the
/// TwiML body (if any) we should reply with.
/// </summary>
public sealed record HandleInboundSmsCommand(
    string From,
    string Body) : IRequest<Result<string?>>;

public sealed class HandleInboundSmsCommandHandler(
    NotificationDbContext dbContext,
    IClock clock)
    : IRequestHandler<HandleInboundSmsCommand, Result<string?>>
{
    public async Task<Result<string?>> Handle(
        HandleInboundSmsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!PhoneNumberE164.TryNormalize(request.From, out var phone))
        {
            return Result<string?>.Success(null);
        }

        if (SmsProgram.IsHelpKeyword(request.Body))
        {
            return Result<string?>.Success(SmsProgram.HelpReply);
        }

        if (SmsProgram.IsStopKeyword(request.Body))
        {
            await SmsConsentStore.ApplyAsync(
                dbContext,
                phone,
                optedIn: false,
                SmsConsent.SourceKeyword,
                clock.UtcNow,
                userId: null,
                cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<string?>.Success(SmsProgram.StopReply);
        }

        if (SmsProgram.IsStartKeyword(request.Body))
        {
            await SmsConsentStore.ApplyAsync(
                dbContext,
                phone,
                optedIn: true,
                SmsConsent.SourceKeyword,
                clock.UtcNow,
                userId: null,
                cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<string?>.Success(SmsProgram.StartReply);
        }

        return Result<string?>.Success(null);
    }
}
