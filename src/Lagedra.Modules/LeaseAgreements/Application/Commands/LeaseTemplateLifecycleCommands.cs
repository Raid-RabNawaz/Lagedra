using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record RequestLeaseTemplateApprovalCommand(Guid TemplateId, Guid VersionId) : IRequest<Result>;
public sealed record ApproveLeaseTemplateVersionCommand(Guid TemplateId, Guid VersionId, Guid ApproverId) : IRequest<Result>;
public sealed record PublishLeaseTemplateVersionCommand(Guid TemplateId, Guid VersionId) : IRequest<Result>;
public sealed record DeprecateLeaseTemplateVersionCommand(Guid TemplateId, Guid VersionId) : IRequest<Result>;
public sealed record AddLeaseTemplateVersionCommand(Guid TemplateId) : IRequest<Result<Guid>>;

internal static class LeaseTemplateLifecycleHelpers
{
    public static async Task<(LeaseAgreementTemplate? Template, LeaseTemplateVersion? Version, Error? Error)>
        LoadAsync(LeaseAgreementDbContext db, Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var template = await db.Templates.Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return (null, null, new Error("LeaseTemplate.NotFound", "Lease template not found."));
        }

        var version = template.Versions.FirstOrDefault(v => v.Id == versionId);
        if (version is null)
        {
            return (null, null, new Error("LeaseTemplate.VersionNotFound", "Template version not found."));
        }

        return (template, version, null);
    }
}

public sealed class RequestLeaseTemplateApprovalCommandHandler(LeaseAgreementDbContext db)
    : IRequestHandler<RequestLeaseTemplateApprovalCommand, Result>
{
    public async Task<Result> Handle(RequestLeaseTemplateApprovalCommand request, CancellationToken cancellationToken)
    {
        var (template, version, error) = await LeaseTemplateLifecycleHelpers
            .LoadAsync(db, request.TemplateId, request.VersionId, cancellationToken).ConfigureAwait(false);
        if (error is not null) return Result.Failure(error);
        try
        {
            version!.RequestApproval();
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("LeaseTemplate.InvalidState", ex.Message));
        }
    }
}

public sealed class ApproveLeaseTemplateVersionCommandHandler(LeaseAgreementDbContext db)
    : IRequestHandler<ApproveLeaseTemplateVersionCommand, Result>
{
    public async Task<Result> Handle(ApproveLeaseTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var (_, version, error) = await LeaseTemplateLifecycleHelpers
            .LoadAsync(db, request.TemplateId, request.VersionId, cancellationToken).ConfigureAwait(false);
        if (error is not null) return Result.Failure(error);
        try
        {
            version!.Approve(request.ApproverId);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("LeaseTemplate.InvalidState", ex.Message));
        }
    }
}

public sealed class PublishLeaseTemplateVersionCommandHandler(LeaseAgreementDbContext db)
    : IRequestHandler<PublishLeaseTemplateVersionCommand, Result>
{
    public async Task<Result> Handle(PublishLeaseTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var (template, version, error) = await LeaseTemplateLifecycleHelpers
            .LoadAsync(db, request.TemplateId, request.VersionId, cancellationToken).ConfigureAwait(false);
        if (error is not null) return Result.Failure(error);
        try
        {
            if (version!.Status != LeaseTemplateVersionStatus.Active && !version.HasDualApproval)
            {
                return Result.Failure(new Error("LeaseTemplate.NotApproved", "Version is not dual-approved."));
            }

            template!.Publish(request.VersionId);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("LeaseTemplate.InvalidState", ex.Message));
        }
    }
}

public sealed class DeprecateLeaseTemplateVersionCommandHandler(LeaseAgreementDbContext db)
    : IRequestHandler<DeprecateLeaseTemplateVersionCommand, Result>
{
    public async Task<Result> Handle(DeprecateLeaseTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var (template, _, error) = await LeaseTemplateLifecycleHelpers
            .LoadAsync(db, request.TemplateId, request.VersionId, cancellationToken).ConfigureAwait(false);
        if (error is not null) return Result.Failure(error);
        try
        {
            template!.DeprecateVersion(request.VersionId);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("LeaseTemplate.InvalidState", ex.Message));
        }
    }
}

public sealed class AddLeaseTemplateVersionCommandHandler(LeaseAgreementDbContext db)
    : IRequestHandler<AddLeaseTemplateVersionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddLeaseTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var template = await db.Templates.Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Result<Guid>.Failure(new Error("LeaseTemplate.NotFound", "Lease template not found."));
        }

        var previous = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var version = template.AddVersion(previous?.BodyHtml);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Success(version.Id);
    }
}
