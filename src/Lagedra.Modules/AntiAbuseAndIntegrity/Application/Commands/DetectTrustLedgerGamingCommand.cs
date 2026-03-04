using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;

public sealed record DetectTrustLedgerGamingCommand(Guid SubjectUserId) : IRequest<Result>;

public sealed class DetectTrustLedgerGamingCommandHandler(
    IntegrityDbContext dbContext)
    : IRequestHandler<DetectTrustLedgerGamingCommand, Result>
{
    public async Task<Result> Handle(DetectTrustLedgerGamingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var abuseCase = AbuseCase.Open(request.SubjectUserId, AbuseType.TrustLedgerGaming);
        dbContext.AbuseCases.Add(abuseCase);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
