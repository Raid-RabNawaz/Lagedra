using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.Time;
using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.Modules.LeaseAgreements.Application.Templates;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lagedra.Tests.Integration.LeaseAgreements;

/// <summary>
/// Guards the startup seed that publishes the jurisdiction lease template.
///
/// This ran silently broken in production for days: the seed threw
/// DbUpdateConcurrencyException ("expected to affect 1 row(s), but actually
/// affected 0 row(s)") on every boot, so live kept serving a superseded
/// template and every generated lease was missing most of its clauses. The
/// failure was swallowed by a catch in Program.cs, and no test could catch it
/// because nothing exercised the seed against a relational provider.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class LeaseTemplateSeedTests(PostgresFixture postgres)
{
    private LeaseAgreementDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<LeaseAgreementDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .Options;

        return new LeaseAgreementDbContext(options, new SystemClock());
    }

    private async Task ResetAsync()
    {
        await using var db = NewContext();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM lease_agreements.deal_lease_documents; " +
            "DELETE FROM lease_agreements.lease_template_versions; " +
            "DELETE FROM lease_agreements.lease_templates; " +
            "DELETE FROM lease_agreements.outbox_messages;");
    }

    private async Task<(Guid TemplateId, Guid VersionId)> GivenStalePublishedTemplateAsync()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        await using var db = NewContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO lease_agreements.lease_templates
                ("Id","jurisdiction_code","Title","ActiveVersionId","CreatedAt","UpdatedAt","IsDeleted")
            VALUES ({0},'US-CA','California Residential Lease Agreement',{1},now(),now(),false);

            INSERT INTO lease_agreements.lease_template_versions
                ("Id","TemplateId","VersionNumber","Status","EffectiveDate","ApprovedAt",
                 "ApprovedBy","SecondApproverId","BodyHtml","CreatedAt","UpdatedAt","IsDeleted")
            VALUES ({1},{0},1,'Active','2026-07-17','2026-07-17',
                 '00000000-0000-0000-0000-0000000000a1','00000000-0000-0000-0000-0000000000a2',
                 '<h1>California Lease Agreement</h1><p>Superseded short body.</p>',
                 now(),now(),false);
            """,
            templateId, versionId);

        return (templateId, versionId);
    }

    private async Task<Result> SeedAsync()
    {
        await using var db = NewContext();
        var handler = new SeedCaliforniaLeaseTemplateCommandHandler(db);
        return await handler.Handle(new SeedCaliforniaLeaseTemplateCommand(), CancellationToken.None);
    }

    [Fact]
    public async Task Seed_publishes_the_full_template_when_no_template_exists()
    {
        await ResetAsync();

        var result = await SeedAsync();

        result.IsSuccess.Should().BeTrue();

        await using var db = NewContext();
        var template = await db.Templates.Include(t => t.Versions).SingleAsync();

        template.Title.Should().Be(CaliforniaLeaseTemplateHtml.Title);
        var live = template.Versions.Single(v => v.Id == template.ActiveVersionId);
        live.Status.Should().Be(LeaseTemplateVersionStatus.Active);
        live.BodyHtml.Should().Be(CaliforniaLeaseTemplateHtml.Body);
    }

    [Fact]
    public async Task Seed_upgrades_a_stale_published_template()
    {
        // The regression this suite exists for. The new version carries a
        // domain-assigned Id, so EF used to treat it as an existing row and
        // emit an UPDATE that matched nothing, rolling back the whole save.
        await ResetAsync();
        var (_, staleVersionId) = await GivenStalePublishedTemplateAsync();

        var result = await SeedAsync();

        result.IsSuccess.Should().BeTrue();

        await using var db = NewContext();
        var template = await db.Templates.Include(t => t.Versions).SingleAsync();

        template.Title.Should().Be(CaliforniaLeaseTemplateHtml.Title);
        template.Versions.Should().HaveCount(2);

        var stale = template.Versions.Single(v => v.Id == staleVersionId);
        stale.Status.Should().Be(LeaseTemplateVersionStatus.Deprecated);

        var live = template.Versions.Single(v => v.Id == template.ActiveVersionId);
        live.Id.Should().NotBe(staleVersionId);
        live.VersionNumber.Should().Be(2);
        live.Status.Should().Be(LeaseTemplateVersionStatus.Active);
        live.HasDualApproval.Should().BeTrue();
        live.BodyHtml.Should().Be(CaliforniaLeaseTemplateHtml.Body);
    }

    [Fact]
    public async Task Seed_does_not_add_a_version_when_the_published_body_already_matches()
    {
        await ResetAsync();
        await GivenStalePublishedTemplateAsync();

        await SeedAsync();
        await SeedAsync();
        await SeedAsync();

        await using var db = NewContext();
        var template = await db.Templates.Include(t => t.Versions).SingleAsync();

        // One upgrade, then no-ops — not a new version on every restart.
        template.Versions.Should().HaveCount(2);
        template.Versions.Count(v => v.Status == LeaseTemplateVersionStatus.Active).Should().Be(1);
    }

    [Fact]
    public async Task AddLeaseTemplateVersion_inserts_a_new_draft_version()
    {
        // Same defect, second call site: this is what the admin UI's
        // "add version" button invokes on an already-persisted template.
        await ResetAsync();
        var (templateId, _) = await GivenStalePublishedTemplateAsync();

        Result<Guid> result;
        await using (var db = NewContext())
        {
            var handler = new AddLeaseTemplateVersionCommandHandler(db);
            result = await handler.Handle(
                new AddLeaseTemplateVersionCommand(templateId), CancellationToken.None);
        }

        result.IsSuccess.Should().BeTrue();

        await using var verify = NewContext();
        var template = await verify.Templates.Include(t => t.Versions).SingleAsync();

        template.Versions.Should().HaveCount(2);
        var added = template.Versions.Single(v => v.Id == result.Value);
        added.Status.Should().Be(LeaseTemplateVersionStatus.Draft);
        added.VersionNumber.Should().Be(2);
    }
}
