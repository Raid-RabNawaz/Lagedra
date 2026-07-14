using System;
using System.Linq;
using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.SharedKernel.Time;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Domain;

public class DepositReturnHandshakeTests
{
    private static readonly Guid Deal = Guid.NewGuid();
    private static readonly Guid Host = Guid.NewGuid();
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid DamageEvidence = Guid.NewGuid();

    private sealed class MutableClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DealFinancials Financials(long deposit = 200_000) =>
        DealFinancials.Create(
            firstMonthRentCents: 300_000,
            depositAmountCents: deposit,
            insuranceFeeCents: 10_000,
            monthlyProtocolFeeCents: 7_900,
            serviceFeeCents: 5_000);

    private static DealPaymentConfirmation ConfirmedWithDeposit(
        IClock clock, long deposit = 200_000)
    {
        var confirmation = DealPaymentConfirmation.Create(Deal, Financials(deposit), clock);
        confirmation.ConfirmByStripe(clock);
        confirmation.ClearDomainEvents();
        return confirmation;
    }

    [Fact]
    public void BeginMoveOut_requires_a_confirmed_booking()
    {
        var clock = new MutableClock();
        var pending = DealPaymentConfirmation.Create(Deal, Financials(), clock);

        var act = () => pending.BeginMoveOut(Host, clock);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void BeginMoveOut_is_idempotent_and_keeps_the_first_initiator()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        confirmation.BeginMoveOut(Host, clock);
        var firstAt = confirmation.MoveOutInitiatedAt;

        clock.UtcNow = clock.UtcNow.AddHours(2);
        confirmation.BeginMoveOut(Tenant, clock);

        confirmation.MoveOutInitiatedByUserId.Should().Be(Host);
        confirmation.MoveOutInitiatedAt.Should().Be(firstAt);
    }

    [Fact]
    public void Host_then_tenant_confirmation_settles_and_raises_event()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        confirmation.ConfirmDepositReturnedByHost(200_000, "Zelle", "ref#42", null, clock);

        confirmation.HostConfirmedDepositReturnedAt.Should().NotBeNull();
        confirmation.DepositReturnAmountCents.Should().Be(200_000);
        confirmation.DepositReturnMethod.Should().Be("Zelle");
        confirmation.DepositReturnNote.Should().Be("ref#42");
        confirmation.DepositReturnSettledAt.Should().BeNull("the tenant has not confirmed yet");

        confirmation.ConfirmDepositReceivedByTenant(clock);

        confirmation.DepositReturnSettledAt.Should().NotBeNull();
        confirmation.DomainEvents.OfType<DepositReturnSettledEvent>()
            .Should().ContainSingle();
    }

    [Fact]
    public void Tenant_then_host_confirmation_also_settles()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        confirmation.ConfirmDepositReceivedByTenant(clock);
        confirmation.DepositReturnSettledAt.Should().BeNull();

        confirmation.ConfirmDepositReturnedByHost(
            150_000, "Cash", "Broken lamp — replaced", DamageEvidence, clock);

        confirmation.DepositReturnSettledAt.Should().NotBeNull();
        confirmation.DepositReturnEvidenceManifestId.Should().Be(DamageEvidence);
        confirmation.DomainEvents.OfType<DepositReturnSettledEvent>()
            .Should().ContainSingle();
    }

    [Fact]
    public void Partial_return_requires_deduction_reason()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        var act = () => confirmation.ConfirmDepositReturnedByHost(
            150_000, "Cash", null, DamageEvidence, clock);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*reason*");
    }

    [Fact]
    public void Partial_return_requires_damage_evidence()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        var act = () => confirmation.ConfirmDepositReturnedByHost(
            150_000, "Cash", "Wall damage", null, clock);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*damage photo*");
    }

    [Fact]
    public void Confirmations_are_noop_after_settled()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        confirmation.ConfirmDepositReturnedByHost(200_000, "Zelle", null, null, clock);
        confirmation.ConfirmDepositReceivedByTenant(clock);
        var settledAt = confirmation.DepositReturnSettledAt;
        confirmation.ClearDomainEvents();

        // A late duplicate host confirm must not change the settled amount,
        // move the settled timestamp, or re-raise the completion event.
        confirmation.ConfirmDepositReturnedByHost(999_999, "Other", "late", null, clock);

        confirmation.DepositReturnAmountCents.Should().Be(200_000);
        confirmation.DepositReturnSettledAt.Should().Be(settledAt);
        confirmation.DomainEvents.OfType<DepositReturnSettledEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Host_confirm_requires_a_deposit_to_return()
    {
        var clock = new MutableClock();
        var noDeposit = ConfirmedWithDeposit(clock, deposit: 0);

        var act = () => noDeposit.ConfirmDepositReturnedByHost(0, "Cash", null, null, clock);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDepositReturnedByPlatform_settles_both_sides()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        confirmation.MarkDepositReturnedByPlatform(200_000, clock);

        confirmation.HostConfirmedDepositReturnedAt.Should().NotBeNull();
        confirmation.TenantConfirmedDepositReceivedAt.Should().NotBeNull();
        confirmation.DepositReturnMethod.Should().Be("PlatformStripe");
        confirmation.DepositReturnSettledAt.Should().NotBeNull();
        confirmation.DomainEvents.OfType<DepositReturnSettledEvent>()
            .Should().ContainSingle();
    }

    [Fact]
    public void Reminder_is_due_when_open_and_not_recently_sent()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);

        confirmation.DepositReturnReminderDue(clock, reminderIntervalDays: 7)
            .Should().BeTrue();

        confirmation.MarkDepositReturnReminderSent(clock);

        confirmation.DepositReturnReminderDue(clock, reminderIntervalDays: 7)
            .Should().BeFalse("a reminder was just sent");

        clock.UtcNow = clock.UtcNow.AddDays(8);
        confirmation.DepositReturnReminderDue(clock, reminderIntervalDays: 7)
            .Should().BeTrue("the interval has elapsed");
    }

    [Fact]
    public void Reminder_is_not_due_once_settled()
    {
        var clock = new MutableClock();
        var confirmation = ConfirmedWithDeposit(clock);
        confirmation.MarkDepositReturnedByPlatform(200_000, clock);

        confirmation.DepositReturnReminderDue(clock, reminderIntervalDays: 7)
            .Should().BeFalse();
    }
}
