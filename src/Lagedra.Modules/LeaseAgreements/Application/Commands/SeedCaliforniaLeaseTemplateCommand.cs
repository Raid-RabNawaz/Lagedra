using Lagedra.Modules.LeaseAgreements.Application.Templates;
using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record SeedCaliforniaLeaseTemplateCommand : IRequest<Result>;

/// <summary>
/// Idempotent seed: creates (or upgrades) a published US-CA lease template so
/// Truth Surface confirmation can generate lease PDFs without a manual
/// dual-approve + publish ceremony for the first jurisdiction pack.
/// Subsequent edits still go through the normal draft → dual-control → publish flow.
/// </summary>
public sealed class SeedCaliforniaLeaseTemplateCommandHandler(LeaseAgreementDbContext db)
    : IRequestHandler<SeedCaliforniaLeaseTemplateCommand, Result>
{
    public async Task<Result> Handle(SeedCaliforniaLeaseTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await db.Templates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.JurisdictionCode.Code == "US-CA", cancellationToken)
            .ConfigureAwait(false);

        var body = CaliforniaLeaseTemplateHtml.Body;
        var title = CaliforniaLeaseTemplateHtml.Title;

        if (template is null)
        {
            template = LeaseAgreementTemplate.CreateDraft("US-CA", title);
            var version = template.AddVersion(body);
            version.SetEffectiveDate(DateTime.UtcNow.Date);
            template.PublishSeedVersion(version.Id);
            db.Templates.Add(template);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }

        if (!string.Equals(template.Title, title, StringComparison.Ordinal))
        {
            template.Rename(title);
        }

        var active = template.ActiveVersionId is { } activeId
            ? template.Versions.FirstOrDefault(v => v.Id == activeId)
            : null;

        if (active is not null && BodiesMatch(active.BodyHtml, body))
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }

        // Existing env with a short/stale published body, or nothing live yet:
        // publish a new seed version so generated PDFs pick up the full
        // California lease without a manual dual-approve ceremony.
        var next = template.AddVersion(body);
        next.SetEffectiveDate(DateTime.UtcNow.Date);
        template.PublishSeedVersion(next.Id);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static bool BodiesMatch(string current, string expected) =>
        string.Equals(Normalize(current), Normalize(expected), StringComparison.Ordinal);

    private static string Normalize(string html) =>
        html.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
