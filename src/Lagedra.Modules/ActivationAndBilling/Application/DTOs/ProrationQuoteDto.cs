namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record ProrationQuoteDto(
    DateTime StartDate,
    DateTime EndDate,
    int TotalDays,
    long ProratedAmountCents,
    long MonthlyFeeCents,
    string Currency);
