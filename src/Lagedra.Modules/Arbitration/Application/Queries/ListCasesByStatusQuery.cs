using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Queries;

public sealed record ListCasesByStatusQuery(
    ArbitrationStatus Status,
    ArbitrationUserContext Caller) : IRequest<Result<IReadOnlyList<CaseDto>>>;

public sealed class ListCasesByStatusQueryHandler(
    ArbitrationDbContext dbContext,
    IDealApplicationStatusProvider dealProvider)
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
            .Include(c => c.ArbitratorAssignments)
            .Where(c => c.Status == request.Status)
            .OrderByDescending(c => c.FiledAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!request.Caller.IsPlatformAdmin)
        {
            var filtered = new List<Domain.Aggregates.ArbitrationCase>();
            foreach (var c in cases)
            {
                var participants = await dealProvider
                    .GetParticipantsAsync(c.DealId, cancellationToken)
                    .ConfigureAwait(false);

                if (ArbitrationCaseAccess.IsVisibleTo(request.Caller, c, participants))
                {
                    filtered.Add(c);
                }
            }

            cases = filtered;
        }

        IReadOnlyList<CaseDto> dtos = cases.Select(c => GetCaseQueryHandler.MapToDto(c)).ToList();
        return Result<IReadOnlyList<CaseDto>>.Success(dtos);
    }
}
