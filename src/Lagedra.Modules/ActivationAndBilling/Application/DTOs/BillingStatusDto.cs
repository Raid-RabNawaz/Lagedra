using Lagedra.Modules.ActivationAndBilling.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record BillingStatusDto(
    Guid BillingAccountId,
    Guid DealId,
    BillingAccountStatus Status,
    DateTime StartDate,
    DateTime? EndDate,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    int TotalInvoices,
    int PaidInvoices);
