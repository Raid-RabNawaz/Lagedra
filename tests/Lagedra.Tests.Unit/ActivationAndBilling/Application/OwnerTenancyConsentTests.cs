using System;
using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.SharedKernel.Integration;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Application;

public class OwnerTenancyConsentTests
{
    [Fact]
    public void IsRequired_only_for_property_manager_with_named_owner()
    {
        var ownerId = Guid.NewGuid();
        OwnerTenancyConsent.IsRequired(Listing("PropertyManager", ownerId)).Should().BeTrue();
        OwnerTenancyConsent.IsRequired(Listing("Owner", ownerId)).Should().BeFalse();
        OwnerTenancyConsent.IsRequired(Listing("PropertyManager", null)).Should().BeFalse();
        OwnerTenancyConsent.IsRequired(Listing("PropertyManager", Guid.Empty)).Should().BeFalse();
    }

    [Fact]
    public void ApplyIfRequired_snapshots_owner_on_the_application()
    {
        var ownerId = Guid.NewGuid();
        var app = DealApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1));

        OwnerTenancyConsent.ApplyIfRequired(app, Listing("PropertyManager", ownerId));

        app.OwnerConsentRequired.Should().BeTrue();
        app.HomeOwnerUserId.Should().Be(ownerId);
    }

    [Fact]
    public void ApplyIfRequired_is_a_no_op_for_owner_listed_homes()
    {
        var app = DealApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1));

        OwnerTenancyConsent.ApplyIfRequired(app, Listing("Owner", Guid.NewGuid()));

        app.OwnerConsentRequired.Should().BeFalse();
        app.HomeOwnerUserId.Should().BeNull();
    }

    private static ListingDetailsDto Listing(string managerRole, Guid? homeOwnerUserId) =>
        new(
            Id: Guid.NewGuid(),
            LandlordUserId: Guid.NewGuid(),
            MinStayDays: 30,
            MaxStayDays: 180,
            MaxDepositCents: 300_000,
            MonthlyRentCents: 300_000,
            JurisdictionCode: "US-CA",
            Title: "Test home",
            ManagerRole: managerRole,
            HomeOwnerUserId: homeOwnerUserId);
}
