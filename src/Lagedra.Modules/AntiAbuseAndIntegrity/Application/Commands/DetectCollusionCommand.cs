using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;

public sealed record DetectCollusionCommand(
    Guid PartyAUserId,
    Guid PartyBUserId,
    int RepeatedDealCount,
    DateTime FirstOccurrence,
    DateTime LatestOccurrence) : IRequest<Result>;

public sealed class DetectCollusionCommandHandler(
    IntegrityDbContext dbContext)
    : IRequestHandler<DetectCollusionCommand, Result>
{
    public async Task<Result> Handle(DetectCollusionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var abuseCase = AbuseCase.Open(request.PartyAUserId, AbuseType.Collusion);
        dbContext.AbuseCases.Add(abuseCase);

        var pattern = CollusionPattern.Create(
            abuseCase.Id,
            request.PartyAUserId,
            request.PartyBUserId,
            request.RepeatedDealCount,
            request.FirstOccurrence,
            request.LatestOccurrence);

        dbContext.CollusionPatterns.Add(pattern);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
