using Lagedra.Modules.AntiAbuseAndIntegrity.Application.DTOs;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Queries;

public sealed record GetAbuseFlagsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<AbuseFlagDto>>>;

public sealed class GetAbuseFlagsQueryHandler(IntegrityDbContext dbContext)
    : IRequestHandler<GetAbuseFlagsQuery, Result<IReadOnlyList<AbuseFlagDto>>>
{
    public async Task<Result<IReadOnlyList<AbuseFlagDto>>> Handle(
        GetAbuseFlagsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flags = await dbContext.FraudFlags
            .AsNoTracking()
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.FlaggedAt)
            .Select(f => new AbuseFlagDto(f.Id, f.UserId, f.FlagType, f.Severity, f.FlaggedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<AbuseFlagDto>>.Success(flags);
    }
}
