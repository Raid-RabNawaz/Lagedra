using Lagedra.Modules.ActivationAndBilling.Domain.Entities;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

public sealed class BillingAccount : AggregateRoot<Guid>
{
    public Guid DealId { get; private set; }
    public Guid LandlordUserId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public BillingAccountStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }

    private readonly List<Invoice> _invoices = [];
    public IReadOnlyList<Invoice> Invoices => _invoices.AsReadOnly();

    private BillingAccount() { }

    public static BillingAccount Create(
        Guid dealId,
        Guid landlordUserId,
        Guid tenantUserId,
        DateTime startDate)
    {
        return new BillingAccount
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            LandlordUserId = landlordUserId,
            TenantUserId = tenantUserId,
            Status = BillingAccountStatus.Inactive,
            StartDate = startDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        if (Status != BillingAccountStatus.Inactive)
        {
            throw new InvalidOperationException($"Cannot activate billing account in status '{Status}'.");
        }

        Status = BillingAccountStatus.Active;

        AddDomainEvent(new DealActivatedEvent(Id, DealId, LandlordUserId, TenantUserId));
    }

    public void Suspend()
    {
        if (Status != BillingAccountStatus.Active)
        {
            throw new InvalidOperationException($"Cannot suspend billing account in status '{Status}'.");
        }

        Status = BillingAccountStatus.Suspended;
    }

    public void Resume()
    {
        if (Status != BillingAccountStatus.Suspended)
        {
            throw new InvalidOperationException($"Cannot resume billing account in status '{Status}'.");
        }

        Status = BillingAccountStatus.Active;
    }

    public void Close()
    {
        if (Status is BillingAccountStatus.Closed)
        {
            throw new InvalidOperationException("Billing account is already closed.");
        }

        Status = BillingAccountStatus.Closed;
        EndDate = DateTime.UtcNow;

        AddDomainEvent(new BillingStoppedEvent(Id, DealId));
    }

    public void SetStripeCustomerId(string stripeCustomerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeCustomerId);
        StripeCustomerId = stripeCustomerId;
    }

    public void SetStripeSubscriptionId(string stripeSubscriptionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeSubscriptionId);
        StripeSubscriptionId = stripeSubscriptionId;
    }
}
