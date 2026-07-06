using Lagedra.Modules.ActivationAndBilling.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

/// <summary>
/// A single monthly platform-fee line item on a host's billing statement.
/// Mirrors a Stripe subscription invoice recorded via the billing webhooks.
/// </summary>
public sealed record InvoiceDto(
    Guid InvoiceId,
    Guid DealId,
    string? ListingTitle,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int AmountCents,
    InvoiceStatus Status,
    DateTime CreatedAt);
