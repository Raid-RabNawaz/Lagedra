using System.Collections.Generic;
using FluentAssertions;
using Lagedra.Modules.Notifications;
using Lagedra.Modules.Notifications.Infrastructure.Dispatch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Lagedra.Tests.Unit.Notifications.Infrastructure;

public class NotificationsModuleRegistrationTests
{
    [Fact]
    public void In_process_dispatcher_is_off_by_default()
    {
        var services = new ServiceCollection();

        services.AddNotifications(BuildConfig(processInProcess: null));

        services.Should().NotContain(d =>
            d.ImplementationType == typeof(NotificationDispatchHostedService));
    }

    [Fact]
    public void In_process_dispatcher_registers_only_when_flagged()
    {
        var services = new ServiceCollection();

        services.AddNotifications(BuildConfig(processInProcess: "true"));

        services.Should().Contain(d =>
            d.ImplementationType == typeof(NotificationDispatchHostedService)
            && d.Lifetime == ServiceLifetime.Singleton
            && d.ServiceType == typeof(IHostedService));
    }

    private static IConfiguration BuildConfig(string? processInProcess)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] =
                "Host=localhost;Port=5432;Database=lagedra_test;Username=lagedra;Password=lagedra",
        };

        if (processInProcess is not null)
        {
            values["Notifications:ProcessInProcess"] = processInProcess;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
