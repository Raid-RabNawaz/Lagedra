using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.Modules.LeaseAgreements.Application.DTOs;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Application.Queries;

public sealed record ListLeaseTemplatesQuery : IRequest<Result<IReadOnlyList<LeaseTemplateSummaryDto>>>;
public sealed record ListLeaseTemplateVersionsQuery(Guid TemplateId) : IRequest<Result<IReadOnlyList<LeaseTemplateVersionSummaryDto>>>;
public sealed record GetLeaseTemplateVersionDetailsQuery(Guid TemplateId, Guid VersionId) : IRequest<Result<LeaseTemplateVersionDetailsDto>>;
public sealed record GetActiveLeaseTemplateQuery(string JurisdictionCode) : IRequest<Result<LeaseTemplateVersionDetailsDto>>;
public sealed record ListPendingLeaseApprovalsQuery : IRequest<Result<IReadOnlyList<PendingLeaseApprovalDto>>>;
public sealed record GetLeasePlaceholderCatalogQuery : IRequest<Result<LeasePlaceholderCatalogDto>>;

public sealed class ListLeaseTemplatesQueryHandler(LeaseAgreementDbContext db)
    : IRequestHandler<ListLeaseTemplatesQuery, Result<IReadOnlyList<LeaseTemplateSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<LeaseTemplateSummaryDto>>> Handle(
        ListLeaseTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await db.Templates.AsNoTracking()
            .Include(t => t.Versions)
            .OrderBy(t => t.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = templates
            .OrderBy(t => t.JurisdictionCode.Code, StringComparer.Ordinal)
            .Select(t => new LeaseTemplateSummaryDto(
                t.Id,
                t.JurisdictionCode.Code,
                t.Title,
                t.ActiveVersionId,
                t.Versions.Count))
            .ToList();

        return Result<IReadOnlyList<LeaseTemplateSummaryDto>>.Success(items);
    }
}

public sealed class ListLeaseTemplateVersionsQueryHandler(LeaseAgreementDbContext db)
    : IRequestHandler<ListLeaseTemplateVersionsQuery, Result<IReadOnlyList<LeaseTemplateVersionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<LeaseTemplateVersionSummaryDto>>> Handle(
        ListLeaseTemplateVersionsQuery request, CancellationToken cancellationToken)
    {
        var template = await db.Templates.AsNoTracking()
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            .ConfigureAwait(false);

        if (template is null)
        {
            return Result<IReadOnlyList<LeaseTemplateVersionSummaryDto>>.Failure(
                new Error("LeaseTemplate.NotFound", "Lease template not found."));
        }

        var versions = template.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => LeaseTemplateMapper.ToSummary(template, v))
            .ToList();

        return Result<IReadOnlyList<LeaseTemplateVersionSummaryDto>>.Success(versions);
    }
}

public sealed class GetLeaseTemplateVersionDetailsQueryHandler(LeaseAgreementDbContext db)
    : IRequestHandler<GetLeaseTemplateVersionDetailsQuery, Result<LeaseTemplateVersionDetailsDto>>
{
    public async Task<Result<LeaseTemplateVersionDetailsDto>> Handle(
        GetLeaseTemplateVersionDetailsQuery request, CancellationToken cancellationToken)
    {
        var template = await db.Templates.AsNoTracking()
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

        return Result<LeaseTemplateVersionDetailsDto>.Success(LeaseTemplateMapper.ToDetails(template, version));
    }
}

public sealed class GetActiveLeaseTemplateQueryHandler(LeaseAgreementDbContext db)
    : IRequestHandler<GetActiveLeaseTemplateQuery, Result<LeaseTemplateVersionDetailsDto>>
{
    public async Task<Result<LeaseTemplateVersionDetailsDto>> Handle(
        GetActiveLeaseTemplateQuery request, CancellationToken cancellationToken)
    {
        var code = request.JurisdictionCode.ToUpperInvariant();
        var template = await db.Templates.AsNoTracking()
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.JurisdictionCode.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (template?.ActiveVersionId is null)
        {
            return Result<LeaseTemplateVersionDetailsDto>.Failure(
                new Error("LeaseTemplate.NotPublished", $"No published lease template for '{code}'."));
        }

        var version = template.Versions.FirstOrDefault(v => v.Id == template.ActiveVersionId);
        if (version is null)
        {
            return Result<LeaseTemplateVersionDetailsDto>.Failure(
                new Error("LeaseTemplate.VersionNotFound", "Active version missing."));
        }

        return Result<LeaseTemplateVersionDetailsDto>.Success(LeaseTemplateMapper.ToDetails(template, version));
    }
}

public sealed class ListPendingLeaseApprovalsQueryHandler(LeaseAgreementDbContext db)
    : IRequestHandler<ListPendingLeaseApprovalsQuery, Result<IReadOnlyList<PendingLeaseApprovalDto>>>
{
    public async Task<Result<IReadOnlyList<PendingLeaseApprovalDto>>> Handle(
        ListPendingLeaseApprovalsQuery request, CancellationToken cancellationToken)
    {
        var templates = await db.Templates.AsNoTracking()
            .Include(t => t.Versions)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pending = templates
            .SelectMany(t => t.Versions
                .Where(v => v.Status == LeaseTemplateVersionStatus.PendingApproval)
                .Select(v => new PendingLeaseApprovalDto(
                    t.Id,
                    t.JurisdictionCode.Code,
                    t.Title,
                    v.Id,
                    v.VersionNumber,
                    v.EffectiveDate,
                    v.ApprovedBy)))
            .OrderBy(p => p.JurisdictionCode)
            .ToList();

        return Result<IReadOnlyList<PendingLeaseApprovalDto>>.Success(pending);
    }
}

public sealed class GetLeasePlaceholderCatalogQueryHandler
    : IRequestHandler<GetLeasePlaceholderCatalogQuery, Result<LeasePlaceholderCatalogDto>>
{
    public Task<Result<LeasePlaceholderCatalogDto>> Handle(
        GetLeasePlaceholderCatalogQuery request, CancellationToken cancellationToken)
    {
        var placeholders = LeasePlaceholderCatalog.All
            .Select(p => new LeasePlaceholderDto(
                p.Key,
                p.Group,
                p.Label,
                p.Description,
                p.Example,
                p.Required,
                "{{" + p.Key + "}}"))
            .ToList();

        var dto = new LeasePlaceholderCatalogDto(
            placeholders,
            LeasePlaceholderCatalog.UsageExampleHtml,
            "Type {{ then pick a variable, or click Insert. Example: Hello {{tenant.fullName}}, your rent is {{deal.monthlyRent}}.");

        return Task.FromResult(Result<LeasePlaceholderCatalogDto>.Success(dto));
    }
}
