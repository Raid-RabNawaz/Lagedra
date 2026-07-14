using FluentValidation;
using Lagedra.Modules.LeaseAgreements.Application.DTOs;
using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record CreateLeaseTemplateDraftCommand(
    string JurisdictionCode,
    string Title) : IRequest<Result<LeaseAgreementTemplateDto>>;

public sealed class CreateLeaseTemplateDraftCommandValidator : AbstractValidator<CreateLeaseTemplateDraftCommand>
{
    public CreateLeaseTemplateDraftCommandValidator()
    {
        RuleFor(x => x.JurisdictionCode)
            .NotEmpty()
            .Matches(@"^[A-Z]{2}(-[A-Z]{2,10}){1,2}$")
            .WithMessage("Jurisdiction code must match format CC-SS or CC-SS-CCC (e.g. US-CA).");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateLeaseTemplateDraftCommandHandler(LeaseAgreementDbContext dbContext)
    : IRequestHandler<CreateLeaseTemplateDraftCommand, Result<LeaseAgreementTemplateDto>>
{
    public async Task<Result<LeaseAgreementTemplateDto>> Handle(
        CreateLeaseTemplateDraftCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedCode = request.JurisdictionCode.ToUpperInvariant();
        var exists = await dbContext.Templates
            .AnyAsync(t => t.JurisdictionCode.Code == normalizedCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return Result<LeaseAgreementTemplateDto>.Failure(
                new Error("LeaseTemplate.AlreadyExists",
                    $"A lease template already exists for jurisdiction '{request.JurisdictionCode}'."));
        }

        var template = LeaseAgreementTemplate.CreateDraft(request.JurisdictionCode, request.Title);
        template.AddVersion();

        dbContext.Templates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<LeaseAgreementTemplateDto>.Success(LeaseTemplateMapper.ToDto(template));
    }
}
