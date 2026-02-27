using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public int AmountCents { get; }
    public string Currency { get; }

    public Money(int amountCents, string currency = "USD")
    {
        if (amountCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountCents), "Amount cannot be negative.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        AmountCents = amountCents;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AmountCents;
        yield return Currency;
    }
}
