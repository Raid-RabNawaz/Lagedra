using Lagedra.Compliance.Application.DTOs;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Queries;

/// <summary>
/// Returns the full compliance view for a deal — restricted to involved parties.
/// Includes all violations and all ledger entries (public + private).
/// </summary>
public sealed record GetFullLedgerForDealQuery(Guid DealId)
    : IRequest<Result<FullDealLedgerDto>>;

public sealed record FullDealLedgerDto(
    Guid DealId,
    IReadOnlyList<ViolationDto> Violations,
    IReadOnlyList<TrustLedgerEntryDto> LedgerEntries);

public sealed class GetFullLedgerForDealQueryHandler(ComplianceDbContext dbContext)
    : IRequestHandler<GetFullLedgerForDealQuery, Result<FullDealLedgerDto>>
{
    public async Task<Result<FullDealLedgerDto>> Handle(
        GetFullLedgerForDealQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await dbContext.Violations
            .AsNoTracking()
            .Where(v => v.DealId == request.DealId)
            .OrderByDescending(v => v.DetectedAt)
            .Select(v => new ViolationDto(
                v.Id, v.DealId, v.ReportedByUserId, v.Category, v.Status,
                v.Description, v.EvidenceReference, v.DetectedAt, v.ResolvedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Ledger entries for users involved in this deal — requires a join through violations
        // For now, collect the distinct user IDs from the deal's violations
        var userIds = violations.Select(v => v.ReportedByUserId).Distinct().ToList();

        var ledgerEntries = await dbContext.TrustLedgerEntries
            .AsNoTracking()
            .Where(e => userIds.Contains(e.UserId) || e.ReferenceId == request.DealId)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new TrustLedgerEntryDto(
                e.Id, e.UserId, e.EntryType, e.ReferenceId,
                e.Description, e.OccurredAt, e.IsPublic))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<FullDealLedgerDto>.Success(
            new FullDealLedgerDto(request.DealId, violations, ledgerEntries));
    }
}
