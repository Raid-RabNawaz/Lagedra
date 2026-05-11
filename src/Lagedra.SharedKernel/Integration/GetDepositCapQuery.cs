using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.SharedKernel.Integration;

public sealed record GetDepositCapQuery(
    string JurisdictionCode,
    long MonthlyRentCents,
    string? Condition = null) : IRequest<Result<DepositCapResultDto>>;

public sealed record DepositCapResultDto(
    long MaxDepositCents,
    decimal MultiplierApplied,
    string LegalReference);
