using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Entities;

public sealed class Invoice : Entity<Guid>
{
    public Guid BillingAccountId { get; private set; }
    public string? StripeInvoiceId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public int AmountCents { get; private set; }
    public int? ProrationDays { get; private set; }
    public InvoiceStatus Status { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Invoice() { }

    public static Invoice Create(
        Guid billingAccountId,
        DateTime periodStart,
        DateTime periodEnd,
        int amountCents,
        int? prorationDays = null,
        string? stripeInvoiceId = null)
    {
        if (amountCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountCents), "Amount cannot be negative.");
        }

        return new Invoice
        {
            Id = Guid.NewGuid(),
            BillingAccountId = billingAccountId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AmountCents = amountCents,
            ProrationDays = prorationDays,
            StripeInvoiceId = stripeInvoiceId,
            Status = InvoiceStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkPaid()
    {
        if (Status != InvoiceStatus.Pending && Status != InvoiceStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot mark invoice as paid in status '{Status}'.");
        }

        Status = InvoiceStatus.Paid;
    }

    public void MarkFailed()
    {
        if (Status != InvoiceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark invoice as failed in status '{Status}'.");
        }

        Status = InvoiceStatus.Failed;
        _domainEvents.Add(new PaymentFailedEvent(Id, BillingAccountId, AmountCents));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
