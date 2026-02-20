# Lagedra: Mid-Term Rental Trust Protocol — Implementation Checklist

> **Legend:** `[x]` = Done &nbsp;|&nbsp; `[ ]` = Pending &nbsp;|&nbsp; `[~]` = In Progress
>
> This file is the single source of truth for implementation progress.
> Update checkboxes as work is completed. Do not alter this document structure.

---

## Confirmed Technology Stack

> All decisions below are locked. Do not substitute libraries without updating this section.

| Concern | Choice | Notes |
|---|---|---|
| Runtime | .NET 10, ASP.NET Core 10 | `net10.0` in `Directory.Build.props`; SDK 10.0.102 |
| Database | PostgreSQL 16 | Schema-per-module, single Docker container |
| ORM | Entity Framework Core 9 + Npgsql 9 | net10.0 runtime; EF Core 9.x + Npgsql 9.x (Npgsql 10.x not stable yet — upgrade together when released) |
| CQRS/Bus | MediatR 12 | In-process only |
| Auth | ASP.NET Identity + JWT (self-hosted) | Custom `ApplicationUser`; refresh tokens in DB |
| Email (library) | MailKit + MimeKit | .NET SMTP client |
| Email (relay) | Brevo (Sendinblue) SMTP | Configured via `SmtpClient` settings in `appsettings` |
| SMS | **None in v1** | Email-only; SMS deferred to v2 |
| Payments | Stripe (NuGet: `Stripe.net`) | Protocol fee only; no rent/deposit handling |
| Maps / Geocoding (backend) | Google Maps Platform — Geocoding API + Address Validation API | HttpClient adapter; REST calls |
| Maps (frontend) | `@react-google-maps/api` npm package | Google Maps JavaScript API |
| KYC / Identity | Persona | Liveness, document auth, synthetic ID detection |
| Background Check | Persona | Combined with KYC — single vendor |
| Background Jobs | Quartz.NET 3.x | Persistent job store: PostgreSQL |
| Object Storage | MinIO (self-hosted, Docker, S3-compatible) | Evidence files, data exports |
| Antivirus | ClamAV (self-hosted, Docker) | REST via `clamav/clamav` Docker image |
| Observability | Serilog + OpenTelemetry | Structured logs → file/console; OTEL traces |
| Frontend Framework | React 19 + Vite + TypeScript | Strict mode, path aliases |
| UI Components | Tailwind CSS + shadcn/ui | No extra component library |
| State Management | Zustand | Lightweight, minimal boilerplate |
| Data Fetching | TanStack Query (React Query) | Server state, caching |
| Frontend Router | React Router v6 | Lazy-loaded routes |
| Payments (frontend) | `@stripe/stripe-js` + `@stripe/react-stripe-js` | Stripe Elements for payment method capture |
| Testing (.NET) | xUnit + FluentAssertions + NSubstitute + Bogus + TestContainers | |
| Testing (frontend) | Vitest + React Testing Library + MSW | |
| Deployment | Docker Compose + Nginx (VPS) | Primary; K8s/Terraform = optional Phase 2 |
| CI/CD | GitHub Actions | Lint → test → build → deploy to VPS |
| Secret Management | `.env` + Docker secrets (VPS) | No cloud Key Vault in v1 |

---

## Mandatory Coding Standards (enforced by TreatWarningsAsErrors)

> Every `.cs` file written in this project **must** follow these rules or the build will fail.
> These are not optional — they reflect permanently enabled Roslyn analyzer rules.

### Logging
- **Never** call `logger.LogInformation(...)`, `logger.LogError(...)`, etc. with interpolated strings or parameterised templates directly.
- **Always** use compile-time `[LoggerMessage]` source generation:
  ```csharp
  public sealed partial class MyService(ILogger<MyService> logger) { ... }

  [LoggerMessage(Level = LogLevel.Information, Message = "Did {Thing} for {Id}")]
  private static partial void LogDidThing(ILogger logger, string thing, Guid id);
  ```
- The enclosing class **must** be declared `partial`.

### Null Validation (CA1062)
- All externally visible method parameters must be validated at the top of the method body:
  ```csharp
  ArgumentNullException.ThrowIfNull(parameter);
  ```

### IDisposable (CA2000)
- Disposable objects (`MimeMessage`, `SmtpClient`, `HttpClient`, stream types, etc.) must be wrapped with `using var` or a `using` block before any `await`.

### Exception Handling (CA1031)
- Never use a bare `catch { }` or `catch (Exception) { }` to swallow exceptions silently.
- Catch the most specific type available (e.g., `SecurityTokenException`, `HttpRequestException`, `DbException`).
- If a broad catch is unavoidable (top-level exception boundary), rethrow or log then rethrow.

### Token Validation — `ValidateLifetime = false` (CA5404)
- Setting `ValidateLifetime = false` in `TokenValidationParameters` triggers CA5404.
- This is **only** permitted in the refresh-token helper (`GetPrincipalFromExpiredToken`) where the lifetime is intentionally skipped because it is controlled by the `RefreshToken` entity expiry instead.
- Suppress with a scoped pragma and a mandatory comment explaining the intent:
  ```csharp
  #pragma warning disable CA5404 // intentional: lifetime checked via RefreshToken entity
  var parameters = new TokenValidationParameters { ValidateLifetime = false, ... };
  #pragma warning restore CA5404
  ```
- Every other `TokenValidationParameters` construction **must** have `ValidateLifetime = true`.

### Braces (IDE0011)
- Always use braces for `if`, `else`, `for`, `foreach`, `while`, `using` blocks, even single-line bodies.

### ConfigureAwait (CA2007)
- `Lagedra.Infrastructure` and `Lagedra.Auth` suppress CA2007 project-wide via `<NoWarn>CA2007</NoWarn>` because ASP.NET Core has no synchronization context.
- New projects that are class libraries **consumed outside ASP.NET Core** must add `.ConfigureAwait(false)` to every `await`.

### DbContext base class selection
- Module DbContexts that extend `BaseDbContext` receive auditing and outbox interceptors automatically and **must** pass `IClock` to the base constructor.
- Module DbContexts that extend `IdentityDbContext` (e.g., `AuthDbContext`) do **not** extend `BaseDbContext` and do **not** take `IClock` — Identity manages its own timestamps.

---

## Current Baseline (as of project creation)

- [x] `src/Lagedra.Api/` — minimal .NET 10 Web API skeleton (single `/weatherforecast` endpoint)
- [x] `src/Lagedra.Api/Lagedra.Api.sln` — solution file (currently inside subfolder, not root)
- [x] `src/Lagedra.Web/` — default Vite + React 19 scaffold (counter template)
- [x] `docker-compose.yml` — PostgreSQL 16, API, Web services wired up
- [x] `.env` — basic environment variables (DB credentials, ports)

---

## Architecture Note: Deal Lifecycle & Application Flow

> **Decision:** Deal state (and the Application that precedes it) lives inside `ActivationAndBilling`.
>
> How it works:
> 1. Tenant applies to a listing → `DealApplication` aggregate created in `ActivationAndBilling`
> 2. Landlord approves application → `DealId` (Guid) is generated and emitted via `ApplicationApprovedEvent`
> 3. All other modules (`TruthSurface`, `InsuranceIntegration`, `ComplianceMonitoring`, etc.) reference this `DealId`
> 4. Once Truth Surface confirmed + insurance verified + first payment initiated → `ActivateDealCommand` runs → `BillingAccount` activated
> 5. Deal status: `ApplicationPending → ApplicationApproved → TruthSurfacePending → TruthSurfaceConfirmed → Active → Closed`
>
> There is **no separate Deal module** and **no separate Application module**.

---

## Phase 0 — Solution Restructuring

> Reshape the repository to match the target layout before any business logic is written.

### 0.1 Root-Level Solution & Config Files

- [x] Move `.sln` from `src/Lagedra.Api/Lagedra.Api.sln` → `Lagedra.sln` at repository root; re-add all future projects
- [x] Create root `Directory.Build.props`:
  ```xml
  <Project>
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
      <LangVersion>latest</LangVersion>
    </PropertyGroup>
  </Project>
  ```
  > Note: Using `net10.0` (only SDK installed is 10.0.102). ASP.NET Core packages use `10.0.x`; EF Core uses `9.0.x` (latest stable that fully supports net10).
