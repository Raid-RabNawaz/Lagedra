using System.Globalization;
using Lagedra.Auth;
using Lagedra.Auth.Infrastructure.Seed;
using Lagedra.Auth.Presentation.Endpoints;
using Lagedra.Infrastructure;
using Microsoft.OpenApi.Models;
using Quartz;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, loggerConfig) =>
        loggerConfig
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext());

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddAuth(builder.Configuration);
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

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

    app.MapAuthEndpoints();

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
