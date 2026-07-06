using System;
using System.Linq;
using FluentAssertions;
using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Domain.Events;
using Xunit;

namespace Lagedra.Tests.Unit.Arbitration.Domain;

public class ArbitrationCaseFilingFeeTests
{
    private static readonly Guid Deal = Guid.NewGuid();
    private static readonly Guid Filer = Guid.NewGuid();

    private static ArbitrationCase FileWithFee(long feeCents) =>
        ArbitrationCase.File(
            Deal, Filer,
            ArbitrationTier.ProtocolAdjudication,
            ArbitrationCategory.CategoryC,
            feeCents);

    [Fact]
    public void File_with_a_fee_parks_the_case_in_pending_payment_without_filing_event()
    {
        var c = FileWithFee(4900);

        c.Status.Should().Be(ArbitrationStatus.PendingPayment);
        c.FilingFeeCents.Should().Be(4900);
        c.FilingFeePaidAt.Should().BeNull();
        c.DomainEvents.OfType<CaseFiledEvent>().Should().BeEmpty();
    }

    [Fact]
    public void File_with_zero_fee_is_active_immediately()
    {
        var c = FileWithFee(0);

        c.Status.Should().Be(ArbitrationStatus.Filed);
        c.FilingFeePaidAt.Should().NotBeNull();
        c.DomainEvents.OfType<CaseFiledEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Marking_the_fee_paid_activates_the_case_and_raises_filing_event()
    {
        var c = FileWithFee(4900);

        c.MarkFilingFeePaid();

        c.Status.Should().Be(ArbitrationStatus.Filed);
        c.FilingFeePaidAt.Should().NotBeNull();
        c.DomainEvents.OfType<CaseFiledEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Marking_the_fee_paid_twice_is_idempotent()
    {
        var c = FileWithFee(4900);

        c.MarkFilingFeePaid();
        c.MarkFilingFeePaid();

        c.Status.Should().Be(ArbitrationStatus.Filed);
        c.DomainEvents.OfType<CaseFiledEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Recording_a_payment_intent_is_only_allowed_while_pending_payment()
    {
        var c = FileWithFee(4900);

        c.RecordFilingFeePaymentIntent("pi_123");
        c.FilingFeePaymentIntentId.Should().Be("pi_123");

        c.MarkFilingFeePaid();

        var act = () => c.RecordFilingFeePaymentIntent("pi_456");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void An_unpaid_case_cannot_have_an_arbitrator_assigned()
    {
        var c = FileWithFee(4900);

        var act = () => c.AssignArbitrator(Guid.NewGuid(), concurrentCaseCount: 0);

        act.Should().Throw<InvalidOperationException>();
    }
}
