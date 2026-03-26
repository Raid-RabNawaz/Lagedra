using Lagedra.Modules.Privacy.Domain.Aggregates;
using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Caching;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Application.Commands;

public sealed record RecordConsentCommand(
    Guid UserId,
    ConsentType ConsentType,
    string IpAddress,
    string UserAgent) : IRequest<Result>;

public sealed class RecordConsentCommandHandler(PrivacyDbContext dbContext, ICacheService cache)
    : IRequestHandler<RecordConsentCommand, Result>
{
    public async Task<Result> Handle(RecordConsentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userConsent = await dbContext.UserConsents
            .Include(uc => uc.ConsentRecords)
            .FirstOrDefaultAsync(uc => uc.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (userConsent is null)
        {
            userConsent = UserConsent.Create(request.UserId);
            dbContext.UserConsents.Add(userConsent);
        }

        var record = userConsent.RecordConsent(request.ConsentType, request.IpAddress, request.UserAgent);
        dbContext.Entry(record).State = EntityState.Added;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cache.RemoveAsync($"user:consent:{request.UserId}", cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
