using Lagedra.Modules.AntiAbuseAndIntegrity.Application.DTOs;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Queries;

public sealed record GetUserRestrictionsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<AccountRestrictionDto>>>;

public sealed class GetUserRestrictionsQueryHandler(IntegrityDbContext dbContext)
    : IRequestHandler<GetUserRestrictionsQuery, Result<IReadOnlyList<AccountRestrictionDto>>>
{
    public async Task<Result<IReadOnlyList<AccountRestrictionDto>>> Handle(
        GetUserRestrictionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restrictions = await dbContext.AccountRestrictions
            .AsNoTracking()
            .Where(r => r.UserId == request.UserId)
            .OrderByDescending(r => r.AppliedAt)
            .Select(r => new AccountRestrictionDto(
                r.Id, r.UserId, r.RestrictionLevel, r.AppliedAt, r.Reason))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<AccountRestrictionDto>>.Success(restrictions);
    }
}
