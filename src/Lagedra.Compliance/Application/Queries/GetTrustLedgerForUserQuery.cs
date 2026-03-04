using Lagedra.Compliance.Application.DTOs;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Queries;

/// <summary>
/// Returns the public trust ledger for a user — pseudonymized view
/// showing only entries marked as IsPublic.
/// </summary>
public sealed record GetTrustLedgerForUserQuery(Guid UserId) : IRequest<Result<IReadOnlyList<TrustLedgerEntryDto>>>;

public sealed class GetTrustLedgerForUserQueryHandler(ComplianceDbContext dbContext)
    : IRequestHandler<GetTrustLedgerForUserQuery, Result<IReadOnlyList<TrustLedgerEntryDto>>>
{
    public async Task<Result<IReadOnlyList<TrustLedgerEntryDto>>> Handle(
        GetTrustLedgerForUserQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entries = await dbContext.TrustLedgerEntries
            .AsNoTracking()
            .Where(e => e.UserId == request.UserId && e.IsPublic)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new TrustLedgerEntryDto(
                e.Id, e.UserId, e.EntryType, e.ReferenceId,
                e.Description, e.OccurredAt, e.IsPublic))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<TrustLedgerEntryDto>>.Success(entries);
    }
}
