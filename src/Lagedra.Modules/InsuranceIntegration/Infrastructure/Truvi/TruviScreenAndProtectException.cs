namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;

public sealed class TruviScreenAndProtectException : Exception
{
    public TruviScreenAndProtectException()
    {
    }

    public TruviScreenAndProtectException(string message)
        : base(message)
    {
    }

    public TruviScreenAndProtectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public int? Status { get; init; }

    public string? Title { get; init; }

    public string? Detail { get; init; }
}
