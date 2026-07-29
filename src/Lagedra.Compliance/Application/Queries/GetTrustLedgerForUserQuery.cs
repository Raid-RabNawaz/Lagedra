using Lagedra.Compliance.Application.DTOs;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Queries;

/// <summary>
/// Returns the trust ledger for a user. By default this is the public,
/// pseudonymized view (entries marked IsPublic only). When
/// <see cref="GetTrustLedgerForUserQuery.IncludeNonPublic"/> is set — the user
/// viewing their own ledger, or a platform admin — every entry is returned.
/// </summary>
public sealed record GetTrustLedgerForUserQuery(Guid UserId, bool IncludeNonPublic = false)
    : IRequest<Result<IReadOnlyList<TrustLedgerEntryDto>>>;

public sealed class GetTrustLedgerForUserQueryHandler(ComplianceDbContext dbContext)
    : IRequestHandler<GetTrustLedgerForUserQuery, Result<IReadOnlyList<TrustLedgerEntryDto>>>
{
    public async Task<Result<IReadOnlyList<TrustLedgerEntryDto>>> Handle(
        GetTrustLedgerForUserQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entries = await dbContext.TrustLedgerEntries
            .AsNoTracking()
            .Where(e => e.UserId == request.UserId && (request.IncludeNonPublic || e.IsPublic))
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new TrustLedgerEntryDto(
                e.Id, e.UserId, e.EntryType, e.ReferenceId,
                e.Description, e.OccurredAt, e.IsPublic))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<TrustLedgerEntryDto>>.Success(entries);
    }
}
