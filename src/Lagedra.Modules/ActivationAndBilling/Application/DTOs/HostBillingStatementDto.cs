namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

/// <summary>
/// Consolidated view of the monthly platform fees a host is charged across
/// every one of their bookings. Combines a forward-looking summary (what the
/// host will owe each month at the current fee) with a full deduction history.
/// </summary>
public sealed record HostBillingStatementDto(
    int ActiveBookingCount,
    long CurrentMonthlyFeeCents,
    long ProjectedMonthlyTotalCents,
    long TotalPaidToDateCents,
    long TotalOutstandingCents,
    IReadOnlyList<InvoiceDto> Invoices);
