using FluentValidation;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.JurisdictionPacks.Application.Commands;

public sealed record PublishPackVersionCommand(
    Guid PackId,
    Guid VersionId) : IRequest<Result>;

public sealed class PublishPackVersionCommandValidator : AbstractValidator<PublishPackVersionCommand>
{
    public PublishPackVersionCommandValidator()
    {
        RuleFor(x => x.PackId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
    }
}

public sealed class PublishPackVersionCommandHandler(JurisdictionPackRepository repository)
    : IRequestHandler<PublishPackVersionCommand, Result>
{
    public async Task<Result> Handle(PublishPackVersionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pack = await repository.GetByIdAsync(request.PackId, cancellationToken).ConfigureAwait(false);

        if (pack is null)
        {
            return Result.Failure(new Error("JurisdictionPack.NotFound", $"Pack '{request.PackId}' not found."));
        }

        pack.Publish(request.VersionId);

        repository.Update(pack);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
