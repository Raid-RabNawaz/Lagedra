using Lagedra.SharedKernel.Results;
using Lagedra.Modules.AuditLog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AuditLog.Application.Queries;

public sealed record AuditEventDto(
    Guid Id,
    Guid? UserId,
    string EventType,
    string EntityType,
    string EntityId,
    string? Details,
    string? IpAddress,
    DateTime Timestamp);

public sealed record AuditSearchResultDto(
    IReadOnlyList<AuditEventDto> Items,
    int TotalCount);

public sealed record SearchAuditEventsQuery(
    Guid? UserId,
    string? EventType,
    string? EntityType,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<AuditSearchResultDto>>;

public sealed class SearchAuditEventsQueryHandler(AuditDbContext dbContext)
    : IRequestHandler<SearchAuditEventsQuery, Result<AuditSearchResultDto>>
{
    public async Task<Result<AuditSearchResultDto>> Handle(
        SearchAuditEventsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.AuditEvents.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.EventType))
            query = query.Where(a => a.EventType == request.EventType);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (request.StartDate.HasValue)
            query = query.Where(a => a.Timestamp >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(a => a.Timestamp <= request.EndDate.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditEventDto(
                a.Id, a.UserId, a.EventType, a.EntityType, a.EntityId,
                a.Details, a.IpAddress, a.Timestamp))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<AuditSearchResultDto>.Success(new AuditSearchResultDto(items, totalCount));
    }
}
