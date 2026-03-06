using System.Globalization;
using Lagedra.Auth;
using Lagedra.Compliance;
using Lagedra.Infrastructure;
using Lagedra.Modules.ActivationAndBilling;
using Lagedra.Modules.AntiAbuseAndIntegrity;
using Lagedra.Modules.Arbitration;
using Lagedra.Modules.ComplianceMonitoring;
using Lagedra.Modules.ContentManagement;
using Lagedra.Modules.Evidence;
using Lagedra.Modules.IdentityAndVerification;
using Lagedra.Modules.InsuranceIntegration;
using Lagedra.Modules.JurisdictionPacks;
using Lagedra.Modules.ListingAndLocation;
using Lagedra.Modules.Notifications;
using Lagedra.Modules.PartnerNetwork;
using Lagedra.Modules.Privacy;
using Lagedra.Modules.StructuredInquiry;
using Lagedra.Modules.VerificationAndRisk;
using Lagedra.TruthSurface;
using Lagedra.Worker.Scheduling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((ctx, loggerConfig) =>
            loggerConfig
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName())
        .ConfigureServices((hostContext, services) =>
        {
            var configuration = hostContext.Configuration;

            services.AddInfrastructure(configuration);
            services.AddAuth(configuration);
            services.AddTruthSurface(configuration);
            services.AddCompliance(configuration);
            services.AddActivationAndBilling(configuration);
            services.AddListingAndLocation(configuration);
            services.AddIdentityVerification(configuration);
            services.AddInsuranceIntegration(configuration);
            services.AddStructuredInquiry(configuration);
            services.AddVerificationAndRisk(configuration);
            services.AddComplianceMonitoring(configuration);
            services.AddArbitration(configuration);
            services.AddEvidence(configuration);
            services.AddJurisdictionPacks(configuration);
            services.AddNotifications(configuration);
            services.AddPrivacy(configuration);
            services.AddAntiAbuseAndIntegrity(configuration);
            services.AddContentManagement(configuration);
            services.AddPartnerNetwork(configuration);

            services.AddQuartzScheduling(configuration);
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.Configure(app =>
            {
                app.UseHealthChecks("/healthz");
            });
        })
        .Build();

    await host.RunAsync().ConfigureAwait(false);
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Worker terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
