using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
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
    bool IsResolved);

public sealed record GetAllFraudFlagsQuery : IRequest<Result<IReadOnlyList<FraudFlagAdminDto>>>;

public sealed class GetAllFraudFlagsQueryHandler(IntegrityDbContext dbContext)
    : IRequestHandler<GetAllFraudFlagsQuery, Result<IReadOnlyList<FraudFlagAdminDto>>>
{
    public async Task<Result<IReadOnlyList<FraudFlagAdminDto>>> Handle(
        GetAllFraudFlagsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flags = await dbContext.FraudFlags
            .AsNoTracking()
            .OrderByDescending(f => f.FlaggedAt)
            .Select(f => new FraudFlagAdminDto(
                f.Id,
                f.UserId,
                f.Severity.ToString(),
                f.FlagType.ToString(),
                f.FlaggedAt,
                f.IsDeleted))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<FraudFlagAdminDto>>.Success(flags);
    }
}
