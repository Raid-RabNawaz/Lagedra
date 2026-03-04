using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Queries;

public sealed record ListCasesByStatusQuery(ArbitrationStatus Status) : IRequest<Result<IReadOnlyList<CaseDto>>>;

public sealed class ListCasesByStatusQueryHandler(ArbitrationDbContext dbContext)
    : IRequestHandler<ListCasesByStatusQuery, Result<IReadOnlyList<CaseDto>>>
{
    public async Task<Result<IReadOnlyList<CaseDto>>> Handle(
        ListCasesByStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cases = await dbContext.ArbitrationCases
            .AsNoTracking()
            .Include(c => c.EvidenceSlots)
            .Where(c => c.Status == request.Status)
            .OrderByDescending(c => c.FiledAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CaseDto> dtos = cases.Select(GetCaseQueryHandler.MapToDto).ToList();
        return Result<IReadOnlyList<CaseDto>>.Success(dtos);
    }
}
