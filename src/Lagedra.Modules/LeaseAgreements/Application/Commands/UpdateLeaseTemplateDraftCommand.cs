using System.Text.RegularExpressions;
using FluentValidation;
using Lagedra.Modules.LeaseAgreements.Application.DTOs;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record UpdateLeaseTemplateDraftCommand(
    Guid TemplateId,
    Guid VersionId,
    string BodyHtml,
    DateTime? EffectiveDate,
    string? Title) : IRequest<Result<LeaseTemplateVersionDetailsDto>>;

public sealed class UpdateLeaseTemplateDraftCommandValidator : AbstractValidator<UpdateLeaseTemplateDraftCommand>
{
    public UpdateLeaseTemplateDraftCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.BodyHtml).NotNull();
    }
}

public sealed partial class UpdateLeaseTemplateDraftCommandHandler(LeaseAgreementDbContext dbContext)
    : IRequestHandler<UpdateLeaseTemplateDraftCommand, Result<LeaseTemplateVersionDetailsDto>>
{
    public async Task<Result<LeaseTemplateVersionDetailsDto>> Handle(
        UpdateLeaseTemplateDraftCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await dbContext.Templates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            .ConfigureAwait(false);

        if (template is null)
        {
            return Result<LeaseTemplateVersionDetailsDto>.Failure(
                new Error("LeaseTemplate.NotFound", "Lease template not found."));
        }

        var version = template.Versions.FirstOrDefault(v => v.Id == request.VersionId);
        if (version is null)
        {
            return Result<LeaseTemplateVersionDetailsDto>.Failure(
                new Error("LeaseTemplate.VersionNotFound", "Template version not found."));
        }

        if (version.Status != LeaseTemplateVersionStatus.Draft)
        {
            return Result<LeaseTemplateVersionDetailsDto>.Failure(
                new Error("LeaseTemplate.NotDraft", "Only draft versions can be edited."));
        }

        var unknown = ExtractPlaceholders(request.BodyHtml)
            .Where(k => !LeasePlaceholderCatalog.AllKeys.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknown.Count > 0)
        {
            return Result<LeaseTemplateVersionDetailsDto>.Failure(
                new Error(
                    "LeaseTemplate.UnknownPlaceholders",
                    $"Unknown placeholders: {string.Join(", ", unknown.Select(k => "{{" + k + "}}"))}"));
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            template.Rename(request.Title);
        }

        try
        {
            version.UpdateDraft(request.BodyHtml, request.EffectiveDate);
        }
        catch (InvalidOperationException ex)
        {
            return Result<LeaseTemplateVersionDetailsDto>.Failure(
                new Error("LeaseTemplate.InvalidState", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<LeaseTemplateVersionDetailsDto>.Success(LeaseTemplateMapper.ToDetails(template, version));
    }

    private static IEnumerable<string> ExtractPlaceholders(string bodyHtml) =>
        PlaceholderRegex().Matches(bodyHtml).Select(m => m.Groups[1].Value.Trim());

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_.]+)\s*\}\}")]
    private static partial Regex PlaceholderRegex();
}
