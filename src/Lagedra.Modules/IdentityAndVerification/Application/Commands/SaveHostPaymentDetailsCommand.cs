using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record SaveHostPaymentDetailsCommand(
    Guid HostUserId,
    string PaymentInfo) : IRequest<Result>;

public sealed class SaveHostPaymentDetailsCommandHandler(
    IdentityDbContext dbContext,
    IEncryptionService encryptionService,
    IClock clock)
    : IRequestHandler<SaveHostPaymentDetailsCommand, Result>
{
    public async Task<Result> Handle(
        SaveHostPaymentDetailsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encrypted = encryptionService.Encrypt(request.PaymentInfo);

        var existing = await dbContext.HostPaymentDetails
            .FirstOrDefaultAsync(h => h.HostUserId == request.HostUserId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.UpdatePaymentInfo(encrypted, clock);
        }
        else
        {
            var details = HostPaymentDetails.Create(request.HostUserId, encrypted, clock);
            dbContext.HostPaymentDetails.Add(details);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
