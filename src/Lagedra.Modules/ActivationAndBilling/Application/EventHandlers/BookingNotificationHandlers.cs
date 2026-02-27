using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

internal static class Channels
{
    internal static readonly NotificationChannel[] EmailAndInApp = [NotificationChannel.Email, NotificationChannel.InApp];
    internal static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];
}

public sealed class OnApplicationSubmittedNotify(BillingDbContext db, IMediator m)
    : IDomainEventHandler<ApplicationSubmittedEvent>
{
    public async Task Handle(ApplicationSubmittedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var app = await db.DealApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == e.ApplicationId, ct).ConfigureAwait(false);
        if (app is null) return;

        await m.Send(new NotifyUserCommand(
            app.LandlordUserId, "application_submitted",
            "New Booking Application",
            "A tenant has submitted a booking application for your listing.",
            new() { ["applicationId"] = e.ApplicationId.ToString(), ["listingId"] = e.ListingId.ToString() },
            Channels.EmailAndInApp, e.ListingId, "Listing"), ct).ConfigureAwait(false);
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

        var refundInfo = e.RefundAmountCents > 0
            ? $"A refund of ${e.RefundAmountCents / 100m:F2} will be processed."
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
        await m.Send(new NotifyUserCommand(
            e.TenantUserId, "damage_claim_filed",
            "Damage Claim Filed",
            $"A damage claim of ${e.ClaimedAmountCents / 100m:F2} has been filed for your stay.",
            new() { ["dealId"] = e.DealId.ToString(), ["amount"] = $"{e.ClaimedAmountCents / 100m:F2}" },
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
