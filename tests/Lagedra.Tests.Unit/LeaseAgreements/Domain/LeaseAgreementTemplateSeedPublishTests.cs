using System;
using System.Linq;
using FluentAssertions;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Application.Templates;
using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Domain.Events;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Domain;

public class LeaseAgreementTemplateSeedPublishTests
{
    [Fact]
    public void PublishSeedVersion_promotes_draft_to_live_active_version()
    {
        var template = LeaseAgreementTemplate.CreateDraft("US-CA", "California Residential Lease");
        var version = template.AddVersion("<p>Lease for {{host.fullName}}</p>");
        version.SetEffectiveDate(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        template.PublishSeedVersion(version.Id);

        version.Status.Should().Be(LeaseTemplateVersionStatus.Active);
        version.HasDualApproval.Should().BeTrue();
        template.ActiveVersionId.Should().Be(version.Id);
        template.DomainEvents.OfType<LeaseAgreementTemplatePublishedEvent>()
            .Should().ContainSingle(e =>
                e.TemplateId == template.Id
                && e.VersionId == version.Id
                && e.JurisdictionCode == "US-CA");
    }

    [Fact]
    public void PublishSeedVersion_is_idempotent_when_already_live()
    {
        var template = LeaseAgreementTemplate.CreateDraft("US-CA", "California Residential Lease");
        var version = template.AddVersion("<p>Body</p>");
        version.SetEffectiveDate(DateTime.UtcNow.Date);

        template.PublishSeedVersion(version.Id);
        template.ClearDomainEvents();
        template.PublishSeedVersion(version.Id);

        template.ActiveVersionId.Should().Be(version.Id);
        version.Status.Should().Be(LeaseTemplateVersionStatus.Active);
        template.DomainEvents.OfType<LeaseAgreementTemplatePublishedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void LeasePdfGenerator_emits_non_empty_pdf_bytes()
    {
        var generator = new LeasePdfGenerator();
        var pdf = generator.Generate(
            "California Lease Agreement",
            "<h1>California Lease Agreement</h1><p>This Lease will <strong>continue</strong> after Utilities are paid. The Tenant is entitled to Residential use.</p>");

        pdf.Should().NotBeNullOrEmpty();
        // PDF magic number
        pdf[0].Should().Be(0x25); // %
        pdf[1].Should().Be(0x50); // P
        pdf[2].Should().Be(0x44); // D
        pdf[3].Should().Be(0x46); // F
    }

    [Fact]
    public void California_seed_body_contains_full_docusign_sections()
    {
        var body = CaliforniaLeaseTemplateHtml.Body;

        body.Should().Contain("California Lease Agreement");
        body.Should().Contain("month-to-month");
        body.Should().Contain("Civil Code § 1950.5");
        body.Should().Contain("Information about Bed Bugs");
        body.Should().Contain("Inspection Checklist");
        body.Should().Contain("class=\"checklist\"");
        body.Should().Contain("Lead-Based Paint");
        body.Should().Contain("Mold Notification Addendum");
        body.Should().Contain("Rent Cap and Just Cause");
        body.Should().Contain("Broker Disclosure");
        body.Should().Contain("OWNER CONSENT AND AUTHORIZATION");
        body.Should().Contain("The Landlord (Owner)");
        body.Should().Contain("The Property Manager / Agent");
        body.Should().Contain("owner.consentDate");
        body.Should().Contain("Megan's Law");
    }
}
