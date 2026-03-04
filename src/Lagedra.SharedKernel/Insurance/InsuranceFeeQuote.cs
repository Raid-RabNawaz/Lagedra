namespace Lagedra.SharedKernel.Insurance;

public sealed record InsuranceFeeQuote(
    long FeeCents,
    string Provider,
    string? QuoteReference);
