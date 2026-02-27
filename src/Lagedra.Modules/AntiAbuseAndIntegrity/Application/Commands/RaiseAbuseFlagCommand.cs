using Lagedra.Modules.AntiAbuseAndIntegrity.Application.DTOs;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;

public sealed record RaiseAbuseFlagCommand(
    Guid UserId,
    FraudFlagType FlagType,
    Severity Severity) : IRequest<Result<AbuseFlagDto>>;

public sealed class RaiseAbuseFlagCommandHandler(
    IntegrityDbContext dbContext)
    : IRequestHandler<RaiseAbuseFlagCommand, Result<AbuseFlagDto>>
{
    public async Task<Result<AbuseFlagDto>> Handle(RaiseAbuseFlagCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flag = FraudFlag.Create(request.UserId, request.FlagType, request.Severity);
        dbContext.FraudFlags.Add(flag);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AbuseFlagDto>.Success(
            new AbuseFlagDto(flag.Id, flag.UserId, flag.FlagType, flag.Severity, flag.FlaggedAt));
    }
}
