using FluentValidation;
using Lagedra.Modules.JurisdictionPacks.Application.DTOs;
using Lagedra.Modules.JurisdictionPacks.Domain.Aggregates;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Application.Commands;

public sealed record CreatePackDraftCommand(string JurisdictionCode) : IRequest<Result<JurisdictionPackDto>>;

public sealed class CreatePackDraftCommandValidator : AbstractValidator<CreatePackDraftCommand>
{
    public CreatePackDraftCommandValidator()
    {
        RuleFor(x => x.JurisdictionCode)
            .NotEmpty()
            .Matches(@"^[A-Z]{2}(-[A-Z]{2,10}){1,2}$")
            .WithMessage("Jurisdiction code must match format CC-SS or CC-SS-CCC (e.g. US-CA, US-CA-LA, GB-ENG).");
    }
}

public sealed class CreatePackDraftCommandHandler(JurisdictionDbContext dbContext)
    : IRequestHandler<CreatePackDraftCommand, Result<JurisdictionPackDto>>
{
    public async Task<Result<JurisdictionPackDto>> Handle(CreatePackDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exists = await dbContext.JurisdictionPacks
            .AnyAsync(p => p.JurisdictionCode.Code.Equals(request.JurisdictionCode, StringComparison.OrdinalIgnoreCase), cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return Result<JurisdictionPackDto>.Failure(
                new Error("JurisdictionPack.AlreadyExists", $"A pack already exists for jurisdiction '{request.JurisdictionCode}'."));
        }

        var pack = JurisdictionPack.CreateDraft(request.JurisdictionCode);
        var version = pack.AddVersion();

        dbContext.JurisdictionPacks.Add(pack);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<JurisdictionPackDto>.Success(MapToDto(pack));
    }

    private static JurisdictionPackDto MapToDto(JurisdictionPack pack) =>
        new(pack.Id,
            pack.JurisdictionCode.Code,
            pack.ActiveVersionId,
            pack.Versions.Select(v => new PackVersionSummaryDto(
                v.Id, v.VersionNumber, v.Status,
                v.EffectiveDate, v.ApprovedAt,
                v.ApprovedBy, v.SecondApproverId)).ToList());
}
