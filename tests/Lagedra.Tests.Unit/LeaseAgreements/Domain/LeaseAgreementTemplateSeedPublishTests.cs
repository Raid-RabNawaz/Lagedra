using System;
using System.Linq;
using FluentAssertions;
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
            "<h1>Lease</h1><p>Landlord <strong>Jane Host</strong> leases to Tenant.</p>");

        pdf.Should().NotBeNullOrEmpty();
        // PDF magic number
        pdf[0].Should().Be(0x25); // %
        pdf[1].Should().Be(0x50); // P
        pdf[2].Should().Be(0x44); // D
        pdf[3].Should().Be(0x46); // F
    }
}