- [x] Create root `Directory.Build.targets` (common build targets; relax `TreatWarningsAsErrors` for test projects)
- [x] Create root `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` and pinned versions for:
  - [x] `Microsoft.EntityFrameworkCore` 9.x (Npgsql 10.x not yet stable; upgrade both together)
  - [x] `Microsoft.EntityFrameworkCore.Design` 9.x
  - [x] `Npgsql.EntityFrameworkCore.PostgreSQL` 9.x (must match EF Core major version exactly)
  - [x] `MediatR` 12.x
  - [x] `MediatR.Contracts` 2.x
  - [x] `FluentValidation.AspNetCore` 11.x
  - [x] `Quartz` 3.x
  - [x] `Quartz.Extensions.Hosting` 3.x
  - [x] `Quartz.Serialization.Json` 3.x
  - [x] `Serilog.AspNetCore` 8.x
  - [x] `Serilog.Sinks.Console`
  - [x] `Serilog.Sinks.File`
  - [x] `Serilog.Enrichers.CorrelationId`
  - [x] `OpenTelemetry.Extensions.Hosting`
  - [x] `OpenTelemetry.Instrumentation.AspNetCore`
  - [x] `OpenTelemetry.Instrumentation.EntityFrameworkCore`
  - [x] `Polly` 8.x
  - [x] `Polly.Extensions.Http`
  - [x] `MailKit` (latest stable)
  - [x] `MimeKit` (latest stable)
  - [x] `Stripe.net` (latest stable)
  - [x] `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.x
  - [x] `Microsoft.AspNetCore.Authentication.JwtBearer` 8.x
  - [x] `System.IdentityModel.Tokens.Jwt` 7.x
  - [x] `Microsoft.AspNetCore.OpenApi`
  - [x] `Swashbuckle.AspNetCore`
  - [x] `Asp.Versioning.Mvc`
  - [x] `Asp.Versioning.Mvc.ApiExplorer`
  - [x] `AWSSDK.S3` (for MinIO S3-compatible client)
  - [x] `NetArchTest.Rules`
  - [x] `xunit` 2.x
  - [x] `xunit.runner.visualstudio`
  - [x] `coverlet.collector`
  - [x] `NSubstitute`
  - [x] `FluentAssertions`
  - [x] `Bogus`
  - [x] `Testcontainers.PostgreSql`
  - [x] `Microsoft.AspNetCore.Mvc.Testing`
- [x] Create `global.json` pinning .NET 8 SDK version
- [x] Create root `.gitignore` (.NET, Node, VS/Rider, Docker, `*.user`, `.env` files with secrets)
- [x] Create `.editorconfig` (indent=4 spaces, charset=utf-8, end-of-line=lf, .NET analyser rules)
- [x] Create `.gitattributes` (line-ending normalization, binary file markers)
- [x] Create `README.md` (project overview, local quickstart, architecture summary, tech stack table)

### 0.2 Rename / Migrate Existing Projects

- [x] Rename `src/Lagedra.Api/` → `src/Lagedra.ApiGateway/` (update `.sln` references, `docker-compose.yml`, `Dockerfile`)
- [x] Update `Lagedra.ApiGateway.csproj` to `Microsoft.NET.Sdk.Web`; inherit framework from `Directory.Build.props`
- [x] Remove boilerplate `WeatherForecast` endpoint from `Program.cs`
- [x] Move `src/Lagedra.Web/` → `apps/web/` (update `docker-compose.yml` paths)
- [x] Create `apps/admin/` folder placeholder

### 0.3 Top-Level Folder Scaffolding

- [x] Create `src/` subfolders: `Lagedra.SharedKernel`, `Lagedra.Auth`, `Lagedra.Infrastructure`, `Lagedra.Modules`, `Lagedra.TruthSurface`, `Lagedra.Compliance`, `Lagedra.ApiGateway`, `Lagedra.Worker`
- [x] Create `tests/` subfolders: `Lagedra.Tests.Unit`, `Lagedra.Tests.Integration`, `Lagedra.Tests.Architecture`
- [x] Create `apps/` subfolders: `web`, `admin`, `marketing`
- [x] Create `packages/` subfolders: `ui`, `contracts`, `test-utils`
- [x] Create `deploy/` subfolders: `env`, `nginx`
- [x] Create `docs/` subfolders: `architecture`, `runbooks`, `decisions`
- [x] Create `tools/` subfolders: `scripts`, `postman`, `openapi`

### 0.4 Docker & Compose

- [x] Update `docker-compose.yml` — add all services: `api`, `worker`, `web`, `admin`, `marketing`, `postgres`, `minio`, `clamav`
- [x] Create `docker-compose.override.yml` — local dev overrides (volume mounts, hot-reload, debug ports)
- [x] Create `Dockerfile` — multi-stage: `restore → build → publish → mcr.microsoft.com/dotnet/aspnet:8.0` runtime
- [x] Create `deploy/env/local.env`, `staging.env`, `prod.env` with documented placeholders (never commit real secrets)
- [x] Create `pnpm-workspace.yaml` for monorepo frontend packages (`apps/*`, `packages/*`)
- [x] Create root `package.json` (workspace root: lint, test, build scripts)
- [x] MinIO service in `docker-compose.yml`: image `minio/minio`, volume for persistence, console port exposed
- [x] ClamAV service in `docker-compose.yml`: image `clamav/clamav`, freshclam updates enabled
- [x] Marketing service in `docker-compose.yml`: Next.js app (`node:20-alpine`), port 3001, env var `NEXT_PUBLIC_API_URL` + `NEXT_PUBLIC_SITE_URL`

### 0.5 Tooling Scripts

- [x] `tools/scripts/dev-up.ps1` / `dev-up.sh` — `docker compose up -d` + migrate + seed
- [x] `tools/scripts/dev-down.ps1` / `dev-down.sh` — `docker compose down`
- [x] `tools/scripts/db-migrate.ps1` / `db-migrate.sh` — run EF Core migrations for ALL DbContexts (loop all csproj with `--context`)
- [x] `tools/scripts/db-seed.ps1` / `db-seed.sh` — run seed runner
- [x] `tools/scripts/lint.ps1` / `lint.sh` — `dotnet format --verify-no-changes` + `pnpm lint`
- [x] `tools/scripts/test.ps1` / `test.sh` — `dotnet test` + `pnpm test`

---

## Phase 1 — Authentication (`Lagedra.Auth`)

> ASP.NET Identity, self-hosted, PostgreSQL-backed. JWT access tokens + refresh tokens.
> This module creates `UserId` (Guid) — every other module references this identifier.

### 1.1 Project Setup

- [x] Create `src/Lagedra.Auth/Lagedra.Auth.csproj`
  - References: `Lagedra.SharedKernel`, `Lagedra.Infrastructure`
  - Packages: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`
- [x] Add project to `.sln`

### 1.2 Domain

- [x] `Domain/ApplicationUser.cs` — extends `IdentityUser<Guid>`:
  - `Role` (enum: `Landlord / Tenant / Arbitrator / PlatformAdmin / InsurancePartner / InstitutionPartner`)
  - `IsActive` bool
  - `CreatedAt` DateTime
  - `LastLoginAt` DateTime?
- [x] `Domain/RefreshToken.cs` — entity: `UserId`, `Token` (hashed), `ExpiresAt`, `RevokedAt`, `ReplacedByToken`, `CreatedByIp`
- [x] `Domain/Events/UserRegisteredEvent.cs` — emitted after email verified; downstream modules listen to create identity profile
- [x] `Domain/Events/UserRoleChangedEvent.cs`

### 1.3 Application

- [x] `Application/Commands/RegisterUserCommand.cs` + handler — creates `ApplicationUser`, sends verification email via `IEmailService`
- [x] `Application/Commands/VerifyEmailCommand.cs` + handler — validates token, marks user active, fires `UserRegisteredEvent`
- [x] `Application/Commands/LoginCommand.cs` + handler — validates credentials, returns `AccessToken` + `RefreshToken`
- [x] `Application/Commands/RefreshTokenCommand.cs` + handler — rotates refresh token, returns new pair
- [x] `Application/Commands/RevokeTokenCommand.cs` + handler — invalidates refresh token on logout
- [x] `Application/Commands/ForgotPasswordCommand.cs` + handler — generates reset token, sends email
- [x] `Application/Commands/ResetPasswordCommand.cs` + handler — validates token, updates password
- [x] `Application/Commands/ChangePasswordCommand.cs` + handler
- [x] `Application/Commands/UpdateRoleCommand.cs` + handler — admin only
- [x] `Application/Queries/GetCurrentUserQuery.cs` + handler — returns user profile from token claims
- [x] `Application/Services/JwtTokenService.cs` — generates signed JWT (`HS256`, configurable expiry), embeds: `sub`, `email`, `role`, `jti`
- [x] `Application/Services/RefreshTokenService.cs` — generates cryptographically random token, stores hashed
- [x] `Application/DTOs/AuthResultDto.cs` — `AccessToken`, `RefreshToken`, `ExpiresIn`, `Role`
- [x] `Application/DTOs/UserProfileDto.cs`

### 1.4 Presentation

- [x] `Presentation/Endpoints/AuthEndpoints.cs` (or `AuthController.cs`):
  - `POST /v1/auth/register`
  - `POST /v1/auth/verify-email`
  - `POST /v1/auth/login`
  - `POST /v1/auth/refresh`
  - `POST /v1/auth/logout`
  - `POST /v1/auth/forgot-password`
  - `POST /v1/auth/reset-password`
  - `GET /v1/auth/me`
- [x] `Presentation/Contracts/RegisterRequest.cs` — `Email`, `Password`, `Role` (Landlord/Tenant only at self-registration)
- [x] `Presentation/Contracts/LoginRequest.cs`
- [x] `Presentation/Contracts/RefreshTokenRequest.cs`
- [x] `Presentation/Contracts/ResetPasswordRequest.cs`

### 1.5 Infrastructure

- [x] `Infrastructure/Persistence/AuthDbContext.cs` — extends `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`; schema `auth`
- [x] `Infrastructure/Persistence/Schemas/auth.schema.sql`
- [x] `Infrastructure/Repositories/RefreshTokenRepository.cs`
- [x] `Infrastructure/Configurations/ApplicationUserConfiguration.cs`
- [x] `Infrastructure/Configurations/RefreshTokenConfiguration.cs`
- [x] `Infrastructure/Jobs/RefreshTokenCleanupJob.cs` — nightly: purge expired / revoked tokens older than 30 days
- [x] EF Core migrations for `auth` schema (users, roles, user-roles, refresh_tokens)

### 1.6 Module Registration

- [x] `AuthModuleRegistration.cs` — `AddAuth(IServiceCollection, IConfiguration)`:
  - Configure `Identity` with password policy
  - Configure JWT Bearer authentication
  - Register `JwtTokenService`, `RefreshTokenService`
  - Register `AuthDbContext`, `RefreshTokenRepository`
  - Register MediatR handlers

---

## Phase 2 — Shared Kernel (`Lagedra.SharedKernel`)

> No business logic. No dependencies on other Lagedra projects. Pure abstractions.

### 2.1 Project Setup

- [x] Create `src/Lagedra.SharedKernel/Lagedra.SharedKernel.csproj` (references `MediatR.Contracts` only)
- [x] Add project to `.sln`

### 2.2 Domain Primitives

- [x] `Domain/Entity.cs` — base entity: `Id`, `CreatedAt`, `UpdatedAt`, equality by Id
- [x] `Domain/AggregateRoot.cs` — extends `Entity<TId>`, owns `List<IDomainEvent>`, exposes `AddDomainEvent` / `ClearDomainEvents`
- [x] `Domain/ValueObject.cs` — abstract: `GetEqualityComponents`, structural equality, `==` / `!=` operators
- [x] `Domain/IDomainEvent.cs` — interface: `EventId` (Guid), `OccurredAt` (DateTime)
- [x] `Domain/IAggregateRoot.cs` — marker interface
- [x] `Domain/IRepository.cs` — generic: `GetByIdAsync`, `AddAsync`, `Update`, `UnitOfWork`

### 2.3 Events

- [x] `Events/IEventBus.cs` — `Publish<TEvent>(TEvent, CancellationToken)` async
- [x] `Events/IDomainEventHandler.cs` — `Handle(TEvent, CancellationToken)` async

### 2.4 Persistence

- [x] `Persistence/IUnitOfWork.cs` — `SaveChangesAsync(CancellationToken)`

### 2.5 Time

- [x] `Time/IClock.cs` — `UtcNow` property

### 2.6 Security

- [x] `Security/IHashingService.cs` — `Hash(string): string`, `Verify(string, string): bool`
- [x] `Security/ICryptographicSigner.cs` — `Sign(byte[]): string`, `Verify(byte[], string): bool`

### 2.7 Email Abstraction

- [x] `Email/IEmailService.cs` — `SendAsync(EmailMessage, CancellationToken)` — used by Auth + Notifications
- [x] `Email/EmailMessage.cs` — `To`, `Subject`, `HtmlBody`, `PlainTextBody`, `ReplyTo`

### 2.8 Results / Errors

- [x] `Results/Result.cs` — `Result<T>` discriminated union (Success / Failure)
- [x] `Results/Error.cs` — structured error: `Code`, `Description`

---

## Phase 3 — Infrastructure (`Lagedra.Infrastructure`)

> Shared implementations consumed by all modules. No module-specific business logic.

### 3.1 Project Setup

- [ ] Create `src/Lagedra.Infrastructure/Lagedra.Infrastructure.csproj`
- [ ] Reference `Lagedra.SharedKernel`
- [ ] Packages: EF Core, Npgsql, Serilog, OpenTelemetry, Polly, MailKit, AWSSDK.S3 (MinIO), Stripe.net

### 3.2 Persistence

- [ ] `Persistence/BaseDbContext.cs` — abstract `DbContext` implementing `IUnitOfWork`; applies interceptors
- [ ] `Persistence/DbContextFactory.cs` — design-time factory for `dotnet ef migrations`
- [ ] `Persistence/Interceptors/AuditingInterceptor.cs` — sets `CreatedAt` / `UpdatedAt` on `SaveChangesAsync`
- [ ] `Persistence/Interceptors/OutboxInterceptor.cs` — serializes `IDomainEvent` list to `outbox.outbox_messages` on `SaveChangesAsync`
- [ ] `Persistence/Interceptors/SoftDeleteInterceptor.cs` — filters `IsDeleted=true` entities globally
- [ ] `Persistence/Configurations/OutboxMessageConfiguration.cs`
- [ ] `Persistence/Configurations/AuditEventConfiguration.cs`
- [ ] `Persistence/OutboxMessage.cs` — `Id`, `Type`, `Content`, `OccurredAt`, `ProcessedAt`, `RetryCount`, `Error`
- [ ] `Persistence/Seed/SeedData.cs` — predefined question library, jurisdiction pack seeds
- [ ] `Persistence/Seed/SeedRunner.cs` — idempotent on startup

### 3.3 Eventing

- [ ] `Eventing/InMemoryEventBus.cs` — resolves `IDomainEventHandler<T>` from DI, dispatches in-process
- [ ] `Eventing/OutboxProcessor.cs` — reads unprocessed outbox messages, dispatches, marks processed
- [ ] `Eventing/OutboxDispatcher.cs` — background polling loop (registered in Worker)
- [ ] `Eventing/EventBusExtensions.cs` — DI helpers

### 3.4 External Client Contracts + Implementations

**Email — MailKit + Brevo SMTP**
- [ ] `External/Email/MailKitEmailService.cs` — implements `IEmailService`; uses `MailKit.Net.Smtp.SmtpClient`; configured via `BrevoSmtpSettings` (`Host`, `Port`, `Username`, `Password`) in `appsettings`; HTML + plain-text support; Polly retry (3 attempts, exponential back-off)
- [ ] `External/Email/BrevoSmtpSettings.cs` — typed config: `Host = smtp-relay.brevo.com`, `Port = 587`, `Username`, `ApiKey`
- [ ] Email template engine: Razor `.cshtml` templates compiled with `RazorLight` or inline string templates — decision: **inline string templates** (simplest, no Razor dependency)

**Payments — Stripe**
- [ ] `External/Payments/IStripeService.cs` — `CreateSubscription`, `CancelSubscription`, `CreateProratedInvoice`, `HandleWebhookEvent`
- [ ] `External/Payments/StripeService.cs` — implements using `Stripe.net` (`Stripe.SubscriptionService`, `Stripe.CustomerService`, `Stripe.InvoiceService`); validates webhook signature via `Stripe.EventUtility.ConstructEvent`
- [ ] `External/Payments/StripeSettings.cs` — `PublishableKey`, `SecretKey`, `WebhookSecret`; loaded from environment/Docker secrets

**Geocoding — Google Maps**
- [ ] `External/Geocoding/IGeocodingService.cs` — `GeocodeAddress(string): Task<GeocodingResult>`, `ReverseGeocode(lat, lon): Task<AddressResult>`, `ResolveJurisdiction(string preciseAddress): Task<JurisdictionCode>`
- [ ] `External/Geocoding/GoogleMapsGeocodingService.cs` — implements via `HttpClient` calling `https://maps.googleapis.com/maps/api/geocode/json`; parses address components for city/county/state; Polly retry; API key from settings
- [ ] `External/Geocoding/GoogleMapsSettings.cs` — `ApiKey`

**KYC + Background Check — Persona**
- [ ] `External/Persona/IPersonaClient.cs` — `CreateInquiry`, `GetInquiry`, `HandleWebhook`
- [ ] `External/Persona/PersonaClient.cs` — `HttpClient`-based; Polly retry; webhook signature validation using Persona's HMAC header
- [ ] `External/Persona/PersonaSettings.cs` — `ApiKey`, `TemplateId`, `WebhookSecret`

**Object Storage — MinIO (S3-compatible)**
- [ ] `External/Storage/IObjectStorageService.cs` — `GeneratePresignedUploadUrl`, `GeneratePresignedDownloadUrl`, `DeleteObject`, `ObjectExists`
- [ ] `External/Storage/MinioStorageService.cs` — implements using `AWSSDK.S3` (`AmazonS3Client`) pointed at MinIO endpoint; bucket-per-purpose (evidence, exports)
- [ ] `External/Storage/MinioSettings.cs` — `Endpoint`, `AccessKey`, `SecretKey`, `EvidenceBucket`, `ExportsBucket`

**Antivirus — ClamAV**
- [ ] `External/Antivirus/IAntivirusService.cs` — `ScanAsync(Stream, CancellationToken): Task<ScanResult>`
- [ ] `External/Antivirus/ClamAvService.cs` — HTTP REST calls to ClamAV REST API (`GET /scan`); or TCP socket via `nClam` NuGet package
- [ ] `External/Antivirus/ClamAvSettings.cs` — `Host`, `Port`

**Insurance API**
- [ ] `External/Insurance/IInsuranceApiClient.cs` — `VerifyPolicy`, `GetPolicyStatus`, `HandleWebhook`
- [ ] `External/Insurance/InsuranceApiClient.cs` — stub implementation (real MGA partner TBD); Polly retry + circuit breaker

### 3.5 Security

- [ ] `Security/DataProtectionSetup.cs` — ASP.NET Data Protection, keys persisted to PostgreSQL or filesystem volume
- [ ] `Security/Secrets.cs` — typed configuration loaded from environment variables / Docker secrets
- [ ] `Security/HashingService.cs` — SHA-256 via `System.Security.Cryptography` (implements `IHashingService`)
- [ ] `Security/CryptographicSigner.cs` — HMAC-SHA256 (implements `ICryptographicSigner`); key from `Secrets`

### 3.6 Observability

- [ ] `Observability/Logging.cs` — Serilog: structured, correlation-id-enriched, console + rolling file sinks
- [ ] `Observability/Metrics.cs` — OpenTelemetry metrics (deal activations, arbitration cases, billing events)
- [ ] `Observability/Tracing.cs` — OTEL tracing for HTTP, EF Core, MediatR, outbox
- [ ] `Observability/HealthChecks.cs` — PostgreSQL, MinIO, ClamAV, Persona API, Google Maps API, Brevo SMTP, Stripe API liveness probes

### 3.7 DI Registration

- [ ] `InfrastructureServiceRegistration.cs` — `AddInfrastructure(IServiceCollection, IConfiguration)`:
  - `IClock` → `SystemClock`
  - `IEventBus` → `InMemoryEventBus`
  - `IEmailService` → `MailKitEmailService`
  - `IStripeService` → `StripeService`
  - `IGeocodingService` → `GoogleMapsGeocodingService`
  - `IPersonaClient` → `PersonaClient`
  - `IObjectStorageService` → `MinioStorageService`
  - `IAntivirusService` → `ClamAvService`
  - `IInsuranceApiClient` → `InsuranceApiClient`
  - Serilog, OpenTelemetry, HealthChecks

### 3.8 Shared DB Schemas

- [ ] SQL schema: `outbox` schema, `outbox_messages` table
- [ ] SQL schema: `audit` schema, `audit_events` table
- [ ] EF Core migrations baseline for shared infrastructure schemas

---

## Phase 4 — Core Services

### 4.1 Truth Surface Engine (`Lagedra.TruthSurface`)

> Immutable, cryptographically signed deal snapshots. Append-only. No deletes.

#### Project Setup
- [ ] Create `src/Lagedra.TruthSurface/Lagedra.TruthSurface.csproj`
- [ ] Reference `Lagedra.SharedKernel`, `Lagedra.Infrastructure`
- [ ] Add to `.sln`

#### Presentation
- [ ] `Presentation/Endpoints/TruthSurfaceEndpoints.cs`
- [ ] `Presentation/Contracts/CreateSnapshotRequest.cs`
- [ ] `Presentation/Contracts/SnapshotProofResponse.cs`

#### Domain
- [ ] `Domain/TruthSnapshot.cs` — aggregate: `DealId`, `Status` (Draft → PendingBothConfirmations → Confirmed → Superseded), `SealedAt`, `Hash`, `Signature`, `ProtocolVersion`, `JurisdictionPackVersion`, `InquiryClosed` (bool — included in hash)
- [ ] `Domain/CryptographicProof.cs` — entity: `SnapshotId`, `Hash`, `Signature`, `SignedAt`
- [ ] `Domain/TruthSurfaceStatus.cs` — enum
- [ ] `Domain/TruthSurfaceVersion.cs` — value object
- [ ] Domain event: `TruthSurfaceConfirmedEvent` — consumed by `ActivationAndBilling`, `StructuredInquiry`
- [ ] Domain event: `TruthSurfaceSupersededEvent`

#### Application
- [ ] `Application/Commands/CreateSnapshotCommand.cs` + handler — assembles all line items (listing, tenant declarations, landlord declarations, insurance state, verification class, deposit band, inquiry responses, verified location, jurisdiction pack version)
- [ ] `Application/Commands/ConfirmTruthSurfaceCommand.cs` + handler — validates both-party confirmation; runs canonical JSON → SHA-256 → HMAC-SHA256 signature; sets `InquiryClosed=true`; fires `TruthSurfaceConfirmedEvent`
- [ ] `Application/Commands/ReconfirmTruthSurfaceForPackUpdateCommand.cs` + handler — creates superseding snapshot when law requires
- [ ] `Application/Queries/GetSnapshotQuery.cs` + handler
- [ ] `Application/Queries/VerifySnapshotQuery.cs` + handler — re-computes hash, compares to stored proof
- [ ] `Application/DTOs/TruthSurfaceDto.cs`
- [ ] `Application/DTOs/SnapshotProofDto.cs`

#### Infrastructure / Crypto
- [ ] `Infrastructure/Crypto/Hashing.cs` — `System.Text.Json` canonical serialization → SHA-256 (`System.Security.Cryptography`)
- [ ] `Infrastructure/Crypto/CryptographicSigner.cs` — HMAC-SHA256 using `ICryptographicSigner`
- [ ] `Infrastructure/Crypto/MerkleTreeBuilder.cs` — line-item Merkle tree for partial proof
- [ ] `Infrastructure/Persistence/TruthSurfaceDbContext.cs` — schema `truth_surface`
- [ ] `Infrastructure/Persistence/Schemas/truth_surface.schema.sql`
- [ ] `Infrastructure/Repositories/SnapshotRepository.cs`
- [ ] `Infrastructure/Configurations/TruthSnapshotConfiguration.cs`
- [ ] `Infrastructure/Configurations/CryptographicProofConfiguration.cs`
- [ ] `Infrastructure/Jobs/SnapshotVerificationJob.cs` — weekly: re-computes all hashes, flags mismatches
- [ ] EF Core migrations

#### Module Registration
- [ ] `TruthSurfaceModuleRegistration.cs`

---

### 4.2 Compliance & Trust Ledger (`Lagedra.Compliance`)

> Append-only. No deletes. Ever.

#### Project Setup
- [ ] Create `src/Lagedra.Compliance/Lagedra.Compliance.csproj`
- [ ] Reference `Lagedra.SharedKernel`, `Lagedra.Infrastructure`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Violation.cs` — entity (append-only): `DealId`, `Category` (ViolationCategory enum), `DetectedAt`, `Status`, `EvidenceReference`
- [ ] `Domain/TrustLedgerEntry.cs` — entity (append-only): `UserId`, `EntryType`, `ReferenceId`, `OccurredAt`, `IsPublic`
- [ ] `Domain/ViolationCategory.cs` — enum: NonPayment, UnauthorizedOccupants, PropertyDamage, RuleViolation, InsuranceLapse, EarlyTermination, Other
- [ ] `Domain/ComplianceSignal.cs` — lightweight: `DealId`, `SignalType`, `Payload`, `ReceivedAt`

#### Application
- [ ] `Application/Commands/RecordViolationCommand.cs` + handler
- [ ] `Application/Commands/RecordLedgerEntryCommand.cs` + handler — enforces append-only invariant (no updates, no deletes)
- [ ] `Application/Commands/CloseComplianceWindowCommand.cs` + handler
- [ ] `Application/Queries/GetTrustLedgerForUserQuery.cs` + handler — pseudonymized public view
- [ ] `Application/Queries/GetFullLedgerForDealQuery.cs` + handler — restricted to involved parties
- [ ] `Application/DTOs/ViolationDto.cs`
- [ ] `Application/DTOs/TrustLedgerEntryDto.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/ComplianceDbContext.cs` — schemas `compliance` + `trust_ledger`
- [ ] `Infrastructure/Persistence/Schemas/compliance.schema.sql`
- [ ] `Infrastructure/Persistence/Schemas/trust_ledger.schema.sql`
- [ ] `Infrastructure/Repositories/ViolationRepository.cs`
- [ ] `Infrastructure/Repositories/TrustLedgerRepository.cs` — write-only append, read-only projection
- [ ] EF Core migrations

#### Module Registration
- [ ] `ComplianceModuleRegistration.cs`

---

## Phase 5 — Business Modules (`src/Lagedra.Modules/`)

> All modules follow identical Clean Architecture. No direct references between modules. Communication via domain events through the Outbox only.

---

### 5.1 ActivationAndBilling

> **Owns Deal lifecycle + Application flow + Billing.** `DealId` (Guid) is created here when landlord approves an application. All other modules reference this `DealId`.

#### Project & References
- [ ] `ActivationAndBilling.csproj` — references SharedKernel, Infrastructure, TruthSurface, Compliance, JurisdictionPacks
- [ ] Add to `.sln`

#### Domain — Application & Deal
- [ ] `Domain/Aggregates/DealApplication.cs` — `ListingId`, `TenantUserId`, `LandlordUserId`, `Status` (Pending/Approved/Rejected), `DealId?` (set on approval), `SubmittedAt`, `DecidedAt`
- [ ] `Domain/Events/ApplicationSubmittedEvent.cs`
- [ ] `Domain/Events/ApplicationApprovedEvent.cs` — carries the newly generated `DealId`; consumed by all modules that need to prepare for this deal
- [ ] `Domain/Events/ApplicationRejectedEvent.cs`

#### Domain — Billing
- [ ] `Domain/Aggregates/BillingAccount.cs` — `DealId`, `LandlordUserId`, `TenantUserId`, `Status` (Inactive/Active/Suspended/Closed), `StartDate`, `EndDate`, `StripeCustomerId`, `StripeSubscriptionId`
- [ ] `Domain/Entities/Invoice.cs` — `BillingAccountId`, `StripeInvoiceId`, `PeriodStart`, `PeriodEnd`, `AmountCents`, `ProrationDays`, `Status` (Pending/Paid/Failed/Disputed)
- [ ] `Domain/ValueObjects/Money.cs` — `AmountCents` (int), `Currency` (string, default "USD")
- [ ] `Domain/ValueObjects/ProrationWindow.cs` — computes days from start/end: `days × (7900 / 30)` cents
- [ ] `Domain/Policies/BillingPolicy.cs` — $79/month = 7900 cents; prorated = `7900 / 30 × daysOccupied`; pilot discount = $39 for VerifiedInstitutionalPartner
- [ ] `Domain/Events/DealActivatedEvent.cs`
- [ ] `Domain/Events/PaymentFailedEvent.cs` — triggers protocol protection suspension
- [ ] `Domain/Events/BillingStoppedEvent.cs`

#### Application
- [ ] `Application/Commands/SubmitApplicationCommand.cs` + handler — tenant applies to listing; creates `DealApplication`
- [ ] `Application/Commands/ApproveDealApplicationCommand.cs` + handler — landlord approves; generates `DealId`; fires `ApplicationApprovedEvent`
- [ ] `Application/Commands/RejectDealApplicationCommand.cs` + handler
- [ ] `Application/Commands/ActivateDealCommand.cs` + handler — gates: Truth Surface Confirmed + Insurance Active + Stripe subscription created; fires `DealActivatedEvent`
- [ ] `Application/Commands/StopBillingCommand.cs` + handler — cancels Stripe subscription; fires `BillingStoppedEvent`
- [ ] `Application/Commands/RecordPaymentSucceededCommand.cs` + handler — from Stripe webhook
- [ ] `Application/Commands/RecordPaymentFailedCommand.cs` + handler — suspends protocol protections
- [ ] `Application/Commands/HandleChargebackNoticeCommand.cs` + handler
- [ ] `Application/Commands/CreateStripeCustomerCommand.cs` + handler — creates Stripe customer for landlord on first deal
- [ ] `Application/Queries/GetDealBillingStatusQuery.cs` + handler
- [ ] `Application/Queries/GetProrationQuoteQuery.cs` + handler
- [ ] `Application/Queries/GetApplicationStatusQuery.cs` + handler
- [ ] `Application/Queries/ListApplicationsForListingQuery.cs` + handler
- [ ] `Application/DTOs/BillingStatusDto.cs`
- [ ] `Application/DTOs/ProrationQuoteDto.cs`
- [ ] `Application/DTOs/DealApplicationDto.cs`
- [ ] `Application/Mapping/BillingMappings.cs`

#### Presentation
- [ ] `Presentation/Endpoints/ApplicationEndpoints.cs`
- [ ] `Presentation/Endpoints/ActivationEndpoints.cs`
- [ ] `Presentation/Endpoints/BillingEndpoints.cs`
- [ ] `Presentation/Contracts/SubmitApplicationRequest.cs`
- [ ] `Presentation/Contracts/ApproveApplicationRequest.cs`
- [ ] `Presentation/Contracts/ActivateDealRequest.cs`
- [ ] `Presentation/Contracts/BillingStatusResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/BillingDbContext.cs` — schema `billing`
- [ ] `Infrastructure/Persistence/Schemas/billing.schema.sql`
- [ ] `Infrastructure/Repositories/DealApplicationRepository.cs`
- [ ] `Infrastructure/Repositories/BillingAccountRepository.cs`
- [ ] `Infrastructure/Repositories/InvoiceRepository.cs`
- [ ] `Infrastructure/Configurations/DealApplicationConfiguration.cs`
- [ ] `Infrastructure/Configurations/BillingAccountConfiguration.cs`
- [ ] `Infrastructure/Configurations/InvoiceConfiguration.cs`
- [ ] `Infrastructure/Handlers/StripeWebhookHandler.cs` — validates Stripe signature; dispatches `RecordPaymentSucceededCommand` / `RecordPaymentFailedCommand` / `HandleChargebackNoticeCommand`
- [ ] `Infrastructure/Jobs/BillingReconciliationJob.cs` — daily: retry failed invoices, reconcile Stripe subscription state
- [ ] EF Core migrations

#### Module Registration
- [ ] `ActivationAndBillingModuleRegistration.cs`

---

### 5.2 IdentityAndVerification

#### Project & References
- [ ] `IdentityAndVerification.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/IdentityProfile.cs` — `UserId`, `FirstName`, `LastName`, `DateOfBirth`, `Status` (VerificationStatus enum)
- [ ] `Domain/Aggregates/VerificationCase.cs` — `UserId`, `PersonaInquiryId`, `Status`, `CompletedAt`
- [ ] `Domain/Entities/BackgroundCheckReport.cs` — `UserId`, `PersonaReportId`, `Result` (Pass/Review/Fail), `ReceivedAt`, `ExpiresAt` (7-year retention)
- [ ] `Domain/Entities/AffiliationVerification.cs` — `UserId`, `OrganizationType`, `OrganizationId`, `VerificationMethod` (OAuth/DomainEmail/PartnerAPI), `VerifiedAt`
- [ ] `Domain/ValueObjects/VerificationStatus.cs` — enum: NotStarted, Pending, Verified, Failed, ManualReviewRequired
- [ ] `Domain/ValueObjects/VerificationClass.cs` — enum: Low, Medium, High (computed in VerificationAndRisk module)
- [ ] `Domain/ValueObjects/ConfidenceIndicator.cs` — High/Medium/Low + reason text
- [ ] `Domain/Events/IdentityVerifiedEvent.cs`
- [ ] `Domain/Events/IdentityVerificationFailedEvent.cs`
- [ ] `Domain/Events/BackgroundCheckReceivedEvent.cs`
- [ ] `Domain/Events/AffiliationVerifiedEvent.cs`
- [ ] `Domain/Events/FraudFlagRaisedEvent.cs`
- [ ] `Domain/Events/VerificationClassChangedEvent.cs`

#### Application
- [ ] `Application/Commands/StartKycCommand.cs` + handler — calls `IPersonaClient.CreateInquiry`
- [ ] `Application/Commands/CompleteKycCommand.cs` + handler — processes Persona webhook; updates status
- [ ] `Application/Commands/SubmitBackgroundCheckConsentCommand.cs` + handler — FCRA consent flow; calls Persona background check API
- [ ] `Application/Commands/IngestBackgroundCheckResultCommand.cs` + handler — Persona webhook ingestion
- [ ] `Application/Commands/VerifyInstitutionAffiliationCommand.cs` + handler — OAuth/domain-email gating; unverified claims discarded + flagged
- [ ] `Application/Commands/CreateFraudFlagCommand.cs` + handler
- [ ] `Application/Queries/GetVerificationStatusQuery.cs` + handler
- [ ] `Application/Queries/GetFraudFlagsQuery.cs` + handler
- [ ] `Application/DTOs/VerificationStatusDto.cs`
- [ ] `Application/DTOs/FraudFlagDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/IdentityEndpoints.cs`
- [ ] `Presentation/Endpoints/VerificationEndpoints.cs`
- [ ] `Presentation/Endpoints/PersonaWebhookEndpoints.cs`
- [ ] `Presentation/Contracts/StartKycRequest.cs`
- [ ] `Presentation/Contracts/VerificationStatusResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/IdentityDbContext.cs` — schema `identity`
- [ ] `Infrastructure/Persistence/Schemas/identity.schema.sql`
- [ ] `Infrastructure/Repositories/IdentityProfileRepository.cs`
- [ ] `Infrastructure/Repositories/VerificationCaseRepository.cs`
- [ ] `Infrastructure/Configurations/IdentityProfileConfiguration.cs`
- [ ] `Infrastructure/Configurations/VerificationCaseConfiguration.cs`
- [ ] `Infrastructure/Handlers/PersonaWebhookHandler.cs` — validates Persona HMAC signature; dispatches complete/fail commands
- [ ] `Infrastructure/Jobs/FraudFlagSlaMonitorJob.cs` — every 15 min: escalate unresolved High-severity flags past 24h
- [ ] EF Core migrations (identity_profiles, verification_cases, background_check_reports, affiliation_verifications)

#### Module Registration
- [ ] `IdentityVerificationModuleRegistration.cs`

---

### 5.3 InsuranceIntegration

#### Project & References
- [ ] `InsuranceIntegration.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/InsurancePolicyRecord.cs` — `TenantUserId`, `DealId`, `State` (InsuranceState enum), `Provider`, `PolicyNumber`, `VerifiedAt`, `ExpiresAt`, `CoverageScope`
- [ ] `Domain/Entities/InsuranceVerificationAttempt.cs` — `PolicyRecordId`, `AttemptedAt`, `Result`, `Source` (API/ManualUpload)
- [ ] `Domain/ValueObjects/InsuranceState.cs` — enum: NotActive, Active, InstitutionBacked, Unknown
- [ ] `Domain/ValueObjects/CoverageRequirements.cs` — minimum coverage type + amount (pulled from listing)
- [ ] `Domain/Policies/UnknownGraceWindowPolicy.cs` — 72h grace: API failure → Unknown (not lapsed); tenant inaction past 72h → lapse violation; partner failure past 72h → manual review (no violation)
- [ ] `Domain/Events/InsuranceStatusChangedEvent.cs`
- [ ] `Domain/Events/InsuranceUnknownSlaBreachedEvent.cs`

#### Application
- [ ] `Application/Commands/StartInsuranceVerificationCommand.cs` + handler
- [ ] `Application/Commands/RecordInsuranceActiveCommand.cs` + handler
- [ ] `Application/Commands/RecordInsuranceNotActiveCommand.cs` + handler
- [ ] `Application/Commands/RecordInsuranceUnknownCommand.cs` + handler — starts 72h grace timer; notifies both parties via `IEmailService`
- [ ] `Application/Commands/UploadManualProofCommand.cs` + handler — uploads to MinIO; notifies ops team
- [ ] `Application/Commands/HandleInsurancePurchaseWebhookCommand.cs` + handler
- [ ] `Application/Commands/CompleteManualVerificationCommand.cs` + handler — ops team confirms within 24h SLA
- [ ] `Application/Queries/GetInsuranceStatusQuery.cs` + handler
- [ ] `Application/Queries/GetPartnerQuotationsQuery.cs` + handler
- [ ] `Application/Queries/GetInsuranceUnknownQueueQuery.cs` + handler — admin ops queue
- [ ] `Application/DTOs/InsuranceStatusDto.cs`
- [ ] `Application/DTOs/InsuranceQueueItemDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/InsuranceEndpoints.cs`
- [ ] `Presentation/Endpoints/InsuranceWebhookEndpoints.cs`
- [ ] `Presentation/Contracts/ManualProofUploadRequest.cs`
- [ ] `Presentation/Contracts/InsuranceStatusResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/InsuranceDbContext.cs` — schema `insurance`
- [ ] `Infrastructure/Persistence/Schemas/insurance.schema.sql`
- [ ] `Infrastructure/Repositories/InsurancePolicyRecordRepository.cs`
- [ ] `Infrastructure/Configurations/InsurancePolicyRecordConfiguration.cs`
- [ ] `Infrastructure/Jobs/InsurancePollerJob.cs` — hourly: polls active policies via `IInsuranceApiClient`
- [ ] `Infrastructure/Jobs/InsuranceUnknownSlaJob.cs` — every 30 min: fires `InsuranceUnknownSlaBreachedEvent` at 72h
- [ ] EF Core migrations

#### Module Registration
- [ ] `InsuranceIntegrationModuleRegistration.cs`

---

### 5.4 ListingAndLocation

#### Project & References
- [ ] `ListingAndLocation.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/Listing.cs` — `LandlordUserId`, `Status` (Draft/Published/Activated/Closed), `StayRange`, `MonthlyRentCents`, `InsuranceRequired`, structured attributes (utilities, furnishings, rules, restrictions, appliances), `ApproxGeoPoint`, `PreciseAddress` (AES-256 encrypted at rest via EF Core value converter), `JurisdictionCode`
- [ ] `Domain/ValueObjects/Address.cs` — street, city, state, zip, country
- [ ] `Domain/ValueObjects/GeoPoint.cs` — `Lat`, `Lon`; used for approx pin
- [ ] `Domain/ValueObjects/StayRange.cs` — `MinDays`, `MaxDays`; validator: 30 ≤ min ≤ max ≤ 180
- [ ] `Domain/Events/ListingPublishedEvent.cs`
- [ ] `Domain/Events/ListingActivatedEvent.cs`
- [ ] `Domain/Events/PreciseAddressLockedEvent.cs` — carries `JurisdictionCode` for downstream gating

#### Application
- [ ] `Application/Commands/CreateListingCommand.cs` + handler — structured fields only; no freeform text
- [ ] `Application/Commands/UpdateListingCommand.cs` + handler
- [ ] `Application/Commands/PublishListingCommand.cs` + handler — calls jurisdiction compliance validation gate before publish
- [ ] `Application/Commands/SetApproxLocationCommand.cs` + handler — stores approx `GeoPoint` (Google Maps geocode of rough area)
- [ ] `Application/Commands/LockPreciseAddressOnActivationCommand.cs` + handler — encrypts + stores `PreciseAddress`; calls `IGeocodingService.ResolveJurisdiction`; fires `PreciseAddressLockedEvent`
- [ ] `Application/Queries/SearchListingsQuery.cs` + handler — filter by approx location radius, stay range, price range; non-promotional
- [ ] `Application/Queries/GetListingDetailsQuery.cs` + handler — returns approx pin pre-activation, decrypted address post-activation (only to authorized parties)
- [ ] `Application/DTOs/ListingSummaryDto.cs`
- [ ] `Application/DTOs/ListingDetailsDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/ListingEndpoints.cs`
- [ ] `Presentation/Endpoints/LocationEndpoints.cs`
- [ ] `Presentation/Contracts/CreateListingRequest.cs`
- [ ] `Presentation/Contracts/ListingDetailsResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/ListingsDbContext.cs` — schema `listings`
- [ ] `Infrastructure/Persistence/Schemas/listings.schema.sql`
- [ ] `Infrastructure/Adapters/GeocodingClientAdapter.cs` — wraps `IGeocodingService`; AES-256 value converter for `PreciseAddress` column via EF Core `ValueConverter`
- [ ] `Infrastructure/Repositories/ListingRepository.cs`
- [ ] `Infrastructure/Configurations/ListingConfiguration.cs` — configures encrypted column
- [ ] `Infrastructure/Jobs/JurisdictionResolutionJob.cs` — nightly sweep: re-derives `JurisdictionCode` for any listing missing it
- [ ] EF Core migrations

#### Module Registration
- [ ] `ListingAndLocationModuleRegistration.cs`

---

### 5.5 StructuredInquiry

> Not a messaging system. One-directional schema-bound data capture. Permanently closed on Truth Surface confirmation.

#### Project & References
- [ ] `StructuredInquiry.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/InquirySession.cs` — `DealId`, `Status` (Locked/Open/Closed), `UnlockedByLandlordAt?`, `ClosedAt?`
- [ ] `Domain/Entities/InquiryQuestion.cs` — `SessionId`, `Category` (enum: UtilitySpecifics/AccessibilityLayout/RuleClarification/Proximity), `PredefinedQuestionId` (FK to seed data), `SubmittedAt`
- [ ] `Domain/Entities/InquiryAnswer.cs` — `QuestionId`, `ResponseType` (YesNo/MultipleChoice/Numeric), `AnswerValue`, `AnsweredAt` — promoted to Landlord Declaration on Truth Surface creation
- [ ] `Domain/Events/InquiryLoggedAsComplianceSignalEvent.cs`
- [ ] `Domain/Events/InquiryClosedEvent.cs` — triggers `CloseInquiryOnTruthSurfaceConfirmationCommand`

#### Application
- [ ] `Application/Commands/RequestDetailUnlockCommand.cs` + handler — tenant requests; default disabled
- [ ] `Application/Commands/ApproveInquiryUnlockCommand.cs` + handler — landlord explicitly approves
- [ ] `Application/Commands/SubmitInquiryQuestionCommand.cs` + handler — only predefined question IDs; contact-info bypass detection (regex scan for phone numbers, emails in response slot context)
- [ ] `Application/Commands/SubmitLandlordResponseCommand.cs` + handler — structured response only; auto-logs compliance signal; auto-promotes to Landlord Declaration queue
- [ ] `Application/Commands/CloseInquiryOnTruthSurfaceConfirmationCommand.cs` + handler — permanent lock; event fires `InquiryClosedEvent`; `IEmailService` sends "The Inquiry Service is now closed" notice to both parties
- [ ] `Application/Queries/GetInquiryThreadQuery.cs` + handler
- [ ] `Application/Queries/ListPredefinedQuestionsQuery.cs` + handler
- [ ] `Application/DTOs/InquiryDto.cs`
- [ ] `Application/DTOs/PredefinedQuestionDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/InquiryEndpoints.cs`
- [ ] `Presentation/Contracts/SubmitInquiryQuestionRequest.cs`
- [ ] `Presentation/Contracts/SubmitLandlordResponseRequest.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/InquiryDbContext.cs` — schema `inquiry`
- [ ] `Infrastructure/Persistence/Schemas/inquiry.schema.sql`
- [ ] `Infrastructure/Repositories/InquirySessionRepository.cs`
- [ ] `Infrastructure/Configurations/InquirySessionConfiguration.cs`
- [ ] `Infrastructure/Jobs/InquiryIntegrityScanJob.cs` — daily: detect systematic landlord rejection patterns; detect contact-info bypass (regex); log Trust Ledger penalty
- [ ] EF Core migrations
- [ ] Seed: predefined question library (counsel-vetted IDs + text)

#### Module Registration
- [ ] `StructuredInquiryModuleRegistration.cs`

---

### 5.6 VerificationAndRisk

> Deterministic Verification Class (v1). No ML. No actuarial loss model.

#### Project & References
- [ ] `VerificationAndRisk.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/RiskProfile.cs` — `TenantUserId`, `VerificationClass` (Low/Medium/High), `ConfidenceIndicator`, `DepositBandLowCents`, `DepositBandHighCents`, `ComputedAt`, `InputHash` (hash of all inputs for audit)
- [ ] `Domain/Policies/VerificationClassPolicy.cs` — deterministic rules engine:
  - Low: identity verified + background Pass + insurance Active/InstitutionBacked + no violations
  - Medium: identity verified + background Pass/Review + insurance Active
  - High: identity failed/pending, or background Fail, or no insurance
  - **Explicitly excludes**: race, color, religion, national origin, sex, familial status, disability
- [ ] `Domain/Policies/DepositRecommendationPolicy.cs` — deposit band = `VerificationClass × InsuranceState × JurisdictionCap`; adverse action limitation enforced; automated adjustment for verified service members
- [ ] `Domain/Events/DepositBandUpdatedEvent.cs`
- [ ] `Domain/Events/VerificationClassComputedEvent.cs`

#### Application
- [ ] `Application/Commands/RecalculateVerificationClassCommand.cs` + handler — triggered by: `IdentityVerifiedEvent`, `InsuranceStatusChangedEvent`, `TrustLedgerEntryRecordedEvent`
- [ ] `Application/Commands/ComputeDepositBandCommand.cs` + handler — fetches active jurisdiction cap from JurisdictionPacks
- [ ] `Application/Queries/GetRiskViewForLandlordQuery.cs` + handler — returns class + confidence + deposit band; raw signals not exposed
- [ ] `Application/DTOs/RiskViewDto.cs`
- [ ] `Application/DTOs/DepositBandDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/RiskEndpoints.cs`
- [ ] `Presentation/Contracts/RiskViewResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/RiskDbContext.cs` — schema `risk`
- [ ] `Infrastructure/Persistence/Schemas/risk.schema.sql`
- [ ] `Infrastructure/Repositories/RiskProfileRepository.cs`
- [ ] EF Core migrations

#### Module Registration
- [ ] `VerificationAndRiskModuleRegistration.cs`

---

### 5.7 ComplianceMonitoring

#### Project & References
- [ ] `ComplianceMonitoring.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Entities/Violation.cs` — `DealId`, `Category` (A–G), `DetectedAt`, `CureDeadline`, `Status` (Open/Cured/Escalated)
- [ ] `Domain/Entities/ComplianceSignal.cs` — `DealId`, `SignalType`, `Source`, `ReceivedAt`
- [ ] `Domain/ValueObjects/ViolationCategory.cs`
- [ ] `Domain/Events/ViolationRecordedEvent.cs`
- [ ] `Domain/Events/InsuranceLapseViolationCreatedEvent.cs`

#### Application
- [ ] `Application/Commands/DetectViolationCommand.cs` + handler
- [ ] `Application/Commands/RecordComplianceSignalCommand.cs` + handler — ingests signals from all modules via Outbox
- [ ] `Application/Commands/CloseComplianceWindowCommand.cs` + handler
- [ ] `Application/Queries/GetDealComplianceStatusQuery.cs` + handler
- [ ] `Application/Queries/ListViolationsQuery.cs` + handler
- [ ] `Application/DTOs/ViolationDto.cs`
- [ ] `Application/DTOs/ComplianceStatusDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/ComplianceEndpoints.cs`
- [ ] `Presentation/Contracts/ComplianceStatusResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/ComplianceDbContext.cs` — schema `compliance_monitoring`
- [ ] `Infrastructure/Persistence/Schemas/compliance.schema.sql`
- [ ] `Infrastructure/Repositories/ViolationRepository.cs`
- [ ] `Infrastructure/Configurations/ViolationConfiguration.cs`
- [ ] `Infrastructure/Jobs/ComplianceScannerJob.cs` — every 6h: check active deals for insurance lapse, overdue cure windows, missing evidence kits; send email alerts via `IEmailService`
- [ ] EF Core migrations

#### Module Registration
- [ ] `ComplianceMonitoringModuleRegistration.cs`

---

### 5.8 Arbitration

#### Project & References
- [ ] `Arbitration.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/ArbitrationCase.cs` — `DealId`, `Tier` (ProtocolAdjudication/BindingArbitration), `Category` (A–G + Other), `Status` (Filed/EvidencePending/EvidenceComplete/UnderReview/Decided/Appealed), `FiledAt`, `EvidenceCompleteAt`, `DecisionDueAt` (14 calendar days from EvidenceComplete)
- [ ] `Domain/Entities/EvidenceSlot.cs` — `CaseId`, `SlotType`, `SubmittedBy`, `FileReference` (MinIO key), `SubmittedAt`
- [ ] `Domain/Entities/ArbitratorAssignment.cs` — `CaseId`, `ArbitratorUserId`, `AssignedAt`, `ConcurrentCaseCount`
- [ ] `Domain/Policies/EvidenceMinimumThresholdPolicy.cs` — per-category minimum bundle (A–G per spec); Category G: requires closest-category mapping + Truth Surface line item citation + ≤200-word justification
- [ ] `Domain/Events/CaseFiledEvent.cs`
- [ ] `Domain/Events/EvidenceCompleteEvent.cs`
- [ ] `Domain/Events/DecisionIssuedEvent.cs`
- [ ] `Domain/Events/ArbitrationBacklogEscalationEvent.cs`

#### Application
- [ ] `Application/Commands/FileCaseCommand.cs` + handler — gates: deal active, fee current, category valid, minimum evidence schema met, initiation deposit recorded (beta friction)
- [ ] `Application/Commands/AttachEvidenceCommand.cs` + handler — structured slots only; late evidence rule; file stored in MinIO
- [ ] `Application/Commands/MarkEvidenceCompleteCommand.cs` + handler — starts 14-day SLA clock
- [ ] `Application/Commands/AssignArbitratorCommand.cs` + handler — random from panel; no prior cases with either party; joint rejection allowed once; hard cap 20 / soft 15
- [ ] `Application/Commands/IssueProtocolDecisionCommand.cs` + handler — Tier 1; records to Trust Ledger
- [ ] `Application/Commands/IssueBindingAwardCommand.cs` + handler — Tier 2; records to Trust Ledger; generates court-confirmation template
- [ ] `Application/Queries/GetCaseQuery.cs` + handler
- [ ] `Application/Queries/ListCasesByStatusQuery.cs` + handler
- [ ] `Application/DTOs/CaseDto.cs`
- [ ] `Application/DTOs/DecisionDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/ArbitrationEndpoints.cs`
- [ ] `Presentation/Endpoints/ArbitratorEndpoints.cs`
- [ ] `Presentation/Contracts/FileCaseRequest.cs`
- [ ] `Presentation/Contracts/IssueDecisionRequest.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/ArbitrationDbContext.cs` — schema `arbitration`
- [ ] `Infrastructure/Persistence/Schemas/arbitration.schema.sql`
- [ ] `Infrastructure/Repositories/ArbitrationCaseRepository.cs`
- [ ] `Infrastructure/Configurations/ArbitrationCaseConfiguration.cs`
- [ ] `Infrastructure/Jobs/ArbitrationBacklogSlaJob.cs` — hourly: caseload per arbitrator; soft threshold 15 → load balance; hard cap 20 → block; triage: safety/habitability → move-out → FIFO
- [ ] EF Core migrations

#### Module Registration
- [ ] `ArbitrationModuleRegistration.cs`

---

### 5.9 JurisdictionPacks

> Dual-control approval. Version-locked to each active deal. California / LA is the v1 jurisdiction.

#### Project & References
- [ ] `JurisdictionPacks.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/JurisdictionPack.cs` — `JurisdictionCode`, `ActiveVersionId`, `Versions` list
- [ ] `Domain/Entities/PackVersion.cs` — `VersionNumber`, `Status` (Draft/PendingApproval/Active/Deprecated), `EffectiveDate`, `ApprovedAt`, `ApprovedBy` (requires 2 distinct admin users)
- [ ] `Domain/Entities/EffectiveDateRule.cs` — `FieldName`, `EffectiveDate` (e.g. AB 2801 pre-occupancy photos: July 1 2025)
- [ ] `Domain/Entities/FieldGatingRule.cs` — `FieldName`, `GatingType` (Hard/Soft), `Value`, `Condition`
- [ ] `Domain/Entities/EvidenceSchedule.cs` — per-category minimum evidence requirements for this jurisdiction
- [ ] `Domain/ValueObjects/JurisdictionCode.cs` — format `US-CA-LA`
- [ ] `Domain/ValueObjects/RuleExpression.cs` — simple DSL string for field gating conditions
- [ ] `Domain/Events/JurisdictionPackPublishedEvent.cs`
- [ ] `Domain/Events/PackEffectiveDateChangedEvent.cs`

#### Application — California / LA v1 Pack
- [ ] AB 12: 1× deposit cap default; 2× small-landlord exception with certification logged
- [ ] SB 611: military status tracking; higher deposit tracking + 6-month return window
- [ ] AB 628: stove mandatory (no waiver); refrigerator tenant-opt-in with lease language; 30-day provider window on tenant withdrawal
- [ ] AB 2801: post-vacancy photos gate Apr 1 2025; pre-occupancy photos gate Jul 1 2025
- [ ] AB 414: Direct-to-Counterparty Refund Instructions mandatory; UI disclaimer enforced
- [ ] JCO: Relocation Assistance Disclaimer triggered at 175-day stay mark (email via `IEmailService`)
- [ ] `Application/Commands/CreatePackDraftCommand.cs` + handler
- [ ] `Application/Commands/UpdatePackDraftCommand.cs` + handler
- [ ] `Application/Commands/ValidatePackCommand.cs` + handler
- [ ] `Application/Commands/RequestDualControlApprovalCommand.cs` + handler
- [ ] `Application/Commands/ApprovePackVersionCommand.cs` + handler — requires 2nd distinct approver
- [ ] `Application/Commands/PublishPackVersionCommand.cs` + handler
- [ ] `Application/Commands/DeprecatePackVersionCommand.cs` + handler
- [ ] `Application/Queries/GetActivePackForJurisdictionQuery.cs` + handler
- [ ] `Application/Queries/GetPackVersionDetailsQuery.cs` + handler
- [ ] `Application/Queries/ListPackVersionsQuery.cs` + handler
- [ ] `Application/DTOs/JurisdictionPackDto.cs`, `FieldGateRuleDto.cs`, `EvidenceScheduleDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/JurisdictionPackEndpoints.cs`
- [ ] `Presentation/Contracts/CreatePackVersionRequest.cs`, `JurisdictionPackResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/JurisdictionDbContext.cs` — schema `jurisdiction`
- [ ] `Infrastructure/Persistence/Schemas/jurisdiction.schema.sql`
- [ ] `Infrastructure/Repositories/JurisdictionPackRepository.cs`
- [ ] `Infrastructure/Configurations/JurisdictionPackConfiguration.cs`, `PackVersionConfiguration.cs`
- [ ] `Infrastructure/Jobs/PackEffectiveDateActivationJob.cs` — daily at midnight: promote Pack Version to Active on effective date
- [ ] EF Core migrations
- [ ] Seed data: California / LA v1 pack with all rules above

#### Module Registration
- [ ] `JurisdictionPacksModuleRegistration.cs`

---

### 5.10 Evidence

#### Project & References
- [ ] `Evidence.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/EvidenceManifest.cs` — `DealId`, `ManifestType` (MoveIn/MoveOut/Arbitration/Insurance), `Status` (Open/Sealed), `SealedAt`, `HashOfAllFiles`
- [ ] `Domain/Entities/EvidenceUpload.cs` — `ManifestId`, `OriginalFileName`, `StorageKey` (MinIO object key), `FileHash` (SHA-256), `MimeType`, `UploadedAt`, `TimestampMetadata` (from EXIF strip log)
- [ ] `Domain/Entities/MalwareScanResult.cs` — `UploadId`, `Status` (Pending/Clean/Infected), `ScannedAt`
- [ ] `Domain/Entities/MetadataStrippingLog.cs` — `UploadId`, `StrippedAt`, `RemovedFields` (JSON list)
- [ ] `Domain/ValueObjects/FileHash.cs` — SHA-256 hex string
- [ ] `Domain/ValueObjects/ScanStatus.cs` — enum
- [ ] Domain events: `EvidenceUploadedEvent`, `EvidenceScannedEvent`, `EvidenceManifestCreatedEvent`, `EvidenceManifestSealedEvent`

#### Application
- [ ] `Application/Commands/RequestUploadUrlCommand.cs` + handler — calls `IObjectStorageService.GeneratePresignedUploadUrl` (MinIO); returns time-limited URL
- [ ] `Application/Commands/CompleteUploadCommand.cs` + handler — records file hash; starts malware scan via `IAntivirusService`
- [ ] `Application/Commands/StartMalwareScanCommand.cs` + handler — sends file stream to ClamAV
- [ ] `Application/Commands/RecordScanResultCommand.cs` + handler — Clean: mark ready; Infected: quarantine in MinIO, notify ops via email
- [ ] `Application/Commands/StripMetadataCommand.cs` + handler — removes PII from EXIF using `MetadataExtractor` NuGet
- [ ] `Application/Commands/CreateEvidenceManifestCommand.cs` + handler
- [ ] `Application/Commands/SealEvidenceManifestCommand.cs` + handler — SHA-256 hash of all file hashes; immutable
- [ ] `Application/Commands/ArchiveEvidenceCommand.cs` + handler — sets MinIO lifecycle policy: 7-year retention
- [ ] `Application/Queries/GetManifestQuery.cs` + handler
- [ ] `Application/Queries/GetScanStatusQuery.cs` + handler
- [ ] `Application/DTOs/UploadUrlDto.cs`, `ManifestDto.cs`, `ScanResultDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/EvidenceEndpoints.cs`
- [ ] `Presentation/Endpoints/UploadEndpoints.cs`
- [ ] `Presentation/Contracts/RequestUploadUrlRequest.cs`, `SubmitManifestRequest.cs`, `EvidenceManifestResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/EvidenceDbContext.cs` — schema `evidence`
- [ ] `Infrastructure/Persistence/Schemas/evidence.schema.sql`
- [ ] `Infrastructure/Repositories/EvidenceManifestRepository.cs`
- [ ] `Infrastructure/Configurations/EvidenceUploadConfiguration.cs`, `EvidenceManifestConfiguration.cs`
- [ ] `Infrastructure/Jobs/MalwareScanPollingJob.cs` — every 5 min: poll ClamAV scan status for pending uploads
- [ ] `Infrastructure/Jobs/EvidenceRetentionJob.cs` — nightly: enforce 7-year MinIO lifecycle; anonymize after 2 years inactivity
- [ ] EF Core migrations
- [ ] Add `MetadataExtractor` NuGet to `Directory.Packages.props`

#### Module Registration
- [ ] `EvidenceModuleRegistration.cs`

---

### 5.11 Notifications

> Email-only in v1. SMS deferred to v2. Uses `IEmailService` (MailKit + Brevo SMTP).

#### Project & References
- [ ] `Notifications.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/Notification.cs` — `RecipientUserId`, `RecipientEmail`, `Channel` (Email only in v1), `TemplateId`, `Status` (Queued/Sent/Failed/Delivered), `ScheduledAt`, `SentAt`
- [ ] `Domain/Entities/NotificationTemplate.cs` — `TemplateId`, `Channel`, `Subject`, `HtmlBody` (inline string template with `{placeholder}` tokens), `PlainTextBody`
- [ ] `Domain/Entities/DeliveryLog.cs` — `NotificationId`, `BrevoMessageId`, `DeliveredAt?`, `Error?`
- [ ] `Domain/Entities/UserNotificationPreferences.cs` — `UserId`, per-event-type opt-in (transactional system notices always sent regardless)
- [ ] Domain events: `NotificationQueuedEvent`, `NotificationDeliveredEvent`, `NotificationFailedEvent`
- [ ] **Required system notice templates** (all email, hardcoded, non-opt-out):
  - Insurance lapse alert
  - Insurance unknown status (72h grace start)
  - Deal activation confirmation
  - Truth Surface confirmation ("The Inquiry Service is now closed. All confirmed details are recorded in the Truth Surface")
  - Compliance violation detected
  - Arbitration case filed
  - Arbitration decision issued
  - Billing payment failure (protocol protections suspended)
  - Deal closure prompt
  - Relocation Assistance Disclaimer (175-day trigger, email to landlord)
  - Cure-window reminder
  - Email verification (from Auth module)
  - Password reset (from Auth module)

#### Application
- [ ] `Application/Commands/SendEmailNotificationCommand.cs` + handler — calls `IEmailService.SendAsync` (MailKit → Brevo SMTP)
- [ ] `Application/Commands/SendInAppNotificationCommand.cs` + handler — stores for in-app notification feed
- [ ] `Application/Commands/QueueNotificationCommand.cs` + handler — persists to DB; outbox dispatches
- [ ] `Application/Commands/MarkNotificationDeliveredCommand.cs` + handler
- [ ] `Application/Commands/UpdateUserPreferencesCommand.cs` + handler
- [ ] `Application/Queries/GetUserPreferencesQuery.cs` + handler
- [ ] `Application/Queries/ListNotificationHistoryQuery.cs` + handler
- [ ] `Application/DTOs/NotificationDto.cs`, `NotificationPreferencesDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/NotificationPreferencesEndpoints.cs`
- [ ] `Presentation/Contracts/UpdatePreferencesRequest.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/NotificationDbContext.cs` — schema `notifications`
- [ ] `Infrastructure/Persistence/Schemas/notifications.schema.sql`
- [ ] `Infrastructure/Repositories/NotificationRepository.cs`
- [ ] `Infrastructure/Repositories/TemplateRepository.cs`
- [ ] `Infrastructure/Configurations/NotificationConfiguration.cs`
- [ ] `Infrastructure/Jobs/NotificationRetryJob.cs` — every 10 min: retry failed emails (max 5 attempts, exponential back-off via Polly)
- [ ] Seed: all required system notice templates
- [ ] EF Core migrations

#### Module Registration
- [ ] `NotificationsModuleRegistration.cs`

---

### 5.12 Privacy

#### Project & References
- [ ] `Privacy.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/UserConsent.cs` — `UserId`, consent records list
- [ ] `Domain/Entities/ConsentRecord.cs` — `UserId`, `ConsentType`, `GrantedAt`, `WithdrawnAt`, `IpAddress`, `UserAgent`
- [ ] `Domain/Entities/LegalHold.cs` — `UserId`, `Reason`, `AppliedAt`, `ReleasedAt`
- [ ] `Domain/Entities/DataExportRequest.cs` — `UserId`, `Status`, `RequestedAt`, `CompletedAt`, `PackageUrl` (MinIO signed URL)
- [ ] `Domain/Entities/DeletionRequest.cs` — `UserId`, `Status`, `RequestedAt`, `CompletedAt`, `BlockingReason`
- [ ] `Domain/ValueObjects/ConsentType.cs` — enum: KYCConsent, FCRAConsent, MarketingEmail, DataProcessing
- [ ] `Domain/ValueObjects/RetentionPeriod.cs` — 7-year core records, 2-year inactive profile, 30-day cancelled pre-activation
- [ ] Domain events: `ConsentRecordedEvent`, `LegalHoldAppliedEvent`, `DataAnonymizedEvent`

#### Application
- [ ] `Application/Commands/RecordConsentCommand.cs` + handler
- [ ] `Application/Commands/WithdrawConsentCommand.cs` + handler
- [ ] `Application/Commands/EnqueueDataExportCommand.cs` + handler
- [ ] `Application/Commands/GenerateDataExportPackageCommand.cs` + handler — zips user data from all modules; uploads to MinIO; sends download email via `IEmailService`
- [ ] `Application/Commands/RequestDeletionCommand.cs` + handler — checks legal holds, active deals, retention periods; blocks if any hold active
- [ ] `Application/Commands/ApplyLegalHoldCommand.cs` + handler
- [ ] `Application/Commands/ReleaseLegalHoldCommand.cs` + handler
- [ ] `Application/Commands/AnonymizeInactiveUserCommand.cs` + handler — 2-year inactivity threshold
- [ ] `Application/Queries/GetUserConsentsQuery.cs`, `GetDataExportStatusQuery.cs`, `ListActiveLegalHoldsQuery.cs` + handlers
- [ ] `Application/DTOs/ConsentDto.cs`, `LegalHoldDto.cs`, `DeletionRequestDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/PrivacyEndpoints.cs`
- [ ] `Presentation/Contracts/ConsentRequest.cs`, `DataExportRequest.cs`, `DeletionRequest.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/PrivacyDbContext.cs` — schema `privacy`
- [ ] `Infrastructure/Persistence/Schemas/privacy.schema.sql`
- [ ] `Infrastructure/Repositories/ConsentRepository.cs`, `LegalHoldRepository.cs`
- [ ] `Infrastructure/Configurations/ConsentConfiguration.cs`
- [ ] `Infrastructure/Jobs/RetentionEnforcementJob.cs` — nightly: anonymize 2-year inactive profiles; delete 30-day pre-activation cancelled data
- [ ] `Infrastructure/Jobs/DataExportPurgeJob.cs` — daily: removes export packages from MinIO after 48h or download
- [ ] EF Core migrations

#### Module Registration
- [ ] `PrivacyModuleRegistration.cs`

---

### 5.13 AntiAbuseAndIntegrity

#### Project & References
- [ ] `AntiAbuseAndIntegrity.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/AbuseCase.cs` — `SubjectUserId`, `AbuseType`, `Status`, `DetectedAt`, `ResolvedAt`
- [ ] `Domain/Entities/CollusionPattern.cs` — repeated deal creation/closure between same parties
- [ ] `Domain/Entities/FraudFlag.cs` — `UserId`, `FlagType` (SyntheticId/ContactInfoBypass/TrustLedgerGaming/BadFaithReporting), `Severity` (High/Medium/Low), `FlaggedAt`
- [ ] `Domain/Entities/AccountRestriction.cs` — `UserId`, `RestrictionLevel` (Limited/Suspended/Banned), `AppliedAt`, `Reason`
- [ ] `Domain/ValueObjects/AbuseType.cs` — enum: Collusion, InquiryAbuse, TrustLedgerGaming, BadFaithReporting, SyntheticIdentity, AffiliationFraud
- [ ] Domain events: `CollusionDetectedEvent`, `InquiryAbuseDetectedEvent`, `TrustLedgerGamingDetectedEvent`, `AccountRestrictionAppliedEvent`

#### Application
- [ ] `Application/Commands/DetectCollusionCommand.cs` + handler — pattern: repeated deal pairs
- [ ] `Application/Commands/DetectInquiryAbuseCommand.cs` + handler — systematic rejection patterns; contact-info bypass
- [ ] `Application/Commands/DetectTrustLedgerGamingCommand.cs` + handler — landlord false violation reporting
- [ ] `Application/Commands/RaiseAbuseFlagCommand.cs` + handler
- [ ] `Application/Commands/ApplyAccountRestrictionCommand.cs` + handler — sends notification email via `IEmailService`
- [ ] `Application/Commands/SuspendAccountCommand.cs` + handler
- [ ] `Application/Queries/GetAbuseFlagsQuery.cs`, `GetUserRestrictionsQuery.cs` + handlers
- [ ] `Application/DTOs/AbuseFlagDto.cs`, `AccountRestrictionDto.cs`

#### Presentation
- [ ] `Presentation/Endpoints/IntegrityEndpoints.cs`
- [ ] `Presentation/Contracts/AbuseFlagResponse.cs`

#### Infrastructure
- [ ] `Infrastructure/Persistence/IntegrityDbContext.cs` — schema `integrity`
- [ ] `Infrastructure/Persistence/Schemas/integrity.schema.sql`
- [ ] `Infrastructure/Repositories/AbuseCaseRepository.cs`, `FraudFlagRepository.cs`
- [ ] `Infrastructure/Configurations/AbuseCaseConfiguration.cs`
- [ ] `Infrastructure/Jobs/PatternDetectionSchedulerJob.cs` — every 4h: collusion scan, gaming scan, bad-faith landlord scan
- [ ] EF Core migrations

#### Module Registration
- [ ] `AntiAbuseAndIntegrityModuleRegistration.cs`

---

### 5.14 ContentManagement

> Manages blog posts and static SEO pages. Exposes **public, unauthenticated read endpoints** consumed by `apps/marketing` (Next.js) and **admin-only write endpoints** consumed by `apps/admin`. Content is stored as Markdown in the database; the marketing site is responsible for rendering.

#### Project & References
- [ ] `ContentManagement.csproj`
- [ ] Add to `.sln`

#### Domain
- [ ] `Domain/Aggregates/BlogPost.cs` — `BlogPostId` (Guid), `Slug` (unique, URL-safe), `Title`, `Excerpt`, `Content` (Markdown string), `Status` (Draft/Published/Archived), `PublishedAt` (nullable), `AuthorUserId`, `Tags` (string[]), `MetaTitle`, `MetaDescription`, `OgImageUrl`, `ReadingTimeMinutes`
- [ ] `Domain/Entities/SeoPage.cs` — `SeoPageId`, `Slug` (unique), `Title`, `MetaTitle`, `MetaDescription`, `OgImageUrl`, `CanonicalUrl`, `NoIndex` (bool), `UpdatedAt`
- [ ] `Domain/ValueObjects/BlogStatus.cs` — `Draft | Published | Archived`
- [ ] Domain events: `BlogPostPublishedEvent`, `BlogPostArchivedEvent`
- [ ] Domain rule: `Slug` must be lowercase, hyphen-separated, unique across all blog posts; validated on creation and update
- [ ] Domain rule: `PublishedAt` is set to UTC now on first publish and is immutable thereafter

#### Application
- [ ] `Application/Commands/CreateBlogPostCommand.cs` + handler — admin only; generates slug from title if not provided; enforces uniqueness
- [ ] `Application/Commands/UpdateBlogPostCommand.cs` + handler — admin only; slug change forbidden after publish
- [ ] `Application/Commands/PublishBlogPostCommand.cs` + handler — transitions Draft → Published; sets `PublishedAt`
- [ ] `Application/Commands/ArchiveBlogPostCommand.cs` + handler — transitions Published → Archived
- [ ] `Application/Commands/UpsertSeoPageCommand.cs` + handler — admin only; create or update SEO page by slug
- [ ] `Application/Queries/GetPublishedBlogPostsQuery.cs` + handler — public; paginated list; filter by tag; returns summary DTOs (no full content)
- [ ] `Application/Queries/GetBlogPostBySlugQuery.cs` + handler — public; returns full content; 404 if not Published
- [ ] `Application/Queries/GetSeoPageBySlugQuery.cs` + handler — public; returns meta fields for a static page
- [ ] `Application/Queries/GetAllBlogPostsAdminQuery.cs` + handler — admin only; all statuses; paginated
- [ ] `Application/Queries/GetSitemapEntriesQuery.cs` + handler — public; returns all published post slugs + publishedAt for sitemap generation
- [ ] `Application/DTOs/BlogPostSummaryDto.cs` — id, slug, title, excerpt, tags, publishedAt, readingTimeMinutes, ogImageUrl
- [ ] `Application/DTOs/BlogPostDetailDto.cs` — all fields including full Markdown content
- [ ] `Application/DTOs/SeoPageDto.cs`
- [ ] `Application/DTOs/SitemapEntryDto.cs` — slug, publishedAt, changeFreq, priority

#### Presentation
- [ ] `Presentation/Endpoints/BlogEndpoints.cs` — public:
  - `GET /api/v1/blog` — paginated list (query: `page`, `pageSize`, `tag`)
  - `GET /api/v1/blog/{slug}` — single post by slug
  - `GET /api/v1/blog/sitemap` — sitemap entries (called by Next.js `sitemap.ts`)
- [ ] `Presentation/Endpoints/SeoPageEndpoints.cs` — public:
  - `GET /api/v1/pages/{slug}` — SEO page meta by slug
- [ ] `Presentation/Endpoints/AdminBlogEndpoints.cs` — all require `PlatformAdmin` role:
  - `POST /api/v1/admin/blog` — create draft
  - `PUT /api/v1/admin/blog/{id}` — update draft
  - `POST /api/v1/admin/blog/{id}/publish`
  - `POST /api/v1/admin/blog/{id}/archive`
  - `GET /api/v1/admin/blog` — all posts (admin view)
  - `PUT /api/v1/admin/pages/{slug}` — upsert SEO page

#### Infrastructure
- [ ] `Infrastructure/Persistence/ContentDbContext.cs` — schema `content`
- [ ] `Infrastructure/Persistence/Schemas/content.schema.sql`
- [ ] `Infrastructure/Repositories/BlogPostRepository.cs`, `SeoPageRepository.cs`
- [ ] `Infrastructure/Configurations/BlogPostConfiguration.cs` — `tags` stored as PostgreSQL text array (`character varying[]`)
- [ ] EF Core migrations

#### Module Registration
- [ ] `ContentManagementModuleRegistration.cs`

---

## Phase 6 — API Gateway (`Lagedra.ApiGateway`)

### 6.1 Project Setup

- [ ] Update `Lagedra.ApiGateway.csproj` — reference `Lagedra.Auth` + all 13 modules + `Lagedra.TruthSurface` + `Lagedra.Compliance` + `Lagedra.Infrastructure`
- [ ] Configure `Program.cs` — register all modules, middleware pipeline, OpenAPI, Swagger, API versioning

### 6.2 Middleware

- [ ] `Middleware/AuthMiddleware.cs` — validate JWT Bearer token; extract `UserId`, `Role` into `HttpContext`; enforce `IsActive=true`
- [ ] `Middleware/ConsentMiddleware.cs` — verify `KYCConsent` + `DataProcessing` consents exist for data-processing endpoints
- [ ] `Middleware/IdempotencyMiddleware.cs` — `Idempotency-Key` header; cache response in memory/DB for 24h
- [ ] `Middleware/RateLimitingMiddleware.cs` — per-user monthly dispute cap (beta); use `System.Threading.RateLimiting`
- [ ] `Middleware/CorrelationIdMiddleware.cs` — injects `X-Correlation-Id`; added to Serilog enrichment

### 6.3 Controllers / Endpoints

- [ ] `Controllers/V1/AuthController.cs` — proxies to `Lagedra.Auth` (or endpoints directly in Auth)
- [ ] `Controllers/V1/ListingsController.cs` — Create, Update, Publish, Search, GetDetails
- [ ] `Controllers/V1/ApplicationsController.cs` — SubmitApplication, GetApplication, UpdateStatus (approve/reject)
- [ ] `Controllers/V1/InquiryController.cs` — RequestUnlock, ApproveUnlock, SubmitQuestion, SubmitResponse
- [ ] `Controllers/V1/TruthSurfaceController.cs` — CreateSnapshot, ConfirmSnapshot, VerifySnapshot, GetSnapshot
- [ ] `Controllers/V1/ActivationController.cs` — ActivateDeal, GetActivationStatus
- [ ] `Controllers/V1/BillingController.cs` — GetBillingStatus, GetProrationQuote
- [ ] `Controllers/V1/ComplianceController.cs` — GetDealCompliance, ListViolations
- [ ] `Controllers/V1/ArbitrationController.cs` — FileCase, AttachEvidence, MarkEvidenceComplete, GetCase, ListCases
- [ ] `Controllers/V1/ArbitratorController.cs` — AssignArbitrator, IssueDecision, IssueAward
- [ ] `Controllers/V1/TrustLedgerController.cs` — GetLedgerForUser, GetFullLedgerForDeal
- [ ] `Controllers/V1/EvidenceController.cs` — RequestUploadUrl, CompleteUpload, GetManifest
- [ ] `Controllers/V1/InsuranceController.cs` — GetStatus, UploadManualProof, GetPartnerQuotes
- [ ] `Controllers/V1/IdentityController.cs` — StartKyc, GetVerificationStatus
- [ ] `Controllers/V1/RiskController.cs` — GetRiskViewForLandlord
- [ ] `Controllers/V1/PrivacyController.cs` — RecordConsent, RequestDataExport, RequestDeletion
- [ ] `Controllers/V1/NotificationsController.cs` — GetHistory, UpdatePreferences
- [ ] `Controllers/V1/WebhooksController.cs` — Stripe webhook, Persona webhook, insurance partner webhook
- [ ] `Controllers/Admin/InsuranceOpsController.cs` — insurance unknown queue, manual verification
- [ ] `Controllers/Admin/FraudOpsController.cs` — fraud flags, account restrictions
- [ ] `Controllers/Admin/ArbitrationOpsController.cs` — backlog management, arbitrator panel management
- [ ] `Controllers/Admin/JurisdictionOpsController.cs` — pack version management, dual-control approvals
- [ ] `Controllers/Admin/AuditController.cs` — audit event search

### 6.4 Authentication & Authorization

- [ ] ASP.NET Identity + JWT Bearer configured in `Lagedra.Auth`; API Gateway adds `[Authorize]` + policy enforcement
- [ ] Authorization policies: `RequireLandlord`, `RequireTenant`, `RequireArbitrator`, `RequirePlatformAdmin`, `RequireInsurancePartner`, `RequireInstitutionPartner`
- [ ] Stripe webhook endpoint: no auth; validates `Stripe-Signature` header via `StripeService`
- [ ] Persona webhook endpoint: no auth; validates `Persona-Signature` header via `PersonaClient`

### 6.5 API Configuration

- [ ] API versioning via `Asp.Versioning.Mvc` — route prefix `v1`
- [ ] OpenAPI / Swagger with JWT security scheme
- [ ] `tools/openapi/lagedra.openapi.yaml` — generated from Swagger
- [ ] `tools/postman/Lagedra.postman_collection.json`
- [ ] `tools/postman/Lagedra.postman_environment.local.json`
- [ ] Global error handling — `ProblemDetails` (RFC 7807) via `app.UseExceptionHandler`
- [ ] FluentValidation integration — `AddFluentValidationAutoValidation()`; validators for all request models
- [ ] CORS policy — allow `apps/web` and `apps/admin` origins

---

## Phase 7 — Background Worker (`Lagedra.Worker`)

### 7.1 Project Setup

- [ ] Create `src/Lagedra.Worker/Lagedra.Worker.csproj` (Worker SDK)
- [ ] Reference `Lagedra.Infrastructure` + all 13 modules + `Lagedra.Auth` + `Lagedra.TruthSurface` + `Lagedra.Compliance`
- [ ] Add to `.sln`

### 7.2 Scheduling

- [ ] `Scheduling/QuartzSetup.cs` — configure Quartz.NET; persistent job store: Npgsql Quartz job store (schema `quartz`)
- [ ] `Scheduling/JobRegistry.cs` — register all jobs with cron expressions

### 7.3 Orchestrators

- [ ] `Orchestration/OutboxDispatchOrchestrator.cs` — polls `outbox_messages`, dispatches via `IEventBus`, marks processed
- [ ] `Orchestration/ModuleJobOrchestrator.cs` — coordinates cross-module job sequencing
- [ ] `Orchestration/HealthOrchestrator.cs` — liveness probe; alerts ops via email on critical job failure

### 7.4 All Registered Background Jobs

- [ ] `RefreshTokenCleanupJob` (Auth) — nightly
- [ ] `BillingReconciliationJob` (ActivationAndBilling) — daily
- [ ] `InsurancePollerJob` (InsuranceIntegration) — hourly
- [ ] `InsuranceUnknownSlaJob` (InsuranceIntegration) — every 30 min
- [ ] `FraudFlagSlaMonitorJob` (IdentityAndVerification) — every 15 min
- [ ] `ComplianceScannerJob` (ComplianceMonitoring) — every 6h
- [ ] `ArbitrationBacklogSlaJob` (Arbitration) — hourly
- [ ] `PackEffectiveDateActivationJob` (JurisdictionPacks) — daily at midnight
- [ ] `MalwareScanPollingJob` (Evidence) — every 5 min
- [ ] `EvidenceRetentionJob` (Evidence) — nightly
- [ ] `NotificationRetryJob` (Notifications) — every 10 min
- [ ] `RetentionEnforcementJob` (Privacy) — nightly
- [ ] `DataExportPurgeJob` (Privacy) — daily
- [ ] `PatternDetectionSchedulerJob` (AntiAbuseAndIntegrity) — every 4h
- [ ] `JurisdictionResolutionJob` (ListingAndLocation) — nightly sweep
- [ ] `InquiryIntegrityScanJob` (StructuredInquiry) — daily
- [ ] `SnapshotVerificationJob` (TruthSurface) — weekly

### 7.5 Program.cs

- [ ] Host builder with all module DI registrations
- [ ] Serilog structured logging
- [ ] Health check endpoint (`/healthz`)
- [ ] Graceful shutdown with Quartz job drain

---

## Phase 8 — Web Frontend (`apps/web`)

### 8.1 Project Setup

- [ ] Move `src/Lagedra.Web/` → `apps/web/`; update `docker-compose.yml`
- [ ] `pnpm` workspace member
- [ ] `vite.config.ts`: path alias `@`, proxy `/api` → API gateway, `@react-google-maps/api` loaded via browser API key
- [ ] `tsconfig.json`: strict mode, path aliases
- [ ] ESLint (eslint-config-react-app or custom) + Prettier
- [ ] Install: `react-router-dom`, `@tanstack/react-query`, `zustand`, `axios`
- [ ] Install: `tailwindcss`, `@tailwindcss/forms`, `@tailwindcss/typography`, `shadcn/ui` (CLI init)
- [ ] Install: `@react-google-maps/api` — Google Maps JavaScript API wrapper
- [ ] Install: `@stripe/stripe-js`, `@stripe/react-stripe-js` — Stripe Elements
- [ ] Install: `react-hook-form`, `zod`, `@hookform/resolvers` — form validation
- [ ] `.env.example`: `VITE_API_BASE_URL`, `VITE_GOOGLE_MAPS_API_KEY`, `VITE_STRIPE_PUBLISHABLE_KEY`

### 8.2 App Shell

- [ ] `src/main.tsx` — providers: `QueryClientProvider`, `AuthProvider`, `RouterProvider`
- [ ] `src/app/App.tsx` — router outlet, global error boundary
- [ ] `src/app/routes.tsx` — role-based lazy routes
- [ ] `src/app/auth/AuthProvider.tsx` — JWT storage (`localStorage`), refresh logic, Zustand slice
- [ ] `src/app/auth/RequireAuth.tsx` — redirects to login if no valid token
- [ ] `src/app/auth/roles.ts` — role constants matching backend enums
- [ ] `src/app/layout/Shell.tsx`, `Nav.tsx`, `Footer.tsx`, `ErrorBoundary.tsx`
- [ ] `src/app/config.ts` — reads `VITE_*` env vars

### 8.3 API Client

- [ ] `src/api/http.ts` — Axios instance: `Authorization: Bearer`, `X-Correlation-Id`, auto-refresh on 401
- [ ] `src/api/endpoints.ts` — typed endpoint map
- [ ] `src/api/types.ts` — DTOs synced with `packages/contracts`

### 8.4 UI Primitives (shadcn/ui base)

- [ ] shadcn/ui components initialized: `Button`, `Card`, `Modal/Dialog`, `Table`, `Input`, `Select`, `Checkbox`, `RadioGroup`, `Badge`, `Alert`, `Skeleton`, `Pagination`, `Separator`, `Tabs`
- [ ] `src/components/shared/Loader.tsx`
- [ ] `src/components/shared/EmptyState.tsx`
- [ ] `src/components/shared/FormError.tsx`

### 8.5 Feature Modules

#### Auth Pages
- [ ] `features/auth/pages/LoginPage.tsx` — email + password form; `POST /v1/auth/login`
- [ ] `features/auth/pages/RegisterPage.tsx` — email + password + role selection (Landlord/Tenant only)
- [ ] `features/auth/pages/VerifyEmailPage.tsx` — token from URL param
- [ ] `features/auth/pages/ForgotPasswordPage.tsx`
- [ ] `features/auth/pages/ResetPasswordPage.tsx`
- [ ] `features/auth/services/authApi.ts`

#### Listings
- [ ] `features/listings/pages/SearchPage.tsx` — filter by stay range, price, approx location; minimal non-promotional
- [ ] `features/listings/pages/ListingDetailPage.tsx` — structured fields only; Google Map with approx pin pre-activation; precise address post-activation
- [ ] `features/listings/pages/CreateListingPage.tsx` — fully structured form; jurisdiction-gated fields (AB 628 stove/fridge checkboxes for CA)
- [ ] `features/listings/components/ListingCard.tsx`
- [ ] `features/listings/components/ListingForm.tsx` — uses `react-hook-form` + `zod`; field visibility driven by jurisdiction
- [ ] `features/listings/components/LocationPicker.tsx` — `@react-google-maps/api` `GoogleMap` + `Marker`; approx vs. precise state toggle
- [ ] `features/listings/hooks/useListings.ts`, `useListingDetail.ts` — TanStack Query
- [ ] `features/listings/services/listingApi.ts`

#### Applications
- [ ] `features/applications/pages/ApplicationsPage.tsx` — landlord inbox; shows tenant's Verification Class, insurance state, deposit band
- [ ] `features/applications/pages/ApplicationDetailPage.tsx` — full risk view; approve/reject actions
- [ ] `features/applications/components/ApplicationCard.tsx`
- [ ] `features/applications/components/ApplicationForm.tsx` — Persona KYC consent, document upload, affiliation declaration, military status checkbox
- [ ] `features/applications/services/applicationApi.ts`

#### Structured Inquiry
- [ ] `features/inquiry/pages/InquiryThreadPage.tsx` — question/answer history; hard-disabled UI after Truth Surface confirmation
- [ ] `features/inquiry/components/InquiryQuestion.tsx` — predefined question dropdown only
- [ ] `features/inquiry/components/InquiryResponseForm.tsx` — structured Yes/No / multi-choice / numeric
- [ ] `features/inquiry/services/inquiryApi.ts`

#### Truth Surface
- [ ] `features/truth-surface/pages/TruthSurfaceConfirmationPage.tsx` — line-by-line display; per-line confirm checkboxes; Platform Disclaimers acknowledgement (required before submit)
- [ ] `features/truth-surface/components/TruthSnapshotViewer.tsx` — immutable snapshot; cryptographic proof display
- [ ] `features/truth-surface/components/ConfirmButton.tsx` — disabled until all checkboxes ticked
- [ ] Hard-coded system notice: "The Inquiry Service is now closed. All confirmed details are recorded in the Truth Surface" — displayed in-page after confirmation
- [ ] `features/truth-surface/services/truthSurfaceApi.ts`

#### Activation & Billing
- [ ] `features/activation-billing/pages/BillingPage.tsx` — current invoice, prorated amount, Stripe Elements payment method card
- [ ] `features/activation-billing/pages/PaymentMethodPage.tsx` — `@stripe/react-stripe-js` `CardElement` or `PaymentElement`
- [ ] Non-custodial disclaimer displayed: "Lagedra does not receive, transmit, or hold these funds. These instructions are provided solely to facilitate your direct settlement."
- [ ] `features/activation-billing/services/billingApi.ts`

#### Compliance
- [ ] `features/compliance/pages/ComplianceStatusPage.tsx` — violations list, cure windows, insurance lapse alerts

#### Arbitration
- [ ] `features/arbitration/pages/CaseListPage.tsx` — case list with status/tier
- [ ] `features/arbitration/pages/CaseDetailPage.tsx` — evidence slots, timeline, decision display
- [ ] `features/arbitration/components/EvidenceUpload.tsx` — structured slot upload; calls presigned URL; file hash displayed
- [ ] `features/arbitration/components/CaseTimeline.tsx`
- [ ] `features/arbitration/services/arbitrationApi.ts`

#### Trust Ledger
- [ ] `features/trust-ledger/pages/TrustLedgerPage.tsx` — public pseudonymized view; full detail for involved parties only
- [ ] `features/trust-ledger/components/LedgerEntryList.tsx`

#### Evidence
- [ ] `features/evidence/services/evidenceApi.ts` — presigned URL request, upload completion

#### Notifications (In-App)
- [ ] `features/notifications/pages/NotificationsPage.tsx` — in-app notification history (email sent = shown here too)
- [ ] `features/notifications/services/notificationApi.ts`

#### Profile
- [ ] `features/profile/pages/ProfilePage.tsx` — identity status, affiliation, insurance status, Verification Class
- [ ] `features/profile/pages/VerificationStatusPage.tsx` — Persona KYC progress, background check status
- [ ] `features/profile/services/profileApi.ts`

### 8.6 Utils

- [ ] `src/utils/format.ts` — date (US locale), money (cents → `$XX.XX`), percentage formatters
- [ ] `src/utils/validation.ts` — shared `zod` schemas (email, password, stay range)

---

## Phase 9 — Admin App (`apps/admin`)

### 9.1 Project Setup

- [ ] Create `apps/admin/` with Vite + React + TypeScript + Tailwind + shadcn/ui + Zustand
- [ ] Separate auth context (admin role only: `PlatformAdmin`)
- [ ] `apps/admin/package.json`, `vite.config.ts`, `tsconfig.json`, `.env.example`
- [ ] Install same core packages as `apps/web` (excluding Google Maps and Stripe Elements)

### 9.2 Admin Pages (Ops Dashboard)

- [ ] `pages/InsuranceUnknownQueue.tsx` — deals in "Status: Unknown"; manual verification portal; 24h SLA countdown indicator
- [ ] `pages/FraudFlags.tsx` — fraud flags by severity (High/Medium/Low); review/resolve workflow; 24h/72h SLA display
- [ ] `pages/ArbitrationBacklog.tsx` — caseload per arbitrator; SLA status; triage view; overflow assignment button
- [ ] `pages/JurisdictionPackVersions.tsx` — draft/pending/active packs; dual-control approval workflow (2nd approver view)
- [ ] `pages/EvidenceReview.tsx` — malware scan queue; infected file quarantine; manual evidence review
- [ ] `pages/AuditSearch.tsx` — full audit event log; filter by user, event type, date range
- [ ] `pages/ManualVerification.tsx` — Persona KYC manual review fallback queue; ≤ 24h SLA
- [ ] `pages/ComplianceViolations.tsx` — all violations across all deals; filter by category/status
- [ ] `pages/UserRestrictions.tsx` — account restrictions/suspensions/bans; manage restrictions
- [ ] `pages/DualControlApprovals.tsx` — pending pack approvals requiring 2nd approver
- [ ] `pages/BlogPosts.tsx` — full CRUD for blog posts; status badge (Draft/Published/Archived); publish/archive actions; Markdown preview pane (react-markdown)
- [ ] `pages/BlogPostEditor.tsx` — rich Markdown editor (`@uiw/react-md-editor` or plain `<textarea>`); slug preview; meta fields (metaTitle, metaDescription, ogImageUrl); tag input; estimated reading time display
- [ ] `pages/SeoPages.tsx` — list and edit static SEO page meta (e.g. `/how-it-works`, `/pricing`, `/about`); noIndex toggle

### 9.3 Admin API Client

- [ ] `api/http.ts`, `api/endpoints.ts`, `api/types.ts` — admin-scoped; all admin endpoints require `PlatformAdmin` role

---

## Phase 10 — Shared Packages (`packages/`)

### 10.1 UI Component Library (`packages/ui`)

- [ ] `packages/ui/package.json`
- [ ] Re-exports from shadcn/ui + custom overrides: `Button`, `Input`, `Modal`, `Select`, `Checkbox`, `RadioGroup`, `DatePicker`, `Badge`, `Alert`
- [ ] `src/index.ts` — barrel export

### 10.2 Shared Contracts (`packages/contracts`)

- [ ] `packages/contracts/package.json`
- [ ] `src/enums.ts` — `VerificationClass`, `InsuranceState`, `ViolationCategory`, `DealStatus`, `ArbitrationTier`, `NotificationType`, `UserRole`, `ConsentType`
- [ ] `src/dtos.ts` — all DTO types (synced manually with backend, or generated via NSwag)
- [ ] `src/events.ts` — domain event type definitions for any future cross-app usage
- [ ] `src/index.ts`

### 10.3 Test Utilities (`packages/test-utils`)

- [ ] `packages/test-utils/package.json`
- [ ] `src/renderWithProviders.tsx` — React Testing Library wrapper with all providers (QueryClient, Router, Auth)
- [ ] `src/mockServer.ts` — MSW handler setup for all API endpoints
- [ ] `src/index.ts`

---

## Phase 10.5 — Marketing Site (`apps/marketing`)

> Public-facing, SEO-first website built with **Next.js 15 (App Router)**. Handles the landing page, blog, static content pages, and all SEO infrastructure. The authenticated product lives in `apps/web`; this app is entirely unauthenticated and optimized for search indexing, Core Web Vitals, and content delivery.
>
> Architecture principle: `apps/marketing` is a **standalone Next.js app** — it calls the public, unauthenticated read endpoints of the `ContentManagement` module for blog data, and has its own `next.config.ts`, `Dockerfile`, and Nginx routing (`/blog`, `/how-it-works`, etc.).

### 10.5.1 Project Setup

- [ ] Bootstrap `apps/marketing/` with Next.js 15 App Router + TypeScript: `npx create-next-app@latest marketing --ts --tailwind --eslint --app --src-dir`
- [ ] `apps/marketing/package.json` — workspace member
- [ ] `apps/marketing/next.config.ts` — `output: 'standalone'` for Docker; `NEXT_PUBLIC_API_URL` env var; image domains whitelist (for OG images)
- [ ] `apps/marketing/tsconfig.json`
- [ ] `apps/marketing/.env.example` — `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_SITE_URL`, `NEXT_PUBLIC_GA_MEASUREMENT_ID`
- [ ] Install additional packages: `react-markdown`, `remark-gfm`, `rehype-highlight` (Markdown rendering), `@vercel/og` or `next/og` (OG image generation)
- [ ] Configure Tailwind CSS + shadcn/ui (`npx shadcn@latest init`) — same design system as `apps/web`
- [ ] `apps/marketing/src/lib/api.ts` — typed fetch wrapper calling `NEXT_PUBLIC_API_URL`

### 10.5.2 Layout & Navigation

- [ ] `src/app/layout.tsx` — root layout with `<html lang="en">`; global `<GoogleTagManager>` (or GA script); default `metadata` export
- [ ] `src/components/Header.tsx` — top nav: logo, How It Works, Pricing, Blog, FAQ, "Get Started" CTA (links to `apps/web`)
- [ ] `src/components/Footer.tsx` — links, legal, social, copyright
- [ ] `src/components/MobileNav.tsx` — hamburger menu for mobile

### 10.5.3 Pages

- [ ] `src/app/page.tsx` — **Home / Landing Page**
  - Hero: value proposition headline, CTA ("Protect Your Rental")
  - How it works (3-step explainer)
  - Trust signals / stats
  - Feature highlights (Trust Ledger, Verification Class, Arbitration)
  - FAQ accordion
  - Footer CTA
  - Full static generation (`export const dynamic = 'force-static'`)
- [ ] `src/app/how-it-works/page.tsx` — detailed product walkthrough; static
- [ ] `src/app/pricing/page.tsx` — protocol fee breakdown; static
- [ ] `src/app/about/page.tsx` — mission, team, founding story; static
- [ ] `src/app/contact/page.tsx` — contact form (sends to `IEmailService` via a `/api/contact` Next.js Route Handler)
- [ ] `src/app/faq/page.tsx` — accordion FAQ; static
- [ ] `src/app/legal/terms/page.tsx` — Terms of Service; static Markdown render
- [ ] `src/app/legal/privacy/page.tsx` — Privacy Policy; static Markdown render

### 10.5.4 Blog

- [ ] `src/app/blog/page.tsx` — **Blog List Page**
  - `fetch` from `GET /api/v1/blog` (with `next: { revalidate: 300 }` — ISR every 5 min)
  - Pagination, tag filter
  - Post card: title, excerpt, tags, publishedAt, reading time, OG image thumbnail
- [ ] `src/app/blog/[slug]/page.tsx` — **Blog Post Detail Page**
  - `fetch` from `GET /api/v1/blog/{slug}` (ISR; `revalidate: 300`)
  - `generateStaticParams()` — pre-renders all published posts at build time
  - `generateMetadata()` — dynamic `title`, `description`, `openGraph` from post meta fields
  - Renders `content` (Markdown) via `react-markdown` + `remark-gfm` + `rehype-highlight`
  - Structured data: JSON-LD `Article` schema (author, publishedAt, headline, image)
  - Related posts section (same tags)
- [ ] `src/app/blog/tag/[tag]/page.tsx` — **Tag Filter Page**; `generateStaticParams()` for all known tags

### 10.5.5 SEO Infrastructure

- [ ] `src/app/sitemap.ts` — **Dynamic Sitemap** (Next.js App Router built-in):
  - Static pages: `/`, `/how-it-works`, `/pricing`, `/about`, `/faq`, `/contact`, `/blog`
  - Dynamic blog posts: fetched from `GET /api/v1/blog/sitemap` at build/revalidation time
  - Outputs `<url>`, `<loc>`, `<lastmod>`, `<changefreq>`, `<priority>` for each
- [ ] `src/app/robots.ts` — **Robots.txt** (Next.js built-in): `User-agent: *`, `Allow: /`, `Disallow: /api/`, `Sitemap: https://lagedra.com/sitemap.xml`
- [ ] `src/app/og/route.tsx` — **OG Image Route Handler** (Next.js `ImageResponse`): generates dynamic OG images for blog posts from title + slug params
- [ ] `src/app/rss.xml/route.ts` — **RSS Feed Route Handler**: fetches all published posts; returns `application/rss+xml` with proper `<channel>` and `<item>` entries
- [ ] `src/app/api/contact/route.ts` — **Contact Form API Route**: validates body (zod), calls `POST /api/v1/contact` on backend (which uses `IEmailService`); rate-limited
- [ ] Default `metadata` in `layout.tsx`: `title.template`, `description`, `openGraph` (site name, type, locale), `twitter` (card type)
- [ ] JSON-LD Organization schema on homepage (`<script type="application/ld+json">`)
- [ ] Canonical URL set on every page via `alternates.canonical` in metadata

### 10.5.6 Performance & Core Web Vitals

- [ ] All images via `next/image` with explicit `width`/`height` or `fill` — prevents CLS
- [ ] Fonts via `next/font/google` — eliminates FOIT/FOUT, self-hosted at build time
- [ ] No layout shift from dynamic content: skeleton loaders or static fallbacks
- [ ] `next.config.ts`: `compress: true`, bundle analyzer script (`@next/bundle-analyzer`)

### 10.5.7 Dockerfile & Nginx Routing

- [ ] `apps/marketing/Dockerfile` — multi-stage: `node:20-alpine` build + standalone output copy
- [ ] Nginx `deploy/nginx/nginx.conf` — route `/blog`, `/how-it-works`, `/pricing`, `/about`, `/contact`, `/faq`, `/legal` → marketing service (port 3001); all other non-API routes → web app (port 3000); `/api` → backend (port 5000)

---

## Phase 11 — Testing

### 11.1 Architecture Tests (`tests/Lagedra.Tests.Architecture`)

- [ ] `Lagedra.Tests.Architecture.csproj` — `NetArchTest.Rules`; references all module + gateway projects
- [ ] `ModuleDependencyTests.cs` — no module references another module directly (only SharedKernel, Infrastructure, TruthSurface, Compliance, JurisdictionPacks)
- [ ] `DomainLayerTests.cs` — Domain has no reference to Infrastructure, EF Core, or HTTP
- [ ] `ApplicationLayerTests.cs` — Application has no reference to Infrastructure or EF Core
- [ ] `NamingConventionTests.cs` — aggregate roots extend `AggregateRoot<>`, value objects extend `ValueObject`, events implement `IDomainEvent`
- [ ] `ApiGatewayHasNoBusinessLogicTests.cs` — controllers only inject `IMediator`; no direct domain or repository injection
- [ ] `NoProtectedClassAttributesInRiskPolicyTests.cs` — `VerificationClassPolicy` references no protected-class attribute names

### 11.2 Unit Tests (`tests/Lagedra.Tests.Unit`)

- [ ] `Lagedra.Tests.Unit.csproj` — xUnit, FluentAssertions, NSubstitute, Bogus
- [ ] Auth: `JwtTokenServiceTests.cs`, `RefreshTokenServiceTests.cs`, `RegisterUserTests.cs`
- [ ] SharedKernel: `AggregateRootTests.cs`, `ValueObjectTests.cs`, `ResultTests.cs`
- [ ] TruthSurface: `TruthSnapshotTests.cs`, `HashingTests.cs`, `MerkleTreeTests.cs`
- [ ] Compliance: `TrustLedgerEntryTests.cs` (append-only), `ViolationTests.cs`
- [ ] ActivationAndBilling: `BillingPolicyTests.cs` ($79 proration formula), `DealApplicationTests.cs`, `BillingAccountTests.cs`
- [ ] IdentityAndVerification: `VerificationStatusTests.cs`, `AffiliationVerificationTests.cs`
- [ ] InsuranceIntegration: `UnknownGraceWindowPolicyTests.cs` (72h, API failure vs. tenant inaction)
- [ ] ListingAndLocation: `StayRangeTests.cs` (30–180 validation), `ListingTests.cs`
- [ ] StructuredInquiry: `InquirySessionTests.cs` (lock on Truth Surface confirmation), `ContactInfoBypassDetectionTests.cs`
- [ ] VerificationAndRisk: `VerificationClassPolicyTests.cs` (all inputs, no protected-class), `DepositRecommendationPolicyTests.cs` (jurisdiction cap, adverse action)
- [ ] ComplianceMonitoring: `ViolationCategoryTests.cs` (A–G + Other mapping)
- [ ] Arbitration: `EvidenceMinimumThresholdPolicyTests.cs`, `ArbitrationCaseTests.cs` (SLA clock, cap enforcement)
- [ ] JurisdictionPacks: `FieldGatingRuleTests.cs`, `CaliforniaPackTests.cs` (AB 12, SB 611, AB 628, AB 2801, AB 414, JCO)
- [ ] Evidence: `EvidenceManifestTests.cs` (seal invariant), `FileHashTests.cs`
- [ ] Privacy: `DeletionRequestTests.cs` (legal hold blocking), `RetentionPeriodTests.cs`
- [ ] AntiAbuseAndIntegrity: `CollusionPatternTests.cs`, `InquiryAbuseDetectionTests.cs`
- [ ] ContentManagement: `BlogPostTests.cs` (slug uniqueness, Draft→Published transition, PublishedAt immutability), `SeoPageTests.cs`

### 11.3 Integration Tests (`tests/Lagedra.Tests.Integration`)

- [ ] `Lagedra.Tests.Integration.csproj` — `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing`; real PostgreSQL via Docker
- [ ] `AuthIntegrationTests.cs` — register → verify email → login → refresh → revoke
- [ ] `TruthSurfaceIntegrationTests.cs` — full snapshot confirmation, hash verification
- [ ] `BillingActivationIntegrationTests.cs` — application → approval → deal activation (Stripe mocked)
- [ ] `InsuranceIntegrationTests.cs` — API failure → Unknown → 72h → manual upload flow
- [ ] `ArbitrationIntegrationTests.cs` — full case filing through decision
- [ ] `PrivacyIntegrationTests.cs` — deletion request blocked by legal hold
- [ ] `JurisdictionPackIntegrationTests.cs` — LA pack field gating end-to-end
- [ ] `StripeWebhookIntegrationTests.cs` — payment succeeded / failed / chargeback webhook handling
- [ ] `PersonaWebhookIntegrationTests.cs` — KYC complete / fail / background check result
- [ ] `ContentManagementIntegrationTests.cs` — create draft → publish → public GET by slug → 404 for archived

### 11.4 Frontend Tests (`apps/web`)

- [ ] Vitest + React Testing Library configured
- [ ] MSW (`mockServiceWorker.js`) for API mocking
- [ ] Tests: `LoginPage.test.tsx`, `TruthSurfaceConfirmationPage.test.tsx`, `LocationPicker.test.tsx`, `BillingPolicy.test.ts` (proration formula)

### 11.5 Marketing Site Tests (`apps/marketing`)

- [ ] Vitest configured (Next.js compatible — `@vitejs/plugin-react`, jsdom)
- [ ] `BlogList.test.tsx` — renders post cards from mocked API response; pagination
- [ ] `BlogPostPage.test.tsx` — renders Markdown content; shows 404 for unknown slug (MSW mock)
- [ ] `Sitemap.test.ts` — sitemap entries include all published slugs; static pages always present
- [ ] `RssFeed.test.ts` — valid XML output; correct `<item>` count matches published posts

---

## Phase 12 — Documentation

### 12.1 Architecture Decision Records (`docs/decisions/`)

- [ ] `ADR-0001-modular-monolith.md` — why modular monolith over microservices at launch
- [ ] `ADR-0002-schema-per-module.md` — PostgreSQL schema-per-module isolation
- [ ] `ADR-0003-truth-surface-signing.md` — HMAC-SHA256 + canonical JSON + Merkle tree
- [ ] `ADR-0004-outbox-required.md` — Transactional Outbox for domain event reliability
- [ ] `ADR-0005-evidence-immutable-manifest.md` — sealed manifest + SHA-256 file hashing
- [ ] `ADR-0006-aspnet-identity.md` — ASP.NET Identity over Auth0 (cost, control, VPS-friendly)
- [ ] `ADR-0007-mailkit-brevo.md` — MailKit + Brevo over SendGrid (EU GDPR, cost, control)
- [ ] `ADR-0008-minio-clamav.md` — self-hosted MinIO + ClamAV for VPS deployment
- [ ] `ADR-0009-deal-in-activation.md` — Deal lifecycle owns in ActivationAndBilling module
- [ ] `ADR-0010-google-maps-persona-stripe.md` — confirmed third-party provider decisions
- [ ] `ADR-0011-nextjs-marketing-site.md` — why Next.js (App Router + ISR) for `apps/marketing` over Vite SPA (SEO, sitemap, OG images, Core Web Vitals)

### 12.2 Architecture Docs (`docs/architecture/`)

- [ ] `00-context.md` — system context, actors, external systems
- [ ] `01-modular-monolith.md` — module boundary rules, communication patterns, outbox
- [ ] `02-truth-surface.md` — cryptographic proof model, confirmation flow, inquiry lock
- [ ] `03-eventing-outbox.md` — domain event lifecycle, outbox processor, Quartz.NET
- [ ] `04-data-retention-privacy.md` — retention periods, anonymization, legal holds, GDPR/CCPA/FCRA
- [ ] `05-evidence-storage.md` — MinIO upload flow, ClamAV scan, metadata strip, sealing
- [ ] `06-arbitration-engine.md` — tier system, evidence schema, SLA, backlog controls
- [ ] `07-jurisdiction-packs.md` — versioning, dual-control, effective-date rules, LA v1
- [ ] `08-anti-abuse.md` — abuse detection patterns, Trust Ledger gaming, inquiry integrity
- [ ] `09-observability.md` — Serilog, OpenTelemetry, health checks
- [ ] `10-access-control-rbac.md` — ASP.NET Identity roles, JWT claims, permissions matrix

### 12.3 Runbooks (`docs/runbooks/`)

- [ ] `insurance-unknown-queue.md` — ops procedure, 24h SLA, escalation
- [ ] `arbitration-backlog-incident.md` — incident declaration, triage, overflow, daily reporting
- [ ] `fraud-flag-triage.md` — severity classification, 24h/72h SLA, Security + Legal escalation
- [ ] `pii-incident-response.md` — breach notification, regulatory reporting, user communication

---

## Phase 13 — Deployment (VPS + Docker Compose + Nginx)

### 13.1 Dockerfile

- [ ] Multi-stage `Dockerfile`:
  - Stage 1: `mcr.microsoft.com/dotnet/sdk:8.0` — `dotnet restore` + `dotnet publish -c Release`
  - Stage 2: `mcr.microsoft.com/dotnet/aspnet:8.0` — non-root user; copy published output
- [ ] `.dockerignore` — exclude `bin/`, `obj/`, `.git/`, `node_modules/`

### 13.2 docker-compose.yml (Primary Deployment)

- [ ] Services: `postgres`, `minio`, `clamav`, `api` (Lagedra.ApiGateway), `worker` (Lagedra.Worker), `web` (Nginx serving built React app), `admin` (Nginx serving built admin app), `marketing` (Next.js standalone — port 3001)
- [ ] `minio` service: `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, volume mount, console at `:9001`
- [ ] `clamav` service: `clamav/clamav:latest`, freshclam enabled, health check
- [ ] Environment variables via `.env` file (never commit secrets)
- [ ] Health check for all services

### 13.3 Nginx (`deploy/nginx/`)

- [ ] `nginx.conf` — upstream API gateway, upstream web, upstream admin; gzip; security headers (CSP, HSTS, X-Frame-Options)
- [ ] `sites-enabled/lagedra.conf` — TLS (Let's Encrypt / Certbot); reverse proxy `/api` → API gateway; `/` → web; `/admin` → admin app
- [ ] `tools/scripts/certbot-renew.sh` — renew Let's Encrypt certificates

### 13.4 CI/CD — GitHub Actions

- [ ] `.github/workflows/ci.yml` — on PR: `dotnet format --verify-no-changes` + `dotnet test` (all three test projects) + `pnpm lint` + `pnpm test`
- [ ] `.github/workflows/cd.yml` — on merge to `main`: `docker build` + `docker push` to GitHub Container Registry + SSH deploy to VPS via `appleboy/ssh-action`
- [ ] Architecture test gate: `Lagedra.Tests.Architecture` must pass before merge
- [ ] Secret scanning: GitHub's built-in secret scanning enabled on repo
- [ ] Container image scan: Trivy action on built image

### 13.5 Environment Files

- [ ] `deploy/env/local.env` — all vars documented with example values (no real secrets)
- [ ] `deploy/env/staging.env` — documented placeholders
- [ ] `deploy/env/prod.env` — documented placeholders
- [ ] Required env vars documented:
  - `POSTGRES_*` (host, user, password, db)
  - `JWT_SECRET_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`
  - `STRIPE_SECRET_KEY`, `STRIPE_PUBLISHABLE_KEY`, `STRIPE_WEBHOOK_SECRET`
  - `GOOGLE_MAPS_API_KEY`
  - `PERSONA_API_KEY`, `PERSONA_TEMPLATE_ID`, `PERSONA_WEBHOOK_SECRET`
  - `BREVO_SMTP_USERNAME`, `BREVO_SMTP_API_KEY`
  - `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, `MINIO_ENDPOINT`
  - `CLAMAV_HOST`, `CLAMAV_PORT`
  - `INSURANCE_API_KEY`, `INSURANCE_API_BASE_URL`

### 13.6 Optional Phase 2 — Kubernetes & Terraform

> Not required for v1 launch. Document as "Phase 2" when VPS outgrows capacity.

- [ ] `deploy/k8s/` — Kubernetes manifests (namespace, deployments, services, ingress, configmap, secrets template, PostgreSQL statefulset)
- [ ] `deploy/terraform/` — IaC modules for VPS provider / cloud migration

---

## Phase 14 — External Integrations

> All integrations read-only or webhook-based. Platform never holds/transmits/guarantees funds.

### 14.1 Stripe (Protocol Fee Billing)

- [ ] `Stripe.net` NuGet integrated in `Lagedra.Infrastructure`
- [ ] Stripe products/prices configured: "Protocol Fee - Standard" ($79/month), "Protocol Fee - Institutional Partner" ($39/month)
- [ ] `CreateStripeCustomerCommand` — creates Stripe Customer for landlord on first deal
- [ ] `ActivateDealCommand` — creates Stripe Subscription (prorated from activation date)
- [ ] `StopBillingCommand` — cancels Stripe Subscription at period end
- [ ] Webhook endpoint (no auth): validates `Stripe-Signature`; dispatches `invoice.paid`, `invoice.payment_failed`, `charge.dispute.created`
- [ ] Frontend: Stripe Elements (`PaymentElement`) for payment method capture; publishable key from `VITE_STRIPE_PUBLISHABLE_KEY`

### 14.2 Google Maps Platform (Geocoding + Maps)

- [ ] **Backend**: `GoogleMapsGeocodingService` — `HttpClient` calls to `https://maps.googleapis.com/maps/api/geocode/json`; parses address components for `city`, `county`, `state`, `country`; derives `JurisdictionCode` (e.g. `US-CA-LA`); API key from env; Polly retry
- [ ] **Frontend**: `@react-google-maps/api`; `GoogleMap` component; `Marker` for approx pin; `useJsApiLoader` with API key from `VITE_GOOGLE_MAPS_API_KEY`; Maps JavaScript API enabled in Google Cloud Console
- [ ] Google Cloud Console: enable Geocoding API, Address Validation API, Maps JavaScript API; restrict API key by domain

### 14.3 Persona (KYC + Background Check)

- [ ] `PersonaClient` — `HttpClient` to `https://withpersona.com/api/v1/`; `Authorization: Bearer {PERSONA_API_KEY}`; Polly retry
- [ ] KYC Inquiry Template configured in Persona dashboard: liveness check + government ID + selfie
- [ ] Background Check Report configured in Persona: FCRA-compliant; criminal, sex offender, global watchlist
- [ ] Webhook endpoint: validates `Persona-Signature` header (HMAC-SHA256); dispatches Complete/Fail commands
- [ ] Manual review queue: high-risk flags → ops dashboard (admin `ManualVerification.tsx` page)

### 14.4 MailKit + Brevo SMTP (Email)

- [ ] `MailKitEmailService` — `MailKit.Net.Smtp.SmtpClient`; connects to `smtp-relay.brevo.com:587`; STARTTLS; authenticates with Brevo API key as password
- [ ] `MimeMessage` built with `MimeKit`; `From: noreply@lagedra.com`; HTML + plain-text parts
- [ ] All 12 required system notice templates implemented as inline C# string templates
- [ ] Polly retry: 3 attempts, 2s / 4s / 8s exponential back-off on SMTP errors
- [ ] Brevo dashboard: sender domain verified (SPF + DKIM), bounce handling configured

### 14.5 MinIO (Object Storage — Evidence + Exports)

- [ ] MinIO running in Docker (self-hosted, S3-compatible)
- [ ] `AWSSDK.S3` (`AmazonS3Client`) configured with MinIO endpoint
- [ ] Buckets: `evidence` (7-year lifecycle policy), `exports` (48h lifecycle)
- [ ] Presigned upload URLs: 15-minute expiry
- [ ] Presigned download URLs: 1-hour expiry
- [ ] Server-side encryption: MinIO SSE-S3 enabled
- [ ] MinIO Console accessible at `:9001` (admin only)

### 14.6 ClamAV (Antivirus)

- [ ] ClamAV running in Docker: `clamav/clamav:latest`; freshclam virus database auto-update
- [ ] `ClamAvService` — TCP socket scan via `nClam` NuGet (or REST if using `clamav/clamav-rest`); returns Clean/Infected
- [ ] Infected files: quarantined to `evidence/quarantine/` prefix; never served to users; ops notified via email
- [ ] Add `nClam` to `Directory.Packages.props`

### 14.7 Insurance API (MGA Partner — TBD)

- [ ] `InsuranceApiClient` implemented as stub; `IInsuranceApiClient` contract defined
- [ ] Webhook endpoint implemented; handles policy change events
- [ ] 72h grace window + manual fallback fully implemented (does not depend on real integration)
- [ ] Real MGA partner integration to be wired when LOI converts to contract

---

## Phase 15 — Beta Readiness Gate

> **Public beta may begin ONLY when ALL 7 conditions are marked complete.**

- [ ] **Gate 1 — Identity Verification** — Persona KYC + background check fully implemented; liveness, document auth, synthetic ID detection; manual review queue in admin dashboard operational; ≤ 24h manual review SLA confirmed
- [ ] **Gate 2 — Insurance Integration** — Insurance API stub + manual fallback tested end-to-end; Active / NotActive / Unknown states handled; 72h grace window verified (both API-failure path and tenant-inaction path); manual proof upload portal functional; 24h SLA confirmed
- [ ] **Gate 3 — Truth Surface Integrity** — HMAC-SHA256 signing + canonical JSON hashing verified on all snapshots; append-only audit log integrity confirmed; `SnapshotVerificationJob` passing; `InquiryClosed=true` included in hash; Merkle tree partial proof working
- [ ] **Gate 4 — Jurisdiction Pack** — California / LA v1 pack unit tested for all field gating (AB 12, SB 611, AB 628, AB 2801, AB 414, JCO); dual-control approval workflow functional; effective-date rules verified; CaliforniaPackTests all green
- [ ] **Gate 5 — Arbitration Panel** — Minimum 3 arbitrators signed, onboarded, and trained on protocol; reserve capacity pool contracted; 14-day decision SLA agreement in place; caseload cap (20 hard / 15 soft) implemented and tested; backlog escalation email automation working
- [ ] **Gate 6 — Monitoring Dashboard** — Admin app live with all 10 ops pages; insurance unknown queue, fraud flags, arbitration backlog, jurisdiction pack versions all functional; email alerts for all SLA breaches working; Serilog logs to file with rotation
- [ ] **Gate 7 — Incident Response** — Tabletop simulation completed: insurance partner outage, arbitration backlog spike, PII breach, fraud flag surge; at least one live drill; RCA process documented in `docs/runbooks/`

---

## KPIs & Pilot Metrics (Instrument Before Beta)

- [ ] **Deposit Reduction Delta (DRD)** — median % change in deposit vs. landlord baseline; per deal at activation; segmented by Verification Class + insurance state; logged to Trust Ledger
- [ ] **Deposit Adoption Rate** — % of eligible deals (Low/Medium Risk + Active/InstitutionBacked) where landlord sets deposit within recommended band or lower
- [ ] **Dispute Rate** — cases filed per 100 active deal-months
- [ ] **Median Resolution Time** — case filed → decision issued
- [ ] **Recovery Evidence Rate** — % of cases where evidence meets minimum schema on first submission
- [ ] **Protocol Fee Churn Rate** — % of deals that lapse on fee within first billing cycle
- [ ] **Publish quarterly transparency report** — anonymized: DRD by class, dispute rate, resolution time, evidence rate

---

## Pilot Programs

- [ ] **Deposit Reduction Guarantee Pilot** — first 100 Low Risk + Insured tenants; if landlord accepts recommended deposit and covered loss occurs, facilitate supplemental insurance claim; track DRD as primary KPI
- [ ] **Institutional Partner Pilot** — first 12 months: $39/month for Verified Institutional Partners; track activation volume from institutional channel
- [ ] **Phase 1.5 Invite-Only Public Beta** — California only; waitlist-gated; concurrent with institutional pilot; stress-test user flows with real non-institutional users

---

*Last updated: 2026-02-19. Technology stack locked. Update checkboxes as work is completed.*
*Each `[ ]` → `[x]` is a step toward a defensible, enforceable, institution-grade mid-term rental protocol.*
