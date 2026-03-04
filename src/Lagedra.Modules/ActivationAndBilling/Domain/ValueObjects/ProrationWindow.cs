using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;

public sealed class ProrationWindow : ValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public long MonthlyFeeCents { get; }

    public int TotalDays => (int)(EndDate - StartDate).TotalDays;
    public long ProratedAmountCents => (long)(MonthlyFeeCents / 30.0 * TotalDays);

    public ProrationWindow(DateTime startDate, DateTime endDate, long monthlyFeeCents)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.", nameof(endDate));
        }

        StartDate = startDate;
        EndDate = endDate;
        MonthlyFeeCents = monthlyFeeCents;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
        yield return MonthlyFeeCents;
    }
}
