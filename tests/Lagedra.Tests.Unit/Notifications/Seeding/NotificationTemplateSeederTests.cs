using System.Linq;
using FluentAssertions;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Seeding;
using Xunit;

namespace Lagedra.Tests.Unit.Notifications.Seeding;

public class NotificationTemplateSeederTests
{
    [Theory]
    [InlineData("application_approved")]
    [InlineData("payment_confirmed")]
    [InlineData("truth_surface_initiated")]
    [InlineData("truth_surface_confirmed")]
    [InlineData("inquiry_offer_accepted")]
    [InlineData("compliance_violation_created")]
    [InlineData("insurance_status_changed")]
    public void Baseline_includes_email_templates_that_were_failing_on_staging(string templateId)
    {
        var emailIds = NotificationTemplateSeeder.EnumerateBaselineTemplates()
            .Where(t => t.Channel == NotificationChannel.Email)
            .Select(t => t.TemplateId)
            .ToHashSet();

        emailIds.Should().Contain(templateId);
    }

    [Fact]
    public void Deal_activated_email_promises_an_attached_lease()
    {
        var template = NotificationTemplateSeeder.EnumerateBaselineTemplates()
            .Single(t => t.TemplateId == "deal_activated" && t.Channel == NotificationChannel.Email);

        template.Subject.Should().Contain("{leaseSubjectSuffix}");
        template.HtmlBody.Should().Contain("{leaseIntroHtml}");
        template.HtmlBody.Should().Contain("{dealUrl}");
    }

    [Fact]
    public void Baseline_does_not_duplicate_email_template_ids()
    {
        var duplicates = NotificationTemplateSeeder.EnumerateBaselineTemplates()
            .Where(t => t.Channel == NotificationChannel.Email)
            .GroupBy(t => t.TemplateId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty();
    }
}
