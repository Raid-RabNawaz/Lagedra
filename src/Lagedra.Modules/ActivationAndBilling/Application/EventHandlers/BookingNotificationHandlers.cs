using System.Globalization;
using System.Text;
using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

internal static class Channels
{
    internal static readonly NotificationChannel[] EmailAndInApp = [NotificationChannel.Email, NotificationChannel.InApp];
    internal static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];
}

public sealed class OnApplicationSubmittedNotify(
    BillingDbContext db,
    IMediator m,
    IActionTokenService actionTokens,
    IListingProvider listingProvider,
    IConfiguration configuration)
    : IDomainEventHandler<ApplicationSubmittedEvent>
{
    private const int OneTapTtlHours = 72;

    public async Task Handle(ApplicationSubmittedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == e.ApplicationId, ct).ConfigureAwait(false);
        if (app is null) return;

        // Phase 16.10: mint a 72h one-tap approval token and ship it
        // alongside the application id. The frontend `/host/approve`
        // page reads the token from the query string and POSTs it to
        // /v1/actions/approve-application — bypassing the in-app
        // approval flow when the host is already on email/mobile.
        var approveToken = actionTokens.Issue(
            ApproveApplicationByTokenCommandHandler.ActionLabel,
            subjectId: e.ApplicationId,
            principalUserId: app.LandlordUserId,
            ttl: TimeSpan.FromHours(OneTapTtlHours));

        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000")
            .TrimEnd('/');

        // Pull listing context so the email subject + body can show the
        // host *what* they're approving, and so /host/approve can pre-fill
        // a sensible default deposit. Best-effort: a missing listing
        // (e.g. concurrent unpublish) just falls back to placeholders.
        var listing = await listingProvider
            .GetListingDetailsAsync(e.ListingId, ct)
            .ConfigureAwait(false);

        var listingTitle = listing?.Title ?? "your listing";
        var defaultDepositCents = listing?.DefaultDepositCents
            ?? listing?.MaxDepositCents
            ?? 0;

        var approveUrl = BuildApproveUrl(
            frontendUrl,
            approveToken,
            e.ApplicationId,
            listingTitle,
            defaultDepositCents);

        await m.Send(new NotifyUserCommand(
            app.LandlordUserId, "application_submitted",
            "New Booking Application",
            "A tenant has submitted a booking application for your listing.",
            new()
            {
                ["applicationId"] = e.ApplicationId.ToString(),
                ["listingId"] = e.ListingId.ToString(),
                ["listingTitle"] = listingTitle,
                ["approveToken"] = approveToken,
                ["approveTokenTtlHours"] = OneTapTtlHours.ToString(CultureInfo.InvariantCulture),
                ["approveUrl"] = approveUrl,
                ["frontendUrl"] = frontendUrl,
                ["defaultDepositCents"] = defaultDepositCents.ToString(CultureInfo.InvariantCulture),
            },
            Channels.EmailAndInApp, e.ListingId, "Listing"), ct).ConfigureAwait(false);
    }

    private static string BuildApproveUrl(
        string frontendUrl,
        string token,
        Guid applicationId,
        string listingTitle,
        long defaultDepositCents)
    {
        // Manual query construction so we don't depend on UriBuilder's
        // legacy quirks (e.g. it strips port 80) and so the values land
        // verbatim in the email — `/host/approve` decodes them itself.
        var sb = new StringBuilder(frontendUrl.Length + 256);
        sb.Append(frontendUrl).Append("/host/approve");
        sb.Append("?token=").Append(Uri.EscapeDataString(token));
        sb.Append("&applicationId=").Append(applicationId.ToString("D"));
        sb.Append("&listingTitle=").Append(Uri.EscapeDataString(listingTitle));
        if (defaultDepositCents > 0)
        {
            sb.Append("&depositCents=")
              .Append(defaultDepositCents.ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}

public sealed class OnApplicationApprovedNotify(IMediator m)
    : IDomainEventHandler<ApplicationApprovedEvent>
{
    public async Task Handle(ApplicationApprovedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.TenantUserId, "application_approved",
            "Application Approved",
            "Your booking application has been approved! Please review and confirm the deal terms.",
            new() { ["dealId"] = e.DealId.ToString(), ["listingId"] = e.ListingId.ToString() },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnApplicationRejectedNotify(IMediator m)
    : IDomainEventHandler<ApplicationRejectedEvent>
{
    public async Task Handle(ApplicationRejectedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.TenantUserId, "application_rejected",
            "Application Not Accepted",
            "Unfortunately, your booking application was not accepted by the host.",
            new() { ["applicationId"] = e.ApplicationId.ToString(), ["listingId"] = e.ListingId.ToString() },
            Channels.EmailAndInApp, e.ListingId, "Listing"), ct).ConfigureAwait(false);
    }
}

public sealed class OnPaymentConfirmedNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<PaymentConfirmedEvent>
{
    public async Task Handle(PaymentConfirmedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == e.DealId, ct).ConfigureAwait(false);
        if (app is null) return;

        await m.Send(new NotifyUserCommand(
            app.TenantUserId, "payment_confirmed",
            "Payment Confirmed",
            "Your host has confirmed receiving your payment. Waiting for insurance activation.",
            new() { ["dealId"] = e.DealId.ToString() },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnPaymentDisputedNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<PaymentDisputedEvent>
{
    public async Task Handle(PaymentDisputedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == e.DealId, ct).ConfigureAwait(false);
        if (app is null) return;

        await m.Send(new NotifyUserCommand(
            app.LandlordUserId, "payment_disputed",
            "Payment Disputed",
            $"The tenant has disputed the payment: {e.Reason}",
            new() { ["dealId"] = e.DealId.ToString(), ["reason"] = e.Reason },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnPaymentDisputeResolvedNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<PaymentDisputeResolvedEvent>
{
    public async Task Handle(PaymentDisputeResolvedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == e.DealId, ct).ConfigureAwait(false);
        if (app is null) return;

        var outcome = e.PaymentValid ? "Payment validated — deal proceeds." : "Payment not validated — deal cancelled.";
        foreach (var userId in new[] { app.TenantUserId, app.LandlordUserId })
        {
            await m.Send(new NotifyUserCommand(
                userId, "payment_dispute_resolved",
                "Payment Dispute Resolved",
                outcome,
                new() { ["dealId"] = e.DealId.ToString(), ["outcome"] = outcome },
                Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
        }
    }
}

public sealed class OnDealActivatedNotify(IMediator m)
    : IDomainEventHandler<DealActivatedEvent>
{
    public async Task Handle(DealActivatedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.TenantUserId, "deal_activated",
            "Booking Active",
            "Your booking is now active and insurance is confirmed.",
            new() { ["dealId"] = e.DealId.ToString() },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);

        await m.Send(new NotifyUserCommand(
            e.LandlordUserId, "deal_activated",
            "Deal Complete",
            "Deal complete. Your booking is active and insurance is in place.",
            new() { ["dealId"] = e.DealId.ToString() },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnBookingCancelledNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<BookingCancelledEvent>
{
    public async Task Handle(BookingCancelledEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == e.DealId, ct).ConfigureAwait(false);
        if (app is null) return;

        // Currency display policy (Lagedra): never show fractional cents
        // in user-facing copy. Ceiling-round to the nearest whole dollar
        // so the recipient is never quoted a lower number than what
        // actually hits their account.
        var refundDollars = (long)Math.Ceiling(e.RefundAmountCents / 100m);
        var refundInfo = e.RefundAmountCents > 0
            ? $"A refund of ${refundDollars:N0} will be processed."
            : "No refund is applicable per the cancellation policy.";

        await m.Send(new NotifyUserCommand(
            app.TenantUserId, "booking_cancelled",
            "Booking Cancelled",
            $"{e.Reason}. {refundInfo}",
            new() { ["dealId"] = e.DealId.ToString(), ["reason"] = e.Reason, ["refundInfo"] = refundInfo },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);

        await m.Send(new NotifyUserCommand(
            app.LandlordUserId, "booking_cancelled",
            "Booking Cancelled",
            $"A booking has been cancelled: {e.Reason}",
            new() { ["dealId"] = e.DealId.ToString(), ["reason"] = e.Reason },
            Channels.EmailAndInApp, e.ListingId, "Listing"), ct).ConfigureAwait(false);
    }
}

public sealed class OnDamageClaimFiledNotify(IMediator m)
    : IDomainEventHandler<DamageClaimFiledEvent>
{
    public async Task Handle(DamageClaimFiledEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var claimDollars = (long)Math.Ceiling(e.ClaimedAmountCents / 100m);
        await m.Send(new NotifyUserCommand(
            e.TenantUserId, "damage_claim_filed",
            "Damage Claim Filed",
            $"A damage claim of ${claimDollars.ToString("N0", CultureInfo.InvariantCulture)} has been filed for your stay.",
            new() { ["dealId"] = e.DealId.ToString(), ["amount"] = claimDollars.ToString("N0", CultureInfo.InvariantCulture) },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnPaymentFailedNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<PaymentFailedEvent>
{
    public async Task Handle(PaymentFailedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var account = await db.BillingAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == e.BillingAccountId, ct).ConfigureAwait(false);
        if (account is null) return;

        await m.Send(new NotifyUserCommand(
            account.LandlordUserId, "payment_failed",
            "Payment Failed",
            "A payment has failed. Please check your billing details.",
            new() { ["invoiceId"] = e.InvoiceId.ToString() },
            Channels.InAppOnly, e.BillingAccountId, "BillingAccount"), ct).ConfigureAwait(false);
    }
}

public sealed class OnDamageClaimApprovedNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<DamageClaimApprovedEvent>
{
    public async Task Handle(DamageClaimApprovedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == e.DealId, ct).ConfigureAwait(false);
        if (app is null) return;

        var approvedDollars = (long)Math.Ceiling(e.ApprovedAmountCents / 100m);
        var approvedDollarsLabel = approvedDollars.ToString("N0", CultureInfo.InvariantCulture);
        await m.Send(new NotifyUserCommand(
            e.TenantUserId, "damage_claim_approved",
            "Damage Claim Approved",
            $"A damage claim of ${approvedDollarsLabel} has been approved. The amount will be deducted from your deposit.",
            new() { ["dealId"] = e.DealId.ToString(), ["amount"] = approvedDollarsLabel },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);

        await m.Send(new NotifyUserCommand(
            app.LandlordUserId, "damage_claim_approved",
            "Damage Claim Approved",
            $"Your damage claim of ${approvedDollarsLabel} has been approved.",
            new() { ["dealId"] = e.DealId.ToString(), ["amount"] = approvedDollarsLabel },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnDamageClaimRejectedNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<DamageClaimRejectedEvent>
{
    public async Task Handle(DamageClaimRejectedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == e.DealId, ct).ConfigureAwait(false);
        if (app is null) return;

        await m.Send(new NotifyUserCommand(
            e.TenantUserId, "damage_claim_rejected",
            "Damage Claim Rejected",
            "A damage claim against your deposit has been rejected. Your full deposit will be returned.",
            new() { ["dealId"] = e.DealId.ToString() },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);

        await m.Send(new NotifyUserCommand(
            app.LandlordUserId, "damage_claim_rejected",
            "Damage Claim Rejected",
            "Your damage claim has been rejected after review.",
            new() { ["dealId"] = e.DealId.ToString() },
            Channels.EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}
