using Lagedra.Modules.AntiAbuseAndIntegrity.Application.DTOs;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;

public sealed record SuspendAccountCommand(
    Guid UserId,
    string Reason) : IRequest<Result<AccountRestrictionDto>>;

public sealed class SuspendAccountCommandHandler(
    IntegrityDbContext dbContext)
    : IRequestHandler<SuspendAccountCommand, Result<AccountRestrictionDto>>
{
    public async Task<Result<AccountRestrictionDto>> Handle(
        SuspendAccountCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restriction = AccountRestriction.Apply(
            request.UserId, RestrictionLevel.Suspended, request.Reason);

        dbContext.AccountRestrictions.Add(restriction);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AccountRestrictionDto>.Success(
            new AccountRestrictionDto(
                restriction.Id, restriction.UserId, restriction.RestrictionLevel,
                restriction.AppliedAt, restriction.Reason));
    }
}
