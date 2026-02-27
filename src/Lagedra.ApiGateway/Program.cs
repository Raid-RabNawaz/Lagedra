using System.Globalization;
using Lagedra.Auth;
using Lagedra.Auth.Infrastructure.Seed;
using Lagedra.Auth.Presentation.Endpoints;
using Lagedra.Compliance;
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
using Lagedra.Modules.JurisdictionPacks;
using Lagedra.Modules.JurisdictionPacks.Presentation.Endpoints;
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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Lagedra.Infrastructure.Observability;
using Lagedra.Infrastructure.RealTime;
using Lagedra.Infrastructure.Settings;
using Quartz;
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
    builder.Services.AddJurisdictionPacks(builder.Configuration);
    builder.Services.AddNotifications(builder.Configuration);
    builder.Services.AddPrivacy(builder.Configuration);
    builder.Services.AddAntiAbuseAndIntegrity(builder.Configuration);
    builder.Services.AddContentManagement(builder.Configuration);
    builder.Services.AddPartnerNetwork(builder.Configuration);
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

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
        .AddPolicy("RequireLandlord", p => p.RequireRole("Landlord", "PlatformAdmin"))
        .AddPolicy("RequireTenant", p => p.RequireRole("Tenant", "PlatformAdmin"))
        .AddPolicy("RequireArbitrator", p => p.RequireRole("Arbitrator", "PlatformAdmin"))
        .AddPolicy("RequirePlatformAdmin", p => p.RequireRole("PlatformAdmin"))
        .AddPolicy("RequireInsurancePartner", p => p.RequireRole("InsurancePartner", "PlatformAdmin"))
        .AddPolicy("RequireInstitutionPartner", p => p.RequireRole("InstitutionPartner", "PlatformAdmin"));

    builder.Services.AddCors(options =>
        options.AddPolicy("Frontend", policy => policy
            .WithOrigins(
                builder.Configuration["App:FrontendUrl"] ?? "http://localhost:3000",
                builder.Configuration["App:AdminUrl"] ?? "http://localhost:3001",
                builder.Configuration["App:MarketingUrl"] ?? "http://localhost:3002")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    var app = builder.Build();

    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDataSeeder>();
    await seeder.SeedAsync().ConfigureAwait(false);

    await Lagedra.Modules.ListingAndLocation.Infrastructure.Seeding.ListingDefinitionsSeeder
        .SeedAsync(app.Services).ConfigureAwait(false);

    await using var settingsScope = app.Services.CreateAsyncScope();
    var settingsDb = settingsScope.ServiceProvider.GetRequiredService<PlatformSettingsDbContext>();
    await settingsDb.Database.EnsureCreatedAsync().ConfigureAwait(false);

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
    app.MapActivationEndpoints();
    app.MapBillingEndpoints();
    app.MapPaymentConfirmationEndpoints();
    app.MapListingEndpoints();
    app.MapListingDefinitionsEndpoints();
    app.MapAdminListingDefinitionsEndpoints();
    app.MapLocationEndpoints();
    app.MapIdentityEndpoints();
    app.MapHostPaymentEndpoints();
    app.MapVerificationEndpoints();
    app.MapInsuranceEndpoints();
    app.MapInquiryEndpoints();
    app.MapRiskEndpoints();
    app.MapComplianceMonitoringEndpoints();
    app.MapArbitrationEndpoints();
    app.MapArbitratorEndpoints();
    app.MapEvidenceEndpoints();
    app.MapUploadEndpoints();
    app.MapJurisdictionPackEndpoints();
    app.MapNotificationEndpoints();
    app.MapInAppNotificationEndpoints();
    app.MapPrivacyEndpoints();
    app.MapIntegrityEndpoints();
    app.MapBlogEndpoints();
    app.MapSeoPageEndpoints();
    app.MapAdminBlogEndpoints();
    app.MapPartnerEndpoints();
    app.MapPlatformSettingsEndpoints();

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
