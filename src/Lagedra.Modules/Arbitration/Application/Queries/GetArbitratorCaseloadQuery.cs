using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Domain.Policies;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Arbitration.Application.Queries;

public sealed record ArbitratorCaseloadDto(
    Guid ArbitratorUserId,
    string Email,
    string? DisplayName,
    int ActiveCaseCount,
    bool IsOverSoftCap,
    bool IsAtHardCap);

public sealed record GetArbitratorCaseloadQuery : IRequest<Result<IReadOnlyList<ArbitratorCaseloadDto>>>;

public sealed class GetArbitratorCaseloadQueryHandler(
    ArbitratorAssignmentSelector assignmentSelector,
    IArbitratorPanelProvider panelProvider)
    : IRequestHandler<GetArbitratorCaseloadQuery, Result<IReadOnlyList<ArbitratorCaseloadDto>>>
{
    public async Task<Result<IReadOnlyList<ArbitratorCaseloadDto>>> Handle(
        GetArbitratorCaseloadQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var panel = await panelProvider.GetPanelMembersAsync(cancellationToken).ConfigureAwait(false);
        var caseloads = await assignmentSelector.GetActiveCaseloadsAsync(cancellationToken).ConfigureAwait(false);

        var dtos = panel
            .Select(m =>
            {
                var count = caseloads.GetValueOrDefault(m.UserId, 0);
                return new ArbitratorCaseloadDto(
                    m.UserId,
                    m.Email,
                    m.DisplayName,
                    count,
                    ArbitratorCaseloadPolicy.IsOverSoftCap(count),
                    ArbitratorCaseloadPolicy.IsAtHardCap(count));
            })
            .OrderBy(d => d.ActiveCaseCount)
            .ThenBy(d => d.Email)
            .ToList();

        return Result<IReadOnlyList<ArbitratorCaseloadDto>>.Success(dtos);
    }
}
