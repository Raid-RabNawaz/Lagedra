using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using FluentValidation;
using Lagedra.Auth;
using Lagedra.Auth.Infrastructure.Seed;
using Lagedra.Auth.Presentation.Endpoints;
using Lagedra.Compliance;
using Lagedra.Compliance.Presentation.Endpoints;
using Lagedra.Infrastructure;
using Lagedra.TruthSurface;
using Lagedra.TruthSurface.Presentation.Endpoints;
using Lagedra.Modules.Notifications;
using Lagedra.Modules.Notifications.Presentation.Endpoints;
using Lagedra.Modules.Privacy;
using Lagedra.Modules.Privacy.Presentation.Endpoints;
using Lagedra.Modules.ListingAndLocation;
using Lagedra.Modules.ListingAndLocation.Presentation.Endpoints;
using Lagedra.Modules.StructuredInquiry;
using Lagedra.Modules.StructuredInquiry.Presentation.Endpoints;
using Lagedra.Modules.Arbitration;
using Lagedra.Modules.Arbitration.Presentation.Endpoints;
using Lagedra.Modules.Evidence;
using Lagedra.Modules.Evidence.Presentation.Endpoints;
using Lagedra.Modules.LeaseAgreements;
using Lagedra.Modules.LeaseAgreements.Presentation.Endpoints;
using Lagedra.Modules.ActivationAndBilling;
using Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;
using Lagedra.Modules.IdentityAndVerification;
using Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;
using Lagedra.Modules.InsuranceIntegration;
using Lagedra.Modules.InsuranceIntegration.Presentation.Endpoints;
using Lagedra.Modules.VerificationAndRisk;
using Lagedra.Modules.VerificationAndRisk.Presentation.Endpoints;
using Lagedra.Modules.ComplianceMonitoring;
using Lagedra.Modules.ComplianceMonitoring.Presentation.Endpoints;
using Lagedra.Modules.AntiAbuseAndIntegrity;
using Lagedra.Modules.AntiAbuseAndIntegrity.Presentation.Endpoints;
using Lagedra.Modules.ContentManagement;
using Lagedra.Modules.ContentManagement.Presentation.Endpoints;
using Lagedra.Modules.PartnerNetwork;
using Lagedra.Modules.PartnerNetwork.Presentation.Endpoints;
using Lagedra.Modules.ChannelIntegration;
using Lagedra.Modules.ChannelIntegration.Presentation.Endpoints;
using Lagedra.Modules.Reviews;
using Lagedra.Modules.Reviews.Presentation.Endpoints;
using Lagedra.Modules.AuditLog;
using Lagedra.Modules.AuditLog.Presentation.Endpoints;
using Lagedra.Modules.Analytics;
using Lagedra.Modules.Analytics.Presentation.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Lagedra.Infrastructure.Middleware;
using Lagedra.Infrastructure.Observability;
using Lagedra.Infrastructure.RealTime;
using Lagedra.Infrastructure.Settings;
using Lagedra.SharedKernel.Settings;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, loggerConfig) =>
        loggerConfig
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName());

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddAuth(builder.Configuration);
    builder.Services.AddTruthSurface(builder.Configuration);
    builder.Services.AddCompliance(builder.Configuration);
    builder.Services.AddActivationAndBilling(builder.Configuration);
    builder.Services.AddListingAndLocation(builder.Configuration);
    builder.Services.AddIdentityVerification(builder.Configuration);
    builder.Services.AddInsuranceIntegration(builder.Configuration);
    builder.Services.AddStructuredInquiry(builder.Configuration);
    builder.Services.AddVerificationAndRisk(builder.Configuration);
    builder.Services.AddComplianceMonitoring(builder.Configuration);
    builder.Services.AddArbitration(builder.Configuration);
    builder.Services.AddEvidence(builder.Configuration);
    builder.Services.AddLeaseAgreements(builder.Configuration);
    builder.Services.AddNotifications(builder.Configuration);
    builder.Services.AddPrivacy(builder.Configuration);
    builder.Services.AddAntiAbuseAndIntegrity(builder.Configuration);
    builder.Services.AddContentManagement(builder.Configuration);
    builder.Services.AddPartnerNetwork(builder.Configuration);
    builder.Services.AddChannelIntegration(builder.Configuration);
    builder.Services.AddReviews(builder.Configuration);
    builder.Services.AddAuditLog(builder.Configuration);
    builder.Services.AddAnalytics(builder.Configuration);

    builder.Services.AddValidatorsFromAssemblies(
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("Lagedra", StringComparison.OrdinalIgnoreCase) == true));

    builder.Services.AddLagedraRateLimiting();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lagedra API", Version = "v1" });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddAuthorizationBuilder()
        // "Landlord" / "Tenant" are accepted transitionally so that JWTs issued before the role merge
        // continue to authorize until their owners refresh / re-login. Remove after one release.
        .AddPolicy("RequireMember", p => p.RequireRole("Member", "PlatformAdmin", "Landlord", "Tenant"))
        .AddPolicy("RequireArbitrator", p => p.RequireRole("Arbitrator", "PlatformAdmin"))
        .AddPolicy("RequirePlatformAdmin", p => p.RequireRole("PlatformAdmin"))
        .AddPolicy("RequirePackApprover", p => p.RequireRole("PlatformAdmin", "Arbitrator"))
        .AddPolicy("RequireInsurancePartner", p => p.RequireRole("InsurancePartner", "PlatformAdmin"))
        .AddPolicy("RequireInstitutionPartner", p => p.RequireRole("InstitutionPartner", "PlatformAdmin"));

    builder.Services.AddCors(options =>
        options.AddPolicy("Frontend", policy => policy
            .WithOrigins(
                builder.Configuration["App:FrontendUrl"] ?? "http://localhost:3000",
                builder.Configuration["App:AdminUrl"] ?? "http://localhost:3001",
                "http://localhost:5173",
                builder.Configuration["App:MarketingUrl"] ?? "http://localhost:3002")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    var app = builder.Build();

    await using (var migrationScope = app.Services.CreateAsyncScope())
    {
        var sp = migrationScope.ServiceProvider;
        var dbContextTypes = new[]
        {
            typeof(Lagedra.Auth.Infrastructure.Persistence.AuthDbContext),
            typeof(Lagedra.Compliance.Infrastructure.Persistence.ComplianceDbContext),
            typeof(Lagedra.TruthSurface.Infrastructure.Persistence.TruthSurfaceDbContext),
            typeof(Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence.ListingsDbContext),
            typeof(Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence.BillingDbContext),
            typeof(Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence.IdentityDbContext),
            typeof(Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence.InsuranceDbContext),
            typeof(Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence.InquiryDbContext),
            typeof(Lagedra.Modules.Arbitration.Infrastructure.Persistence.ArbitrationDbContext),
            typeof(Lagedra.Modules.Evidence.Infrastructure.Persistence.EvidenceDbContext),
            typeof(Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence.LeaseAgreementDbContext),
            typeof(Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence.RiskDbContext),
            typeof(Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence.ComplianceMonitoringDbContext),
            typeof(Lagedra.Modules.Notifications.Infrastructure.Persistence.NotificationDbContext),
            typeof(Lagedra.Modules.Privacy.Infrastructure.Persistence.PrivacyDbContext),
            typeof(Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence.IntegrityDbContext),
            typeof(Lagedra.Modules.ContentManagement.Infrastructure.Persistence.ContentDbContext),
            typeof(Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence.PartnerDbContext),
            typeof(Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence.ChannelDbContext),
            typeof(Lagedra.Modules.AuditLog.Infrastructure.Persistence.AuditDbContext),
            typeof(Lagedra.Modules.Reviews.Infrastructure.Persistence.ReviewsDbContext),
            typeof(Lagedra.Infrastructure.Settings.PlatformSettingsDbContext),
        };

        foreach (var ctxType in dbContextTypes)
        {
            if (sp.GetService(ctxType) is Microsoft.EntityFrameworkCore.DbContext ctx)
            {
                try
                {
                    await ctx.Database.MigrateAsync().ConfigureAwait(false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChanges", StringComparison.Ordinal))
                {
                    Log.Warning("Pending model changes detected for {Context} – existing migrations applied, skipping.", ctxType.Name);
                }
            }
        }
    }

    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDataSeeder>();
    await seeder.SeedAsync().ConfigureAwait(false);

    await Lagedra.Modules.ListingAndLocation.Infrastructure.Seeding.ListingDefinitionsSeeder
        .SeedAsync(app.Services).ConfigureAwait(false);

    // Phase 16.10 — ensure the application_submitted email template (and
    // the rest of the baseline set) exists so the one-tap approve link
    // actually renders. No-op when admin-managed rows already exist.
    // Duplicate TemplateId races (Email vs Sms before the composite unique
    // index migration) must not take down the API.
    try
    {
        await Lagedra.Modules.Notifications.Infrastructure.Seeding.NotificationTemplateSeeder
            .SeedAsync(app.Services).ConfigureAwait(false);
    }
    catch (DbUpdateException ex)
    {
        Log.Warning(ex, "Notification template seed skipped (DB error): {Message}", ex.Message);
    }

    try
    {
        await using var leaseSeedScope = app.Services.CreateAsyncScope();
        var leaseMediator = leaseSeedScope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        await leaseMediator.Send(
            new Lagedra.Modules.LeaseAgreements.Application.Commands.SeedCaliforniaLeaseTemplateCommand())
            .ConfigureAwait(false);
    }
    // Logged at Error, not Warning: a skipped seed means every lease generated
    // from here on uses whatever template was already published, which is a
    // legal-document defect rather than a cosmetic one.
    catch (DbUpdateException ex)
    {
        Log.Error(ex, "Lease agreement template seed FAILED (DB error) — leases will keep using the "
            + "previously published template: {Message}", ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        Log.Error(ex, "Lease agreement template seed FAILED (invalid state) — leases will keep using the "
            + "previously published template: {Message}", ex.Message);
    }

    await using var settingsScope = app.Services.CreateAsyncScope();
    var settingsDb = settingsScope.ServiceProvider.GetRequiredService<PlatformSettingsDbContext>();
    try
    {
        await settingsDb.Database.MigrateAsync().ConfigureAwait(false);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChanges", StringComparison.Ordinal))
    {
        Log.Warning("Pending model changes detected for PlatformSettingsDbContext – existing migrations applied, skipping.");
    }

    // Ensure the pre-launch toggle exists so it surfaces in Admin → Fees &
    // Settings as an on/off switch. Idempotent: only creates the row when
    // missing, so an admin flipping it on/off is never clobbered on deploy.
    try
    {
        var platformSettings = settingsScope.ServiceProvider.GetRequiredService<IPlatformSettingsService>();
        var existing = await platformSettings
            .GetStringAsync(PlatformSettingKeys.PreLaunchEnabled)
            .ConfigureAwait(false);
        if (existing is null)
        {
            await platformSettings.SetAsync(
                PlatformSettingKeys.PreLaunchEnabled,
                "false",
                "Pre-launch mode: turns self sign-up into a founding-partner waitlist and hides the product from non-staff users.",
                updatedByUserId: null)
                .ConfigureAwait(false);
        }
    }
    catch (DbUpdateException ex)
    {
        Log.Warning(ex, "Pre-launch setting seed skipped (DB error): {Message}", ex.Message);
    }

    app.UseCorrelationId();
    app.UseGlobalExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lagedra API v1"));
    }

    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAuthEnforcement();
    app.UseConsentEnforcement();
    app.UseLagedraRateLimiting();
    app.UseIdempotency();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
       .AllowAnonymous()
       .WithTags("Health");

    app.MapHealthChecks("/health/detail", new HealthCheckOptions
    {
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    tags = e.Value.Tags
                })
            });
            await ctx.Response.WriteAsync(result).ConfigureAwait(false);
        }
    }).AllowAnonymous();

    app.MapAuthEndpoints();
    app.MapTruthSurfaceEndpoints();
    app.MapApplicationEndpoints();
    app.MapActionEndpoints();
    app.MapActivationEndpoints();
    app.MapDealEndpoints();
    app.MapCheckoutEndpoints();
    app.MapBillingEndpoints();
    app.MapPaymentConfirmationEndpoints();
    app.MapDamageClaimEndpoints();
    app.MapStripeWebhookEndpoints();
    app.MapListingEndpoints();
    app.MapListingImportEndpoints();
    app.MapListingDefinitionsEndpoints();
    app.MapAdminListingDefinitionsEndpoints();
    app.MapAdminListingReviewEndpoints();
    app.MapLocationEndpoints();
    app.MapIdentityEndpoints();
    app.MapHostPaymentEndpoints();
    app.MapHostStripeEndpoints();
    app.MapVerificationEndpoints();
    app.MapKycWebhookEndpoints();
    app.MapInsuranceEndpoints();
    app.MapInsuranceWebhookEndpoints();
    app.MapInquiryEndpoints();
    app.MapRiskEndpoints();
    app.MapComplianceEndpoints();
    app.MapComplianceMonitoringEndpoints();
    app.MapArbitrationEndpoints();
    app.MapArbitratorEndpoints();
    app.MapEvidenceEndpoints();
    app.MapUploadEndpoints();
    app.MapLeaseAgreementEndpoints();
    app.MapNotificationEndpoints();
    app.MapSmsConsentEndpoints();
    app.MapInAppNotificationEndpoints();
    app.MapPrivacyEndpoints();
    app.MapIntegrityEndpoints();
    app.MapBlogEndpoints();
    app.MapSeoPageEndpoints();
    app.MapAdminBlogEndpoints();
    app.MapPartnerEndpoints();
    app.MapChannelEndpoints();
    app.MapHostawayWebhookEndpoints();
    app.MapOwnerRezWebhookEndpoints();
    app.MapTwilioWebhookEndpoints();
    app.MapReviewsEndpoints();
    app.MapPlatformSettingsEndpoints();
    app.MapPublicConfigEndpoints();
    app.MapAdminComplianceEndpoints();
    app.MapAdminInsuranceEndpoints();
    app.MapAdminIntegrityEndpoints();
    app.MapAdminArbitrationEndpoints();
    app.MapAdminEvidenceEndpoints();
    app.MapAdminIdentityEndpoints();
    app.MapAdminAnalyticsEndpoints();
    app.MapAuditEndpoints();

    app.MapHub<NotificationHub>("/hubs/notifications");

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
