using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Application.Commands;

public sealed record WithdrawConsentCommand(
    Guid UserId,
    ConsentType ConsentType) : IRequest<Result>;

public sealed class WithdrawConsentCommandHandler(PrivacyDbContext dbContext)
    : IRequestHandler<WithdrawConsentCommand, Result>
{
    public async Task<Result> Handle(WithdrawConsentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userConsent = await dbContext.UserConsents
            .Include(uc => uc.ConsentRecords)
            .FirstOrDefaultAsync(uc => uc.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (userConsent is null)
        {
            return Result.Failure(new Error("Privacy.Consent.NotFound", "No consent records found for this user."));
        }

        userConsent.WithdrawConsent(request.ConsentType);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
