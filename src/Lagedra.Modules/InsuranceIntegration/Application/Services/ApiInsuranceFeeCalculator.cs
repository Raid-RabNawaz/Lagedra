using System.Net.Http.Json;
using Lagedra.SharedKernel.Insurance;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

public sealed class ApiInsuranceFeeCalculator(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
    : IInsuranceFeeCalculator
{
    public async Task<InsuranceFeeQuote> CalculateFeeAsync(
        long monthlyRentCents,
        int stayDurationDays,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("InsurancePartner");
        var baseUrl = configuration["Insurance:ApiBaseUrl"]
            ?? throw new InvalidOperationException("Insurance:ApiBaseUrl is not configured.");

        var response = await client.PostAsJsonAsync(
            $"{baseUrl}/v1/quotes",
            new { monthlyRentCents, stayDurationDays },
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var quoteResponse = await response.Content
            .ReadFromJsonAsync<InsuranceQuoteApiResponse>(ct)
            .ConfigureAwait(false);

        return new InsuranceFeeQuote(
            quoteResponse?.FeeCents ?? 0,
            quoteResponse?.Provider ?? "API",
            quoteResponse?.QuoteReference);
    }

#pragma warning disable CA1812 // Instantiated by System.Text.Json deserializer via ReadFromJsonAsync<T>()
    private sealed record InsuranceQuoteApiResponse(
        long FeeCents,
        string? Provider,
        string? QuoteReference);
#pragma warning restore CA1812
}
