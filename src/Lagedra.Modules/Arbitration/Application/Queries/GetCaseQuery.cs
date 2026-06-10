using Lagedra.Modules.Arbitration.Application.Commands;
using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Queries;

public sealed record GetCaseQuery(Guid CaseId, ArbitrationUserContext Caller) : IRequest<Result<CaseDto>>;

public sealed class GetCaseQueryHandler(
    ArbitrationDbContext dbContext,
    ArbitrationCaseAccessEvaluator accessEvaluator,
    IUserEmailResolver emailResolver)
    : IRequestHandler<GetCaseQuery, Result<CaseDto>>
{
    public async Task<Result<CaseDto>> Handle(GetCaseQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await accessEvaluator
            .RequireAsync(request.CaseId, request.Caller, CaseAccessLevel.View, cancellationToken)
            .ConfigureAwait(false);

        if (!access.IsSuccess)
        {
            return Result<CaseDto>.Failure(access.Error);
        }

        var arbitrationCase = await dbContext.ArbitrationCases
            .AsNoTracking()
            .Include(c => c.EvidenceSlots)
            .Include(c => c.ArbitratorAssignments)
            .Include(c => c.DecisionPenalties)
            .FirstAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        return Result<CaseDto>.Success(
            await MapToDtoAsync(arbitrationCase, access.Value.Participants, cancellationToken)
                .ConfigureAwait(false));
    }

    internal async Task<CaseDto> MapToDtoAsync(
        ArbitrationCase c,
        DealParticipantsDto? participants,
        CancellationToken cancellationToken)
    {
        var latestAssignment = c.ArbitratorAssignments
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault();

        string? arbitratorEmail = null;
        if (latestAssignment is not null)
        {
            arbitratorEmail = await emailResolver
                .GetEmailAsync(latestAssignment.ArbitratorUserId, cancellationToken)
                .ConfigureAwait(false);
        }

        return MapToDto(c, participants, latestAssignment?.ArbitratorUserId, arbitratorEmail);
    }

    internal static CaseDto MapToDto(
        ArbitrationCase c,
        DealParticipantsDto? participants = null,
        Guid? assignedArbitratorUserId = null,
        string? assignedArbitratorEmail = null)
    {
        var arbitratorId = assignedArbitratorUserId
            ?? c.ArbitratorAssignments
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefault()
                ?.ArbitratorUserId;

        var (activeDecision, priorDecision) = MapDecisions(c);

        return new CaseDto(
            c.Id,
            c.DealId,
            c.FiledByUserId,
            participants?.LandlordUserId,
            participants?.TenantUserId,
            c.Tier,
            c.Category,
            c.Status,
            c.FilingFeeCents,
            c.FiledAt,
            c.EvidenceCompleteAt,
            c.DecisionDueAt,
            c.EvidenceSlots.Count,
            arbitratorId,
            assignedArbitratorEmail,
            activeDecision,
            priorDecision,
            c.EvidenceSlots.Select(s => new EvidenceSlotDto(
                s.Id, s.SlotType, s.SubmittedBy, s.EvidenceManifestId, s.SubmittedAt)).ToList());
    }

    internal static (DecisionDto? Active, DecisionDto? Prior) MapDecisions(Domain.Aggregates.ArbitrationCase c)
    {
        if (!c.DecidedAt.HasValue || string.IsNullOrWhiteSpace(c.DecisionSummary))
        {
            return (null, null);
        }

        var mapped = IssueDecisionCommandHandler.MapDecision(c);

        return c.Status is Domain.Enums.ArbitrationStatus.Decided or Domain.Enums.ArbitrationStatus.Closed
            ? (mapped, null)
            : (null, mapped);
    }
}
