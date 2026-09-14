using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Queries;

public sealed record FraudFlagAdminDto(
    Guid Id,
    Guid UserId,
    string Severity,
    string Category,
    DateTime DetectedAt,
    bool IsResolved,
    string? UserEmail = null,
    string? UserDisplayName = null);

public sealed record GetAllFraudFlagsQuery : IRequest<Result<IReadOnlyList<FraudFlagAdminDto>>>;

public sealed class GetAllFraudFlagsQueryHandler(
    IntegrityDbContext dbContext,
    IUserLookupService userLookup)
    : IRequestHandler<GetAllFraudFlagsQuery, Result<IReadOnlyList<FraudFlagAdminDto>>>
{
    public async Task<Result<IReadOnlyList<FraudFlagAdminDto>>> Handle(
        GetAllFraudFlagsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flags = await dbContext.FraudFlags
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(f => f.FlaggedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<FraudFlagAdminDto>(flags.Count);
        foreach (var flag in flags)
        {
            var account = await userLookup
                .FindAccountByIdAsync(flag.UserId, cancellationToken)
                .ConfigureAwait(false);

            items.Add(new FraudFlagAdminDto(
                flag.Id,
                flag.UserId,
                flag.Severity.ToString(),
                flag.FlagType.ToString(),
                flag.FlaggedAt,
                flag.IsDeleted,
                account?.Email,
                account?.DisplayName));
        }

        return Result<IReadOnlyList<FraudFlagAdminDto>>.Success(items);
    }
}
