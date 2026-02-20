using Lagedra.Infrastructure.Eventing;
using Lagedra.Infrastructure.External.Email;
using Lagedra.Infrastructure.Security;
using Lagedra.Infrastructure.Time;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IHashingService, HashingService>();
        services.AddSingleton<ICryptographicSigner, CryptographicSigner>();

        services.Configure<BrevoSmtpSettings>(
            configuration.GetSection(BrevoSmtpSettings.SectionName));
        services.AddScoped<IEmailService, MailKitEmailService>();

        services.AddScoped<IEventBus, InMemoryEventBus>();

        return services;
    }
}
