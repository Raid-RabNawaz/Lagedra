using System.Globalization;
using System.Security.Cryptography;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Sms;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record SendPhoneVerificationCommand(Guid UserId) : IRequest<Result>;

public sealed class SendPhoneVerificationCommandHandler(
    UserManager<ApplicationUser> userManager,
    ISmsService smsService,
    IHashingService hashingService,
    IClock clock)
    : IRequestHandler<SendPhoneVerificationCommand, Result>
{
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MinResendInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan HourlyWindow = TimeSpan.FromHours(1);
    private const int MaxSendsPerHour = 5;

    public async Task<Result> Handle(
        SendPhoneVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return AuthErrors.PhoneRequired;
        }

        if (user.IsPhoneVerified)
        {
            return AuthErrors.PhoneAlreadyVerified;
        }

        var now = clock.UtcNow;

        if (user.PhoneVerificationSentAt is { } lastSent
            && now - lastSent < MinResendInterval)
        {
            return AuthErrors.PhoneCodeRateLimited;
        }

        if (user.PhoneVerificationWindowStartedAt is null
            || now - user.PhoneVerificationWindowStartedAt.Value >= HourlyWindow)
        {
            user.PhoneVerificationWindowStartedAt = now;
            user.PhoneVerificationSendCount = 0;
        }

        if (user.PhoneVerificationSendCount >= MaxSendsPerHour)
        {
            return AuthErrors.PhoneCodeRateLimited;
        }

        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000)
            .ToString(CultureInfo.InvariantCulture);

        user.PhoneVerificationCodeHash = hashingService.Hash(code);
        user.PhoneVerificationExpiresAt = now.Add(CodeTtl);
        user.PhoneVerificationSentAt = now;
        user.PhoneVerificationSendCount++;

        var update = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!update.Succeeded)
        {
            return AuthErrors.IdentityError(update.Errors.First().Description);
        }

        await smsService.SendAsync(new SmsMessage
        {
            ToE164 = user.PhoneNumber!,
            Body = $"Your Lagedra verification code is {code}. It expires in 10 minutes."
        }, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
