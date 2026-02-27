using FluentValidation;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.JurisdictionPacks.Application.Commands;

public sealed record RequestDualControlApprovalCommand(
    Guid PackId,
    Guid VersionId) : IRequest<Result>;

public sealed class RequestDualControlApprovalCommandValidator : AbstractValidator<RequestDualControlApprovalCommand>
{
    public RequestDualControlApprovalCommandValidator()
    {
        RuleFor(x => x.PackId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
    }
}

public sealed class RequestDualControlApprovalCommandHandler(JurisdictionPackRepository repository)
    : IRequestHandler<RequestDualControlApprovalCommand, Result>
{
    public async Task<Result> Handle(RequestDualControlApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pack = await repository.GetByIdAsync(request.PackId, cancellationToken).ConfigureAwait(false);

        if (pack is null)
        {
            return Result.Failure(new Error("JurisdictionPack.NotFound", $"Pack '{request.PackId}' not found."));
        }

        var version = pack.Versions.FirstOrDefault(v => v.Id == request.VersionId);

        if (version is null)
        {
            return Result.Failure(new Error("PackVersion.NotFound", $"Version '{request.VersionId}' not found."));
        }

        version.RequestApproval();

        repository.Update(pack);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
