using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record CreateFraudFlagCommand(
    Guid UserId,
    string Reason,
    string Source) : IRequest<Result<FraudFlagDto>>;

public sealed class CreateFraudFlagCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<CreateFraudFlagCommand, Result<FraudFlagDto>>
{
    private static readonly TimeSpan DefaultSlaDuration = TimeSpan.FromHours(24);

    public async Task<Result<FraudFlagDto>> Handle(
        CreateFraudFlagCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flag = FraudFlag.Create(request.UserId, request.Reason, request.Source, DefaultSlaDuration);

        dbContext.FraudFlags.Add(flag);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<FraudFlagDto>.Success(MapToDto(flag));
    }

    private static FraudFlagDto MapToDto(FraudFlag f) =>
        new(f.Id, f.UserId, f.Reason, f.Source,
            f.RaisedAt, f.SlaDeadline, f.ResolvedAt, f.IsEscalated);
}
