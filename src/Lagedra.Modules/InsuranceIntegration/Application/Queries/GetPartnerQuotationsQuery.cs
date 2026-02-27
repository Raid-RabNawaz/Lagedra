using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.InsuranceIntegration.Application.Queries;

public sealed record GetPartnerQuotationsQuery(
    long MonthlyRentCents,
    int StayDurationDays) : IRequest<Result<PartnerQuotationsDto>>;

public sealed record PartnerQuotationsDto(
    InsuranceFeeQuote PlatformQuote,
    IReadOnlyList<InsuranceFeeQuote> PartnerQuotes);

public sealed class GetPartnerQuotationsQueryHandler(
    IInsuranceFeeCalculator feeCalculator)
    : IRequestHandler<GetPartnerQuotationsQuery, Result<PartnerQuotationsDto>>
{
    public async Task<Result<PartnerQuotationsDto>> Handle(
        GetPartnerQuotationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var platformQuote = await feeCalculator
            .CalculateFeeAsync(request.MonthlyRentCents, request.StayDurationDays, cancellationToken)
            .ConfigureAwait(false);

        // Partner quotes would come from external MGA APIs in production.
        // For now, only the platform quote is returned.
        var partnerQuotes = new List<InsuranceFeeQuote>();

        return Result<PartnerQuotationsDto>.Success(
            new PartnerQuotationsDto(platformQuote, partnerQuotes));
    }
}
