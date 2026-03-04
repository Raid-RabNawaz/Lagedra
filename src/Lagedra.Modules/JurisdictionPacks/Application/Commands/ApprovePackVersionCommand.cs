using FluentValidation;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.JurisdictionPacks.Application.Commands;

public sealed record ApprovePackVersionCommand(
    Guid PackId,
    Guid VersionId,
    Guid ApproverId) : IRequest<Result>;

public sealed class ApprovePackVersionCommandValidator : AbstractValidator<ApprovePackVersionCommand>
{
    public ApprovePackVersionCommandValidator()
    {
        RuleFor(x => x.PackId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.ApproverId).NotEmpty();
    }
}

public sealed class ApprovePackVersionCommandHandler(JurisdictionPackRepository repository)
    : IRequestHandler<ApprovePackVersionCommand, Result>
{
    public async Task<Result> Handle(ApprovePackVersionCommand request, CancellationToken cancellationToken)
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

        version.Approve(request.ApproverId);

        repository.Update(pack);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
