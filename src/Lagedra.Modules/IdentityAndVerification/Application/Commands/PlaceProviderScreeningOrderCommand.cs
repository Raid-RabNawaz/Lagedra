using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Screening;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record PlaceProviderScreeningOrderCommand(Guid ScreeningOrderId) : IRequest<Result>;

public sealed class PlaceProviderScreeningOrderCommandHandler(
    IdentityDbContext dbContext,
    IApplicationScreeningContextProvider applicationContext,
    IListingProvider listingProvider,
    ILeasePartyProfileProvider partyProfiles,
    ITenantScreeningProvider screeningProvider,
    IMediator mediator)
    : IRequestHandler<PlaceProviderScreeningOrderCommand, Result>
{
    public async Task<Result> Handle(
        PlaceProviderScreeningOrderCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await dbContext.ScreeningOrders
            .FirstOrDefaultAsync(o => o.Id == request.ScreeningOrderId, cancellationToken)
            .ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure(new Error("Screening.NotFound", "Screening order not found."));
        }

        if (!string.IsNullOrWhiteSpace(order.ExternalOrderId))
        {
            return Result.Success();
        }

        var app = await applicationContext
            .GetAsync(order.ApplicationId, cancellationToken)
            .ConfigureAwait(false);
        if (app is null)
        {
            return Result.Failure(new Error("Application.NotFound", "Application not found."));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(order.ListingId, cancellationToken)
            .ConfigureAwait(false);
        var address = listing?.PreciseAddress;
        if (address is null
            || string.IsNullOrWhiteSpace(address.Street)
            || string.IsNullOrWhiteSpace(address.City)
            || string.IsNullOrWhiteSpace(address.State)
            || string.IsNullOrWhiteSpace(address.ZipCode))
        {
            const string addressRequired =
                "This listing needs a full street address before a screening can be ordered.";
            order.MarkFailed(addressRequired);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure(new Error("Screening.AddressRequired", addressRequired));
        }

        var tenant = await partyProfiles.GetAsync(order.TenantUserId, cancellationToken).ConfigureAwait(false);
        var host = await partyProfiles.GetAsync(order.HostUserId, cancellationToken).ConfigureAwait(false);
        var requester = ScreeningRequesterFactory.TryCreateFromHost(order.HostUserId, host);
        if (requester is null)
        {
            const string requesterRequired =
                "The host needs a name and email on their profile before a screening can be ordered.";
            order.MarkFailed(requesterRequired);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure(new Error("Screening.RequesterRequired", requesterRequired));
        }

        var first = tenant?.FirstName;
        var last = tenant?.LastName;
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
        {
            SplitName(tenant?.FullName, out first, out last);
        }

        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) || string.IsNullOrWhiteSpace(tenant?.Email))
        {
            const string applicantRequired =
                "The applicant is missing a name or email required to place a screening.";
            order.MarkFailed(applicantRequired);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure(new Error("Screening.ApplicantRequired", applicantRequired));
        }

        try
        {
            var created = await screeningProvider.CreateOrderAsync(
                new ScreeningOrderRequest(
                    ScreeningPackage.CriminalAndCredit,
                    new ScreeningPropertyAddress(
                        address.Street,
                        address.City,
                        address.State,
                        address.ZipCode,
                        listing?.Title),
                    new ScreeningApplicant(first, last, tenant.Email, tenant.Phone),
                    requester,
                    IdempotencyKey: order.Status == Domain.Enums.ScreeningOrderStatus.Failed
                        ? $"screening-{order.Id:N}-retry"
                        : $"screening-{order.Id:N}"),
                cancellationToken).ConfigureAwait(false);

            order.AttachProviderOrder(created);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await mediator.Send(
                new NotifyScreeningApplyLinkCommand(order.Id),
                cancellationToken).ConfigureAwait(false);

            if (created.Status == ScreeningOrderStatus.Completed)
            {
                var report = created.ProviderReportId is { } rid
                    ? await screeningProvider.GetReportAsync(rid, cancellationToken).ConfigureAwait(false)
                    : await screeningProvider.GetOrderReportAsync(created.ProviderOrderId, cancellationToken)
                        .ConfigureAwait(false);
                if (report is not null)
                {
                    await mediator.Send(
                        new IngestBackgroundCheckResultCommand(
                            order.TenantUserId,
                            report.ProviderReportId,
                            report.OverallResult),
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            order.MarkFailed(ex.Message);
            await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            return Result.Failure(new Error("Screening.ProviderFailed", ex.Message));
        }

        return Result.Success();
    }

    private static void SplitName(string? fullName, out string first, out string last)
    {
        first = "Applicant";
        last = "Unknown";
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return;
        }

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        first = parts[0];
        last = parts.Length > 1 ? parts[1] : parts[0];
    }
}
