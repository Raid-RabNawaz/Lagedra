using System.Collections.Generic;
using FluentAssertions;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Application;

public sealed class LeaseTemplateConditionalsTests
{
    [Fact]
    public void If_keeps_block_when_value_is_present()
    {
        var html = "{{#if broker.name}}Broker: {{broker.name}}{{/if}}";
        var values = new Dictionary<string, string> { ["broker.name"] = "Gerardo Gutierrez" };

        LeaseTemplateConditionals.Apply(html, values).Should().Be("Broker: {{broker.name}}");
    }

    [Fact]
    public void If_drops_block_when_value_is_empty()
    {
        var html = "Start{{#if broker.name}}Broker{{/if}}End";
        var values = new Dictionary<string, string> { ["broker.name"] = "" };

        LeaseTemplateConditionals.Apply(html, values).Should().Be("StartEnd");
    }

    [Fact]
    public void Unless_keeps_block_for_No()
    {
        var html = "{{#unless listing.smokingAllowed}}smoke-free{{/unless}}";
        var values = new Dictionary<string, string> { ["listing.smokingAllowed"] = "No" };

        LeaseTemplateConditionals.Apply(html, values).Should().Be("smoke-free");
    }

    [Fact]
    public void Unless_drops_block_for_Yes()
    {
        var html = "{{#unless listing.isFurnished}}unfurnished{{/unless}}";
        var values = new Dictionary<string, string> { ["listing.isFurnished"] = "Yes" };

        LeaseTemplateConditionals.Apply(html, values).Should().BeEmpty();
    }

    [Fact]
    public void Owner_consent_block_is_omitted_when_owner_is_absent()
    {
        var html = "{{#if owner.fullName}}OWNER CONSENT{{/if}}{{#unless owner.fullName}}Landlord only{{/unless}}";
        var values = new Dictionary<string, string> { ["owner.fullName"] = "" };

        LeaseTemplateConditionals.Apply(html, values).Should().Be("Landlord only");
    }
}
