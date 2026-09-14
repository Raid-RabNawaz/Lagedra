using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record NotifyScreeningApplyLinkCommand(Guid ScreeningOrderId) : IRequest<Result>;

public sealed class NotifyScreeningApplyLinkCommandHandler(
    IdentityDbContext dbContext,
    IListingProvider listingProvider,
    IMediator mediator,
    IConfiguration configuration)
    : IRequestHandler<NotifyScreeningApplyLinkCommand, Result>
{
    public async Task<Result> Handle(
        NotifyScreeningApplyLinkCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await dbContext.ScreeningOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.ScreeningOrderId, cancellationToken)
            .ConfigureAwait(false);
        if (order is null)
        {
            return Result.Success();
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(order.ListingId, cancellationToken)
            .ConfigureAwait(false);
        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var applicationUrl = $"{frontendUrl}/app/applications/{order.ApplicationId}";
        // Checkr test mode usually omits application_url (apply flow skipped).
        // Still email the guest with the booking-request page as the CTA.
        var applyUrl = order.ApplicationUrl?.ToString() ?? applicationUrl;

        await mediator.Send(
            new NotifyUserCommand(
                order.TenantUserId,
                "screening_apply_link_ready",
                "Complete your background screening",
                "Your host requested a background and credit check. Open the link to complete it.",
                new Dictionary<string, string>
                {
                    ["applyUrl"] = applyUrl,
                    ["applicationUrl"] = applicationUrl,
                    ["listingTitle"] = listing?.Title ?? "your stay",
                    ["frontendUrl"] = frontendUrl
                },
                [NotificationChannel.Email, NotificationChannel.InApp, NotificationChannel.Sms],
                order.ApplicationId,
                "DealApplication"),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
