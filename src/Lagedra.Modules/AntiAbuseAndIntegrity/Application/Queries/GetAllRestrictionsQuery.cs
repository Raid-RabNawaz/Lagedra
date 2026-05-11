using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Queries;

public sealed record AdminRestrictionDto(
    Guid Id,
    Guid UserId,
    string RestrictionType,
    string Reason,
    DateTime AppliedAt);

public sealed record GetAllRestrictionsQuery : IRequest<Result<IReadOnlyList<AdminRestrictionDto>>>;

public sealed class GetAllRestrictionsQueryHandler(IntegrityDbContext dbContext)
    : IRequestHandler<GetAllRestrictionsQuery, Result<IReadOnlyList<AdminRestrictionDto>>>
{
    public async Task<Result<IReadOnlyList<AdminRestrictionDto>>> Handle(
        GetAllRestrictionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restrictions = await dbContext.AccountRestrictions
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.AppliedAt)
            .Select(r => new AdminRestrictionDto(
                r.Id,
                r.UserId,
                r.RestrictionLevel.ToString(),
                r.Reason,
                r.AppliedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<AdminRestrictionDto>>.Success(restrictions);
    }
}
