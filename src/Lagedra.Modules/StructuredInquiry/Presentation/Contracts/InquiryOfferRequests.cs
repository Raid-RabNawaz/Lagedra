namespace Lagedra.Modules.StructuredInquiry.Presentation.Contracts;

public sealed record ProposeInquiryOfferRequest(
    long RentCents,
    long DepositCents,
    string? Note = null);

public sealed record CounterInquiryOfferRequest(
    long RentCents,
    long DepositCents,
    string? Note = null);
