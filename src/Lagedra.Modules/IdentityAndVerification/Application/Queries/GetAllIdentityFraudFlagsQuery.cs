using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

public sealed record IdentityFraudFlagAdminDto(
    Guid Id,
    Guid UserId,
    string Reason,
    string Source,
    DateTime DetectedAt,
    DateTime SlaDeadline,
    bool IsResolved,
    bool IsEscalated,
    string? UserEmail,
    string? UserDisplayName);

public sealed record GetAllIdentityFraudFlagsQuery
    : IRequest<Result<IReadOnlyList<IdentityFraudFlagAdminDto>>>;

public sealed class GetAllIdentityFraudFlagsQueryHandler(
    IdentityDbContext dbContext,
    IUserLookupService userLookup)
    : IRequestHandler<GetAllIdentityFraudFlagsQuery, Result<IReadOnlyList<IdentityFraudFlagAdminDto>>>
{
    public async Task<Result<IReadOnlyList<IdentityFraudFlagAdminDto>>> Handle(
        GetAllIdentityFraudFlagsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flags = await dbContext.FraudFlags
            .AsNoTracking()
            .OrderByDescending(f => f.RaisedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<IdentityFraudFlagAdminDto>(flags.Count);
        foreach (var flag in flags)
        {
            var account = await userLookup
                .FindAccountByIdAsync(flag.UserId, cancellationToken)
                .ConfigureAwait(false);

            items.Add(new IdentityFraudFlagAdminDto(
                flag.Id,
                flag.UserId,
                flag.Reason,
                flag.Source,
                flag.RaisedAt,
                flag.SlaDeadline,
                flag.ResolvedAt is not null,
                flag.IsEscalated,
                account?.Email,
                account?.DisplayName));
        }

        return Result<IReadOnlyList<IdentityFraudFlagAdminDto>>.Success(items);
    }
}
