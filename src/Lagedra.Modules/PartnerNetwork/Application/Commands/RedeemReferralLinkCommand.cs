using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Events;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record RedeemReferralLinkCommand(
    string Code,
    Guid RedeemedByUserId) : IRequest<Result>;

public sealed class RedeemReferralLinkCommandHandler(
    PartnerDbContext dbContext,
    IClock clock,
    IEventBus eventBus)
    : IRequestHandler<RedeemReferralLinkCommand, Result>
{
    public async Task<Result> Handle(
        RedeemReferralLinkCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var link = await dbContext.ReferralLinks
            .Include(l => l) // load the full entity
            .FirstOrDefaultAsync(l => l.Code == request.Code, cancellationToken)
            .ConfigureAwait(false);

        if (link is null)
        {
            return Result.Failure(new Error("Referral.NotFound", "Referral code not found."));
        }

        if (!link.IsActive)
        {
            return Result.Failure(new Error("Referral.Inactive", "Referral link is no longer active."));
        }

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == link.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result.Failure(new Error("Partner.NotFound", "Partner organization not found."));
        }

        var alreadyRedeemed = await dbContext.ReferralRedemptions
            .AnyAsync(r => r.ReferralLinkId == link.Id
                        && r.RedeemedByUserId == request.RedeemedByUserId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyRedeemed)
        {
            return Result.Failure(new Error("Referral.AlreadyRedeemed",
                "You have already redeemed this referral link."));
        }

        link.Redeem(clock);

        var redemption = ReferralRedemption.Create(
            link.Id, link.OrganizationId, request.RedeemedByUserId, clock);

        dbContext.ReferralRedemptions.Add(redemption);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await eventBus.Publish(new ReferralRedeemedEvent(
            link.OrganizationId, link.Id, request.RedeemedByUserId, org.Name),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
