namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record CheckoutDto(
    string ClientSecret,
    string PaymentIntentId,
    string Status,
    long TotalAmountCents,
    long FirstMonthRentCents,
    long DepositAmountCents,
    long InsuranceFeeCents,
    long ApplicationFeeCents,
    long ServiceFeeCents,
    string Currency);
