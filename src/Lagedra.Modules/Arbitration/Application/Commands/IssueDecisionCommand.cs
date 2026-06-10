using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Domain.Entities;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Domain.Policies;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record IssueDecisionCommand(
    Guid CaseId,
    ArbitrationUserContext Caller,
    string DecisionSummary,
    decimal? AwardAmount,
    bool IsStructured,
    DecisionOutcome? Outcome,
    DecisionSeverity? Severity,
    IReadOnlyList<DecisionPenaltyInput> Penalties) : IRequest<Result<DecisionDto>>;

public sealed record DecisionPenaltyInput(
    Guid PartyUserId,
    PenaltyType PenaltyType,
    long? AmountCents,
    string? Description);

public sealed class IssueDecisionCommandHandler(
    ArbitrationDbContext dbContext,
    ArbitrationCaseAccessEvaluator accessEvaluator)
    : IRequestHandler<IssueDecisionCommand, Result<DecisionDto>>
{
    public async Task<Result<DecisionDto>> Handle(IssueDecisionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await accessEvaluator
            .RequireAsync(request.CaseId, request.Caller, CaseAccessLevel.DecideOrClose, cancellationToken)
            .ConfigureAwait(false);

        if (!access.IsSuccess)
        {
            return Result<DecisionDto>.Failure(access.Error);
        }

        var arbitrationCase = await dbContext.ArbitrationCases
            .Include(c => c.ArbitratorAssignments)
            .Include(c => c.DecisionPenalties)
            .FirstAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        var participants = access.Value.Participants;

        try
        {
            var penaltyTuples = request.Penalties
                .Select(p => (p.PartyUserId, p.PenaltyType, p.AmountCents))
                .ToList();

            StructuredVerdictPolicy.Validate(
                request.IsStructured,
                request.Outcome,
                request.Severity,
                penaltyTuples,
                participants.LandlordUserId,
                participants.TenantUserId);

            var penaltyEntities = request.Penalties
                .Select(p => DecisionPenalty.Create(
                    arbitrationCase.Id,
                    p.PartyUserId,
                    p.PenaltyType,
                    p.AmountCents,
                    p.Description))
                .ToList();

            arbitrationCase.IssueDecision(
                request.DecisionSummary,
                request.AwardAmount,
                request.IsStructured,
                request.Outcome,
                request.Severity,
                penaltyEntities);

            foreach (var penalty in penaltyEntities)
            {
                dbContext.Entry(penalty).State = EntityState.Added;
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<DecisionDto>.Success(MapDecision(arbitrationCase));
        }
        catch (InvalidOperationException ex)
        {
            return Result<DecisionDto>.Failure(new Error("Arbitration.InvalidVerdict", ex.Message));
        }
    }

    internal static DecisionDto MapDecision(Domain.Aggregates.ArbitrationCase c) =>
        new(
            c.DecisionSummary!,
            c.AwardAmount,
            c.DecidedAt!.Value,
            c.IsStructuredVerdict,
            c.DecisionOutcome,
            c.DecisionSeverity,
            c.DecisionPenalties
                .Select(p => new DecisionPenaltyDto(
                    p.Id,
                    p.PartyUserId,
                    p.PenaltyType,
                    p.AmountCents,
                    p.Description))
                .ToList());
}
