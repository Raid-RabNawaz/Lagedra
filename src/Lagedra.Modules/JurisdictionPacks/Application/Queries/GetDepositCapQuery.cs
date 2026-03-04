using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Application.Queries;

public sealed record GetDepositCapQuery(
    string JurisdictionCode,
    long MonthlyRentCents,
    string? Condition = null) : IRequest<Result<DepositCapResultDto>>;

public sealed record DepositCapResultDto(
    long MaxDepositCents,
    decimal MultiplierApplied,
    string LegalReference);

public sealed class GetDepositCapQueryHandler(JurisdictionDbContext dbContext)
    : IRequestHandler<GetDepositCapQuery, Result<DepositCapResultDto>>
{
    public async Task<Result<DepositCapResultDto>> Handle(
        GetDepositCapQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = request.JurisdictionCode.ToUpperInvariant();

        var pack = await dbContext.JurisdictionPacks
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.JurisdictionCode.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (pack is null || pack.ActiveVersionId is null)
        {
            return Result<DepositCapResultDto>.Failure(
                new Error("DepositCap.NoActiveJurisdiction",
                    $"No active jurisdiction pack found for '{request.JurisdictionCode}'."));
        }

        var rule = await dbContext.DepositCapRules
            .AsNoTracking()
            .Where(r => r.VersionId == pack.ActiveVersionId.Value
                        && r.JurisdictionCode == code)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rule is null)
        {
            return Result<DepositCapResultDto>.Failure(
                new Error("DepositCap.NoRule",
                    $"No deposit cap rule defined for jurisdiction '{request.JurisdictionCode}'."));
        }

        var multiplier = rule.MaxMultiplier;

        if (!string.IsNullOrWhiteSpace(request.Condition)
            && string.Equals(rule.ExceptionCondition, request.Condition, StringComparison.OrdinalIgnoreCase)
            && rule.ExceptionMultiplier.HasValue)
        {
            multiplier = rule.ExceptionMultiplier.Value;
        }

        var maxDepositCents = (long)(request.MonthlyRentCents * multiplier);

        return Result<DepositCapResultDto>.Success(
            new DepositCapResultDto(maxDepositCents, multiplier, rule.LegalReference));
    }
}
