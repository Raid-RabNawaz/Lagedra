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
> 4. Once Truth Surface confirmed → system reveals host payment details to tenant → tenant pays host directly (off-platform, total = first month rent + deposit + insurance fee) → host confirms receipt via platform → host pays platform fees (insurance + activation fee) → `ActivateDealCommand` runs → `BillingAccount` activated
> 5. Deal status: `ApplicationPending → ApplicationApproved → TruthSurfacePending → TruthSurfaceConfirmed → AwaitingPaymentConfirmation → Active → Closed`
>
> There is **no separate Deal module** and **no separate Application module**.

---

## Architecture Note: Direct Payment & Payment Confirmation Flow

> **Decision:** The platform stays **out of the money flow**. Tenants pay hosts directly. The platform provides a verifiable confirmation/dispute mechanism.
>
> Flow:
> 1. Both parties confirm the Truth Surface → `TruthSurfaceConfirmedEvent` fires
> 2. `OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler` creates a `DealPaymentConfirmation` (Pending) in `ActivationAndBilling`
> 3. System reveals the host's payment details to the tenant (fetched from `IdentityAndVerification` via `IHostPaymentDetailsProvider` interface in SharedKernel, decrypted server-side using `IEncryptionService`)
> 4. Tenant pays host directly (bank transfer, etc.) — **off-platform**
> 5. Host confirms receipt via `POST /v1/deals/{dealId}/payment/confirm` → deal activates (protections, billing start)
> 6. If host doesn't confirm within 72h grace period, tenant can dispute via `POST /v1/deals/{dealId}/payment/dispute` with proof of payment (uploaded to Evidence module)
> 7. Ops/admin reviews dispute evidence and resolves via `POST /v1/admin/deals/{dealId}/resolve-payment-dispute`
> 8. `PaymentConfirmationTimeoutJob` (Quartz, hourly) escalates pending confirmations past 72h
>
> **TruthSurface is NOT modified** — it seals the immutable agreement when both parties confirm. Payment confirmation is a Deal lifecycle concern owned by `ActivationAndBilling`.
>
> **Cross-module data access:** `IHostPaymentDetailsProvider` interface lives in SharedKernel, implemented by `IdentityAndVerification`, consumed by `ActivationAndBilling` query handler. No direct module-to-module reference.

---

## Architecture Note: Partner Company System & Financial Structure

> **Decision:** Business partners (relocation/tech companies) have a dedicated entry point. The platform tracks partner membership, referral links, and direct reservations via the `PartnerNetwork` module.
>
> How it works:
> 1. Partner companies register via `POST /v1/partners` (role: `InstitutionPartner`); platform admin verifies the org via `POST /v1/partners/{id}/verify`
> 2. Partners can add members, generate referral links with usage limits and expiry, and create direct reservations for employees/clients
> 3. Referral redemption fires `ReferralRedeemedEvent` → `OnReferralRedeemedRecalculateRiskHandler` upgrades risk profile to Low (InstitutionBacked insurance status)
> 4. `IPartnerMembershipProvider` (SharedKernel interface, implemented by PartnerNetwork) provides cross-module access to partner organization membership
> 5. `JwtTokenService.GenerateAccessTokenAsync()` adds `partner_org_id` claim to JWT if the user belongs to a partner organization
>
> **Financial Structure (Initial Payment):**
> - Tenant pays host directly: **First month's rent + Deposit amount + Insurance fee** = `TotalTenantPaymentCents`
> - Host pays platform at activation: **Insurance fee + Activation fee** = `TotalHostPlatformPaymentCents`
> - Deposit amount is set by host when approving application (cannot exceed listing's `MaxDepositCents`)
> - Insurance fee is a one-time amount depending on stay duration, calculated via `IInsuranceFeeCalculator`
> - `DealFinancials` value object encapsulates the full breakdown
> - `ActivateDealCommand` gates on `DealPaymentConfirmation.HostPaidPlatform == true`
>
> **Jurisdiction Warning (AB 1482):**
> - `JurisdictionWarningService.CheckForWarnings()` returns a warning string if stay > 175 days in Los Angeles jurisdiction (`US-CA-LA`)
> - Warning is stored on `DealApplication.JurisdictionWarning` and shown to the host at approval time

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

- [x] `Application/Commands/RegisterUserCommand.cs` + handler — creates `ApplicationUser`, sends verification email via `IEmailService`; self-registration allowed for `Tenant`, `Landlord`, `InstitutionPartner` only (blocks `Arbitrator`, `PlatformAdmin`, `InsurancePartner`)
- [x] `Application/Commands/VerifyEmailCommand.cs` + handler — validates token, marks user active, fires `UserRegisteredEvent`
- [x] `Application/Commands/ResendVerificationCommand.cs` + handler — resends verification email for unconfirmed users; returns generic success even if user not found or already verified (prevents email enumeration)
- [x] `Application/Commands/LoginCommand.cs` + handler — validates credentials, returns `AccessToken` + `RefreshToken`; SuperAdmin bypass: authenticates against config credentials (`Seed:SuperAdmin` section) without DB verification
- [x] `Application/Commands/RefreshTokenCommand.cs` + handler — rotates refresh token, returns new pair
- [x] `Application/Commands/RevokeTokenCommand.cs` + handler — invalidates refresh token on logout
- [x] `Application/Commands/ForgotPasswordCommand.cs` + handler — generates reset token, sends email
- [x] `Application/Commands/ResetPasswordCommand.cs` + handler — validates token, updates password
- [x] `Application/Commands/ChangePasswordCommand.cs` + handler
- [x] `Application/Commands/UpdateRoleCommand.cs` + handler — admin only
- [x] `Application/Queries/GetCurrentUserQuery.cs` + handler — returns user profile from token claims
- [x] `Application/Services/JwtTokenService.cs` — generates signed JWT (`HS256`, configurable expiry), embeds: `sub`, `email`, `role`, `jti`, `partner_org_id` (if user belongs to a partner org); `GenerateAccessTokenAsync()` async method looks up partner membership via `IPartnerMembershipProvider`
- [x] `Application/Services/RefreshTokenService.cs` — generates cryptographically random token, stores hashed
- [x] `Application/DTOs/AuthResultDto.cs` — `AccessToken`, `RefreshToken`, `ExpiresIn`, `Role`
- [x] `Application/DTOs/UserProfileDto.cs`
- [x] `Application/DTOs/RegisterResultDto.cs` — `UserId`, `VerificationUrl` (`System.Uri`), `VerificationToken`
- [x] `Application/DTOs/ResendVerificationResultDto.cs` — `Sent`, `VerificationUrl` (`System.Uri?`), `VerificationToken`; includes `Blind()` factory for enumeration-safe empty response

### 1.4 Presentation

- [x] `Presentation/Endpoints/AuthEndpoints.cs` (Minimal API):
  - `POST /v1/auth/register`
  - `GET /v1/auth/verify-email`
  - `POST /v1/auth/resend-verification`
  - `POST /v1/auth/login`
  - `POST /v1/auth/refresh`
  - `POST /v1/auth/logout`
  - `POST /v1/auth/forgot-password`
  - `POST /v1/auth/reset-password`
  - `GET /v1/auth/me`
- [x] `Presentation/Contracts/RegisterRequest.cs` — `Email`, `Password`, `Role` (`Tenant` / `Landlord` / `InstitutionPartner` at self-registration)
- [x] `Presentation/Contracts/ResendVerificationRequest.cs` — `Email`
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
- [x] `Infrastructure/Seed/AuthDataSeeder.cs` — partial class; `SeedAsync()` calls `SeedSuperAdminAsync()` + `SeedDevUsersAsync()`
- [x] `Infrastructure/Seed/SuperAdminSettings.cs` — typed config from `Seed:SuperAdmin` section: `Email`, `Password`, `DisplayName`; SuperAdmin is created idempotently on startup, has `PlatformAdmin` role, email pre-confirmed, no DB verification required for login
- [x] Dev user seeding (Development env only): `tenant@lagedra.dev`, `landlord@lagedra.dev`, `arbitrator@lagedra.dev`, `insurance@lagedra.dev`, `institution@lagedra.dev` — all pre-verified, password `Dev@1234!`
- [x] EF Core migrations for `auth` schema (users, roles, user-roles, refresh_tokens)

### 1.6 Module Registration

- [x] `AuthModuleRegistration.cs` — `AddAuth(IServiceCollection, IConfiguration)`:
  - Configure `Identity` with password policy
  - Configure JWT Bearer authentication
  - Register `JwtTokenService`, `RefreshTokenService`
  - Register `AuthDbContext`, `RefreshTokenRepository`
  - Register `AuthDataSeeder`
  - Register MediatR handlers

### 1.7 Social / External Login (Google, Apple, Microsoft)

- [x] `Domain/ExternalAuthProvider.cs` — enum: `Google`, `Apple`, `Microsoft`
- [x] `Application/Settings/ExternalAuthSettings.cs` — typed config from `ExternalAuth` section: `Google.ClientId`, `Apple.ClientId/TeamId/KeyId`, `Microsoft.ClientId/TenantId`
- [x] `Application/Services/ExternalAuthValidator.cs` — validates provider ID tokens server-side:
  - Google: `GoogleJsonWebSignature.ValidateAsync` via `Google.Apis.Auth`
  - Apple: JWKS from `https://appleid.apple.com/auth/keys` via OpenID Connect discovery
  - Microsoft: JWKS from `https://login.microsoftonline.com/{tenant}/v2.0` via OpenID Connect discovery
  - Returns `ExternalUserInfo(ProviderKey, Email, FirstName?, LastName?)`
- [x] `Application/Commands/ExternalLoginCommand.cs` + handler — validates token, finds or creates user, links external login via `UserManager`, issues JWT + refresh token; new users get email pre-verified, default role `Tenant` (or `PreferredRole` if provided and allowed)
- [x] `Presentation/Contracts/ExternalLoginRequest.cs` — `Provider`, `IdToken`, `PreferredRole?`
- [x] `Presentation/Endpoints/AuthEndpoints.cs` — `POST /v1/auth/external-login` (AllowAnonymous)
- [x] `AuthModuleRegistration.cs` — register `ExternalAuthValidator`, bind `ExternalAuthSettings`, register `IHttpClientFactory`
- [x] NuGet packages added: `Google.Apis.Auth`, `Microsoft.Identity.Client`, `Microsoft.IdentityModel.Protocols.OpenIdConnect`
- [x] `appsettings.json` — `ExternalAuth` section with placeholder client IDs for Google, Apple, Microsoft

### 1.8 Soft Delete for ApplicationUser

- [x] `Domain/ApplicationUser.cs` — implements `ISoftDeletable` (`IsDeleted`, `DeletedAt`)
- [x] `Infrastructure/Persistence/AuthDbContext.cs` — registers `SoftDeleteInterceptor`, adds query filter `HasQueryFilter(u => !u.IsDeleted)` on `ApplicationUser`
- [x] EF Core migration for `auth` schema: add `IsDeleted` (bool, default false) + `DeletedAt` (DateTime?) columns to `AspNetUsers`

---

## Phase 2 — Shared Kernel (`Lagedra.SharedKernel`)

> No business logic. No dependencies on other Lagedra projects. Pure abstractions.

### 2.1 Project Setup

- [x] Create `src/Lagedra.SharedKernel/Lagedra.SharedKernel.csproj` (references `MediatR.Contracts` only)
- [x] Add project to `.sln`

### 2.2 Domain Primitives

- [x] `Domain/Entity.cs` — base entity: `Id`, `CreatedAt`, `UpdatedAt`, equality by Id; implements `ISoftDeletable` (`IsDeleted`, `DeletedAt`)
- [x] `Domain/AggregateRoot.cs` — extends `Entity<TId>`, owns `List<IDomainEvent>`, exposes `AddDomainEvent` / `ClearDomainEvents`
- [x] `Domain/ValueObject.cs` — abstract: `GetEqualityComponents`, structural equality, `==` / `!=` operators
- [x] `Domain/IDomainEvent.cs` — interface: `EventId` (Guid), `OccurredAt` (DateTime)
- [x] `Domain/IAggregateRoot.cs` — marker interface
- [x] `Domain/ISoftDeletable.cs` — interface: `IsDeleted` (bool), `DeletedAt` (DateTime?)
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
- [x] `Security/IEncryptionService.cs` — `Encrypt(string plaintext): string`, `Decrypt(string ciphertext): string` — symmetric AES-256 for sensitive data at rest (host payment details, PII)

### 2.6.1 Cross-Module Integration Contracts

- [x] `Integration/IHostPaymentDetailsProvider.cs` — `GetDecryptedPaymentDetailsAsync(Guid hostUserId, CancellationToken): Task<HostPaymentDetailsDto?>` — implemented by `IdentityAndVerification`, consumed by `ActivationAndBilling`
- [x] `Integration/HostPaymentDetailsDto.cs` — `HostUserId`, `PaymentInfo` (plaintext after decryption)
- [x] `Integration/IPartnerMembershipProvider.cs` — `GetPartnerOrganizationIdAsync(Guid userId, CancellationToken): Task<Guid?>` — implemented by `PartnerNetwork`, consumed by `Auth` (JWT claims)

### 2.6.2 Insurance Contracts

- [x] `Insurance/IInsuranceFeeCalculator.cs` — `CalculateFeeAsync(long monthlyRentCents, int stayDurationDays, CancellationToken): Task<InsuranceFeeQuote>` — implemented by `InsuranceIntegration`, consumed by `ActivationAndBilling`
- [x] `Insurance/InsuranceFeeQuote.cs` — `FeeCents` (long), `Provider` (string), `QuoteReference` (string?)

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

- [x] Create `src/Lagedra.Infrastructure/Lagedra.Infrastructure.csproj`
- [x] Reference `Lagedra.SharedKernel`
- [x] Packages: EF Core, Npgsql, Serilog, OpenTelemetry, Polly, MailKit, AWSSDK.S3 (MinIO), Stripe.net, nClam

### 3.2 Persistence

- [x] `Persistence/BaseDbContext.cs` — abstract `DbContext` implementing `IUnitOfWork`; applies interceptors
- [x] `Persistence/DbContextFactory.cs` — design-time factory for `dotnet ef migrations`
- [x] `Persistence/Interceptors/AuditingInterceptor.cs` — sets `CreatedAt` / `UpdatedAt` on `SaveChangesAsync`
- [x] `Persistence/Interceptors/OutboxInterceptor.cs` — serializes `IDomainEvent` list to `outbox.outbox_messages` on `SaveChangesAsync`
- [x] `Persistence/Interceptors/SoftDeleteInterceptor.cs` — converts `Delete` to `Modified` for `ISoftDeletable` entities; sets `IsDeleted=true`, `DeletedAt` via `IClock`
- [x] `Persistence/BaseDbContext.cs` — `ApplySoftDeleteQueryFilters()` auto-applies `HasQueryFilter(e => !e.IsDeleted)` to all `ISoftDeletable` entities via reflection
- [x] `Persistence/Configurations/OutboxMessageConfiguration.cs`
- [x] `Persistence/Configurations/AuditEventConfiguration.cs` — deferred to Phase 4 (no module-level audit reads yet)
- [x] `Persistence/OutboxMessage.cs` — `Id`, `Type`, `Content`, `OccurredAt`, `ProcessedAt`, `RetryCount`, `Error`
- [x] `Persistence/Seed/SeedData.cs` — predefined question library, jurisdiction pack seeds (Phase 4+)
- [x] `Persistence/Seed/SeedRunner.cs` — idempotent on startup (Phase 4+)

### 3.3 Eventing

- [x] `Eventing/InMemoryEventBus.cs` — resolves `IDomainEventHandler<T>` from DI, dispatches in-process
- [x] `Eventing/OutboxProcessor.cs` — reads unprocessed outbox messages, dispatches, marks processed
- [x] `Eventing/OutboxDispatcher.cs` — background polling loop (registered in Worker)
- [x] `Eventing/EventBusExtensions.cs` — DI helpers

### 3.4 External Client Contracts + Implementations

**Email — MailKit + Brevo SMTP**
- [x] `External/Email/MailKitEmailService.cs` — implements `IEmailService`; uses `MailKit.Net.Smtp.SmtpClient`; configured via `BrevoSmtpSettings`; HTML + plain-text support
- [x] `External/Email/BrevoSmtpSettings.cs` — typed config: `Host = smtp-relay.brevo.com`, `Port = 587`, `Username`, `ApiKey`
- [x] Email templates: inline string templates (no Razor dependency)

**Payments — Stripe**
- [x] `External/Payments/IStripeService.cs` — `CreateSubscription`, `CancelSubscription`, `CreateProratedInvoice`, `HandleWebhookEvent`
- [x] `External/Payments/StripeService.cs` — implements using `Stripe.net`; validates webhook signature via `Stripe.EventUtility.ConstructEvent`
- [x] `External/Payments/StripeSettings.cs` — `PublishableKey`, `SecretKey`, `WebhookSecret`

**Geocoding — Google Maps**
- [x] `External/Geocoding/IGeocodingService.cs` — `GeocodeAddress`, `ReverseGeocode`, `ResolveJurisdiction`
- [x] `External/Geocoding/GoogleMapsGeocodingService.cs` — implements via `HttpClient` calling Google Maps API
- [x] `External/Geocoding/GoogleMapsSettings.cs` — `ApiKey`

**KYC + Background Check — Persona**
- [x] `External/Persona/IPersonaClient.cs` — `CreateInquiry`, `GetInquiry`, `HandleWebhook`
- [x] `External/Persona/PersonaClient.cs` — `HttpClient`-based; webhook HMAC-SHA256 signature validation
- [x] `External/Persona/PersonaSettings.cs` — `ApiKey`, `TemplateId`, `WebhookSecret`

**Object Storage — MinIO (S3-compatible)**
- [x] `External/Storage/IObjectStorageService.cs` — `GeneratePresignedUploadUrl`, `GeneratePresignedDownloadUrl`, `DeleteObject`, `ObjectExists`, `EnsureBucketExistsAsync`
- [x] `External/Storage/MinioStorageService.cs` — implements using `AWSSDK.S3` (`AmazonS3Client`) pointed at MinIO endpoint
- [x] `External/Storage/MinioSettings.cs` — `Endpoint`, `AccessKey`, `SecretKey`, `EvidenceBucket`, `ExportsBucket`

**Antivirus — ClamAV**
- [x] `External/Antivirus/IAntivirusService.cs` — `ScanAsync(Stream, CancellationToken): Task<ScanResult>`
- [x] `External/Antivirus/ClamAvService.cs` — TCP socket via `nClam` NuGet package
- [x] `External/Antivirus/ClamAvSettings.cs` — `Host`, `Port`, `TimeoutSeconds`

**Insurance API**
- [x] `External/Insurance/IInsuranceApiClient.cs` — `VerifyPolicy`, `GetPolicyStatus`, `HandleWebhook`
- [x] `External/Insurance/InsuranceApiClient.cs` — stub implementation (real MGA partner TBD)

### 3.5 Security

- [x] `Security/DataProtectionSetup.cs` — ASP.NET Data Protection, keys persisted to filesystem volume (switch to Redis/PG for multi-replica)
- [x] `Security/Secrets.cs` — deferred; environment variables used directly via IConfiguration
- [x] `Security/HashingService.cs` — SHA-256 via `System.Security.Cryptography` (implements `IHashingService`)
- [x] `Security/CryptographicSigner.cs` — HMAC-SHA256 (implements `ICryptographicSigner`)
- [x] `Security/EncryptionService.cs` — AES-256-GCM via `System.Security.Cryptography` (implements `IEncryptionService`); key sourced from `IConfiguration["Encryption:Key"]`

### 3.6 Observability

- [x] `Observability/HealthChecks.cs` — PostgreSQL, MinIO, ClamAV, Persona, Google Maps, Stripe liveness probes
- [x] `Observability/CorrelationIdMiddleware.cs` — reads/generates `X-Correlation-Id` header, pushes to Serilog `LogContext`, adds to response headers
- [x] `Observability/GlobalExceptionHandlerMiddleware.cs` — catches all unhandled exceptions, logs at Error level with full context (method, path, correlation ID, stack trace), returns RFC 7807 Problem Details JSON; development mode includes exception details
- [x] Serilog configured with console sink + rolling file sink (`logs/lagedra-{Date}.log`, 30-day retention, 100MB per file) + structured JSON file in Development (`logs/lagedra-structured-{Date}.json`)
- [x] Serilog enrichment: `FromLogContext`, `WithCorrelationId`, `WithMachineName`, `WithEnvironmentName`
- [ ] `Observability/Metrics.cs` — OpenTelemetry metrics (Phase 4+)
- [ ] `Observability/Tracing.cs` — OTEL tracing (Phase 4+)

### 3.6.1 MediatR Pipeline Behaviors

- [x] `Behaviors/LoggingBehavior.cs` — logs every command/query: request type name on start, elapsed time on completion, warns if handler takes > 500ms; uses `[LoggerMessage]` source generation
- [x] `Behaviors/UnhandledExceptionBehavior.cs` — wraps handler execution in try/catch, logs unhandled exceptions at Error level with request type, re-throws for global exception middleware

### 3.7 DI Registration

- [x] `InfrastructureServiceRegistration.cs` — `AddInfrastructure(IServiceCollection, IConfiguration)`:
  - `IClock` → `SystemClock`
  - `IEventBus` → `InMemoryEventBus`
  - `IEmailService` → `MailKitEmailService`
  - `IStripeService` → `StripeService`
  - `IGeocodingService` → `GoogleMapsGeocodingService`
  - `IPersonaClient` → `PersonaClient`
  - `IEncryptionService` → `EncryptionService` (already registered)
  - `IObjectStorageService` → `MinioStorageService`
  - `IAntivirusService` → `ClamAvService`
  - `IInsuranceApiClient` → `InsuranceApiClient`
  - Data Protection, HealthChecks, OutboxDispatcher background service

### 3.8 Shared DB Schemas

- [x] Outbox table is now **per-module schema** (`truth_surface.outbox_messages`, `compliance.outbox_messages`, etc.) — no shared `outbox` schema. `OutboxMessageConfiguration` accepts a `schema` constructor arg; `BaseDbContext.ModuleSchema` sets it.
- [x] `IOutboxContext` interface introduced — `BaseDbContext` implements it; `OutboxDispatcher` resolves `IEnumerable<IOutboxContext>` and processes all registered modules independently. No cross-module row collisions.
- [x] `EventBusExtensions.AddOutboxContext<TContext>()` helper — each module calls this in its registration to self-enroll in the outbox dispatch loop.
- [x] SQL schema: `audit` schema, `audit_events` table (Phase 4+)
- [x] EF Core migrations baseline applied via `dotnet ef database update`

---

## Phase 4 — Core Services

### 4.1 Truth Surface Engine (`Lagedra.TruthSurface`)

> Immutable, cryptographically signed deal snapshots. Append-only. No deletes.

#### Project Setup
- [x] Create `src/Lagedra.TruthSurface/Lagedra.TruthSurface.csproj`
- [x] Reference `Lagedra.SharedKernel`, `Lagedra.Infrastructure`
- [x] Add to `.sln`

#### Presentation
- [x] `Presentation/Endpoints/TruthSurfaceEndpoints.cs` — `POST /`, `POST /{id}/confirm`, `POST /{id}/reconfirm`, `GET /{id}`, `GET /{id}/verify`
- [x] `Presentation/Contracts/CreateSnapshotRequest.cs`
- [x] `Presentation/Contracts/ConfirmSnapshotRequest.cs`
- [x] `Presentation/Contracts/ReconfirmSnapshotRequest.cs`

#### Domain
- [x] `Domain/TruthSnapshot.cs` — aggregate: `DealId`, `Status` (Draft → PendingBothConfirmations → PendingLandlord/PendingTenant → Confirmed → Superseded), `SealedAt`, `Hash`, `Signature`, `ProtocolVersion`, `JurisdictionPackVersion`, `InquiryClosed`
- [x] `Domain/CryptographicProof.cs` — entity: `SnapshotId`, `Hash`, `Signature`, `SignedAt`
- [x] `Domain/TruthSurfaceStatus.cs` — enum
- [x] `Domain/TruthSurfaceVersion.cs` — value object (Major.Minor.Patch)
- [x] Domain event: `TruthSurfaceConfirmedEvent`
- [x] Domain event: `TruthSurfaceSupersededEvent`

#### Application
- [x] `Application/Commands/CreateSnapshotCommand.cs` + handler — creates draft, submits for confirmation
- [x] `Application/Commands/ConfirmTruthSurfaceCommand.cs` + handler — validates per-party confirmation; on both confirmed, seals with canonical JSON → SHA-256 → HMAC-SHA256; fires `TruthSurfaceConfirmedEvent`
- [x] `Application/Commands/ReconfirmTruthSurfaceCommand.cs` + handler — supersedes original, creates new draft for pack update
- [x] `Application/Queries/GetSnapshotQuery.cs` + handler
- [x] `Application/Queries/VerifySnapshotQuery.cs` + handler — re-computes hash, verifies signature
- [x] `Application/DTOs/TruthSurfaceDto.cs`
- [x] `Application/DTOs/SnapshotProofDto.cs`

#### Infrastructure / Crypto
- [x] `Infrastructure/Crypto/CanonicalHasher.cs` — SHA-256 from canonical JSON string
- [x] `Infrastructure/Crypto/MerkleTreeBuilder.cs` — line-item Merkle tree for partial proof
- [x] `Infrastructure/Persistence/TruthSurfaceDbContext.cs` — schema `truth_surface`, extends `BaseDbContext`
- [x] `Infrastructure/Persistence/TruthSurfaceDbContextFactory.cs` — `IDesignTimeDbContextFactory`
- [x] `Infrastructure/Repositories/SnapshotRepository.cs`
- [x] `Infrastructure/Configurations/TruthSnapshotConfiguration.cs`
- [x] `Infrastructure/Configurations/CryptographicProofConfiguration.cs`
- [x] `Infrastructure/Jobs/SnapshotVerificationJob.cs` — weekly (Sunday 3AM): re-computes all hashes, flags tamper mismatches at CRITICAL level
- [x] EF Core migrations (run `dotnet ef migrations add InitialCreate` after restore)

#### Module Registration
- [x] `TruthSurfaceModuleRegistration.cs` — DbContext, outbox, repository, MediatR, Quartz job

---

### 4.2 Compliance & Trust Ledger (`Lagedra.Compliance`)

> Append-only. No deletes. Ever.

#### Project Setup
- [x] Create `src/Lagedra.Compliance/Lagedra.Compliance.csproj`
- [x] Reference `Lagedra.SharedKernel`, `Lagedra.Infrastructure`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Violation.cs` — entity (append-only): `DealId`, `ReportedByUserId`, `Category`, `Status` (Open → UnderReview → Resolved/Dismissed/Escalated), `Description`, `EvidenceReference`, `DetectedAt`, `ResolvedAt`
- [x] `Domain/TrustLedgerEntry.cs` — entity (append-only, immutable): `UserId`, `EntryType`, `ReferenceId`, `Description`, `OccurredAt`, `IsPublic`
- [x] `Domain/ViolationCategory.cs` — enum: NonPayment, UnauthorizedOccupants, PropertyDamage, RuleViolation, InsuranceLapse, EarlyTermination, Other
- [x] `Domain/ViolationStatus.cs` — enum: Open, UnderReview, Resolved, Dismissed, Escalated
- [x] `Domain/TrustLedgerEntryType.cs` — enum: DealCompleted, ViolationRecorded, ViolationDismissed, ArbitrationRuling, InsuranceClaim, PaymentDefault, EarlyTermination, PositiveReview, IdentityVerified
- [x] `Domain/ComplianceSignal.cs` — lightweight inbound signal: `DealId`, `SignalType`, `Payload`, `ReceivedAt`, `Processed`

#### Application
- [x] `Application/Commands/RecordViolationCommand.cs` + handler
- [x] `Application/Commands/RecordLedgerEntryCommand.cs` + handler — append-only (no update/delete methods on repository)
- [x] `Application/Commands/CloseComplianceWindowCommand.cs` + handler — resolves open violations, marks signals processed
- [x] `Application/Queries/GetTrustLedgerForUserQuery.cs` + handler — public entries only (IsPublic filter)
- [x] `Application/Queries/GetFullLedgerForDealQuery.cs` + handler — all violations + related ledger entries
- [x] `Application/DTOs/ViolationDto.cs`
- [x] `Application/DTOs/TrustLedgerEntryDto.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/ComplianceDbContext.cs` — schema `compliance`, extends `BaseDbContext`
- [x] `Infrastructure/Persistence/ComplianceDbContextFactory.cs` — `IDesignTimeDbContextFactory`
- [x] `Infrastructure/Configurations/ViolationConfiguration.cs`
- [x] `Infrastructure/Configurations/TrustLedgerEntryConfiguration.cs`
- [x] `Infrastructure/Configurations/ComplianceSignalConfiguration.cs`
- [x] `Infrastructure/Repositories/ViolationRepository.cs`
- [x] `Infrastructure/Repositories/TrustLedgerRepository.cs` — write-only append, read-only projection
- [x] EF Core migrations (run `dotnet ef migrations add InitialCreate` after restore)

#### Module Registration
- [x] `ComplianceModuleRegistration.cs` — DbContext, outbox, repositories, MediatR

---

## Phase 5 — Business Modules (`src/Lagedra.Modules/`)

> All modules follow identical Clean Architecture. No direct references between modules. Communication via domain events through the Outbox only.

---

### 5.1 ActivationAndBilling

> **Owns Deal lifecycle + Application flow + Billing.** `DealId` (Guid) is created here when landlord approves an application. All other modules reference this `DealId`.

#### Project & References
- [x] `ActivationAndBilling.csproj` — references SharedKernel, Infrastructure, TruthSurface, Compliance, JurisdictionPacks, ListingAndLocation
- [x] Add to `.sln`

#### Domain — Application & Deal
- [x] `Domain/Aggregates/DealApplication.cs` — `ListingId`, `TenantUserId`, `LandlordUserId`, `Status` (Pending/Approved/Rejected), `DealId?` (set on approval), `SubmittedAt`, `DecidedAt`, `DepositAmountCents` (set by host at approval), `InsuranceFeeCents`, `FirstMonthRentCents`, `PartnerOrganizationId?`, `IsPartnerReferred`, `JurisdictionWarning?`; `Submit()` optionally accepts `partnerOrganizationId`/`isPartnerReferred`; `Approve()` accepts financial params + `jurisdictionWarning`
- [x] `Domain/Events/ApplicationSubmittedEvent.cs`
- [x] `Domain/Events/ApplicationApprovedEvent.cs` — carries the newly generated `DealId`; consumed by all modules that need to prepare for this deal
- [x] `Domain/Events/ApplicationRejectedEvent.cs`

#### Domain — Payment Confirmation (Direct Payment Flow)
- [x] `Domain/Aggregates/DealPaymentConfirmation.cs` — `AggregateRoot<Guid>`: `DealId`, `HostConfirmed`, `HostConfirmedAt`, `TenantDisputed`, `TenantDisputedAt`, `DisputeReason`, `DisputeEvidenceManifestId` (from Evidence module), `Status` (PaymentConfirmationStatus), `GracePeriodExpiresAt`, `TotalTenantPaymentCents`, `TotalHostPlatformPaymentCents`, `HostPaidPlatform` (bool), `HostPaidPlatformAt` (DateTime?); methods: `ConfirmByHost(IClock)`, `DisputeByTenant(reason, evidenceManifestId, IClock)`, `ResolveDispute(paymentValid, IClock)`, `IsGracePeriodExpired(IClock)`, `ConfirmHostPlatformPayment(IClock)`; raises `PaymentConfirmedEvent`, `PaymentDisputedEvent`, `PaymentDisputeResolvedEvent` via `AddDomainEvent()`
- [x] `Domain/ValueObjects/PaymentConfirmationStatus.cs` — enum: Pending, Confirmed, Disputed, Rejected
- [x] `Domain/Events/PaymentConfirmedEvent.cs` — `DealId`, `ConfirmedAt`
- [x] `Domain/Events/PaymentDisputedEvent.cs` — `DealId`, `TenantUserId`, `Reason`, `EvidenceManifestId?`
- [x] `Domain/Events/PaymentDisputeResolvedEvent.cs` — `DealId`, `PaymentValid`, `ResolvedBy`
- [x] `Domain/Interfaces/IDealPaymentConfirmationRepository.cs` — `GetByDealIdAsync(Guid dealId, CancellationToken): Task<DealPaymentConfirmation?>`, `GetPendingExpiredAsync(DateTime cutoff, CancellationToken): Task<IReadOnlyList<DealPaymentConfirmation>>`

#### Domain — Financial Structure & Jurisdiction Warnings
- [x] `Domain/ValueObjects/DealFinancials.cs` — encapsulates deal financial breakdown: `FirstMonthRentCents`, `DepositAmountCents`, `InsuranceFeeCents`, `ActivationFeeCents`; calculated properties: `TotalTenantPaymentCents` (rent + deposit + insurance), `TotalHostPlatformPaymentCents` (insurance + activation); `Create()` factory with validation
- [x] `Domain/Services/JurisdictionWarningService.cs` — static service: `CheckForWarnings(string? jurisdictionCode, int stayDurationDays): string?`; returns AB 1482 "Just Cause" warning for `US-CA-LA` stays > 175 days; extensible for future jurisdictions

#### Domain — Billing
- [x] `Domain/Aggregates/BillingAccount.cs` — `DealId`, `LandlordUserId`, `TenantUserId`, `Status` (Inactive/Active/Suspended/Closed), `StartDate`, `EndDate`, `StripeCustomerId`, `StripeSubscriptionId`
- [x] `Domain/Entities/Invoice.cs` — `BillingAccountId`, `StripeInvoiceId`, `PeriodStart`, `PeriodEnd`, `AmountCents`, `ProrationDays`, `Status` (Pending/Paid/Failed/Disputed)
- [x] `Domain/ValueObjects/Money.cs` — `AmountCents` (int), `Currency` (string, default "USD")
- [x] `Domain/ValueObjects/ProrationWindow.cs` — computes days from start/end: `days × (7900 / 30)` cents
- [x] `Domain/Policies/BillingPolicy.cs` — $79/month = 7900 cents; prorated = `7900 / 30 × daysOccupied`; pilot discount = $39 for VerifiedInstitutionalPartner
- [x] `Domain/Events/DealActivatedEvent.cs`
- [x] `Domain/Events/PaymentFailedEvent.cs` — triggers protocol protection suspension
- [x] `Domain/Events/BillingStoppedEvent.cs`

#### Application
- [x] `Application/Commands/SubmitApplicationCommand.cs` + handler — tenant applies to listing; creates `DealApplication`
- [x] `Application/Commands/ApproveDealApplicationCommand.cs` + handler — landlord approves; accepts `DepositAmountCents` + `StayDurationDays`; validates deposit ≤ listing's `MaxDepositCents`; calculates insurance fee via `IInsuranceFeeCalculator`; checks for jurisdiction warnings via `JurisdictionWarningService`; generates `DealId`; fires `ApplicationApprovedEvent`
- [x] `Application/Commands/RejectDealApplicationCommand.cs` + handler
- [x] `Application/Commands/ActivateDealCommand.cs` + handler — gates: Truth Surface Confirmed + Payment Confirmed (or dispute resolved valid) + Insurance Active + `DealPaymentConfirmation.HostPaidPlatform == true`; fires `DealActivatedEvent`
- [x] `Application/Commands/StopBillingCommand.cs` + handler — cancels Stripe subscription; fires `BillingStoppedEvent`
- [x] `Application/Commands/RecordPaymentSucceededCommand.cs` + handler — from Stripe webhook
- [x] `Application/Commands/RecordPaymentFailedCommand.cs` + handler — suspends protocol protections
- [x] `Application/Commands/HandleChargebackNoticeCommand.cs` + handler
- [x] `Application/Commands/CreateStripeCustomerCommand.cs` + handler — creates Stripe customer for landlord on first deal
- [x] `Application/Queries/GetDealBillingStatusQuery.cs` + handler
- [x] `Application/Queries/GetProrationQuoteQuery.cs` + handler
- [x] `Application/Queries/GetApplicationStatusQuery.cs` + handler
- [x] `Application/Queries/ListApplicationsForListingQuery.cs` + handler
- [x] `Application/DTOs/BillingStatusDto.cs`
- [x] `Application/DTOs/ProrationQuoteDto.cs`
- [x] `Application/DTOs/DealApplicationDto.cs`
- [x] `Application/Mapping/BillingMappings.cs`

#### Application — Payment Confirmation
- [x] `Application/Commands/ConfirmPaymentCommand.cs` + handler — host confirms receipt; calls `DealPaymentConfirmation.ConfirmByHost(IClock)`; outbox fires `PaymentConfirmedEvent`
- [x] `Application/Commands/ConfirmHostPlatformPaymentCommand.cs` + handler — host confirms platform fee payment (insurance + activation fee); calls `DealPaymentConfirmation.ConfirmHostPlatformPayment(IClock)`; required before `ActivateDealCommand` can proceed
- [x] `Application/Commands/DisputePaymentCommand.cs` + handler — tenant disputes non-confirmation; requires `DisputeReason` + optional `EvidenceManifestId`; calls `DealPaymentConfirmation.DisputeByTenant(...)`; outbox fires `PaymentDisputedEvent`
- [x] `Application/Commands/ResolvePaymentDisputeCommand.cs` + handler — **Ops/Admin only**; reviews evidence; calls `DealPaymentConfirmation.ResolveDispute(paymentValid, IClock)`; outbox fires `PaymentDisputeResolvedEvent`
- [x] `Application/Queries/GetPaymentDetailsForTenantQuery.cs` + handler — verifies deal is in `AwaitingPaymentConfirmation` state; fetches host payment details via `IHostPaymentDetailsProvider` (cross-module); returns decrypted plaintext over HTTPS
- [x] `Application/Queries/GetPaymentConfirmationStatusQuery.cs` + handler — returns current status and timestamps
- [x] `Application/DTOs/PaymentDetailsDto.cs` — `PaymentInfoPlain` (decrypted on server)
- [x] `Application/DTOs/PaymentConfirmationDto.cs` — `DealId`, `Status`, `HostConfirmedAt?`, `TenantDisputedAt?`, `GracePeriodExpiresAt`, `DisputeReason?`, `TotalTenantPaymentCents`, `TotalHostPlatformPaymentCents`, `HostPaidPlatform`, `HostPaidPlatformAt?`
- [x] `Application/DTOs/DealApplicationDto.cs` — includes `DepositAmountCents`, `InsuranceFeeCents`, `FirstMonthRentCents`, `PartnerOrganizationId?`, `IsPartnerReferred`, `JurisdictionWarning?`

#### Application — Payment Confirmation Event Handlers
- [x] `Application/EventHandlers/OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler.cs` — listens to `TruthSurfaceConfirmedEvent`; fetches `DealApplication` to calculate `TotalTenantPaymentCents` and `TotalHostPlatformPaymentCents`; creates `DealPaymentConfirmation.Create(dealId, IClock, totalTenantPayment, totalHostPlatformPayment)` with 72h grace period
- [x] `Application/EventHandlers/OnPaymentConfirmedActivateDealHandler.cs` — listens to `PaymentConfirmedEvent`; sends `ActivateDealCommand` via MediatR
- [x] `Application/EventHandlers/OnPaymentDisputeResolvedHandler.cs` — listens to `PaymentDisputeResolvedEvent`; if valid, sends `ActivateDealCommand`; if rejected, sends notification

#### Presentation
- [x] `Presentation/Endpoints/ApplicationEndpoints.cs`
- [x] `Presentation/Endpoints/ActivationEndpoints.cs`
- [x] `Presentation/Endpoints/BillingEndpoints.cs`
- [x] `Presentation/Endpoints/PaymentConfirmationEndpoints.cs` — `MapPaymentConfirmationEndpoints()`:
  - `GET /v1/deals/{dealId}/payment/details` — tenant only; returns decrypted host payment details
  - `GET /v1/deals/{dealId}/payment/status` — both parties; returns payment confirmation status
  - `POST /v1/deals/{dealId}/payment/confirm` — host only; confirms receipt
  - `POST /v1/deals/{dealId}/payment/confirm-platform-payment` — host only; confirms platform fee payment (insurance + activation)
  - `POST /v1/deals/{dealId}/payment/dispute` — tenant only; disputes non-confirmation with reason + evidence
  - `POST /v1/admin/deals/{dealId}/resolve-payment-dispute` — Ops/Admin only; resolves dispute
- [x] `Presentation/Contracts/SubmitApplicationRequest.cs`
- [x] `Presentation/Contracts/ApproveApplicationRequest.cs` — `DepositAmountCents`, `StayDurationDays`
- [x] `Presentation/Contracts/ActivateDealRequest.cs`
- [x] `Presentation/Contracts/BillingStatusResponse.cs`
- [x] `Presentation/Contracts/DisputePaymentRequest.cs` — `Reason`, `EvidenceManifestId?`
- [x] `Presentation/Contracts/ResolvePaymentDisputeRequest.cs` — `PaymentValid` (bool)

#### Infrastructure
- [x] `Infrastructure/Persistence/BillingDbContext.cs` — schema `activation_billing`
- [x] `Infrastructure/Persistence/Schemas/billing.schema.sql`
- [x] `Infrastructure/Repositories/DealApplicationRepository.cs`
- [x] `Infrastructure/Repositories/BillingAccountRepository.cs`
- [x] `Infrastructure/Repositories/InvoiceRepository.cs`
- [x] `Infrastructure/Configurations/DealApplicationConfiguration.cs`
- [x] `Infrastructure/Configurations/BillingAccountConfiguration.cs`
- [x] `Infrastructure/Configurations/InvoiceConfiguration.cs`
- [x] `Infrastructure/Handlers/StripeWebhookHandler.cs` — validates Stripe signature; dispatches `RecordPaymentSucceededCommand` / `RecordPaymentFailedCommand` / `HandleChargebackNoticeCommand`
- [x] `Infrastructure/Jobs/BillingReconciliationJob.cs` — daily: retry failed invoices, reconcile Stripe subscription state
- [x] `Infrastructure/Repositories/DealPaymentConfirmationRepository.cs` — implements `IDealPaymentConfirmationRepository`; includes `GetPendingExpiredAsync` for timeout job
- [x] `Infrastructure/Configurations/DealPaymentConfirmationConfiguration.cs` — table `deal_payment_confirmations` in schema `activation_billing`; `DealId` unique index; `Status` stored as string; includes `TotalTenantPaymentCents`, `TotalHostPlatformPaymentCents`, `HostPaidPlatform`, `HostPaidPlatformAt`
- [x] `Infrastructure/Jobs/PaymentConfirmationTimeoutJob.cs` — Quartz, hourly: queries `GetPendingExpiredAsync(cutoff = now - 72h)`; sends reminder notifications; escalates to ops queue for stale confirmations
- [x] EF Core migrations
- [x] EF Core migration: `AddPaymentConfirmation` — adds `deal_payment_confirmations` table

#### Module Registration
- [x] `ActivationAndBillingModuleRegistration.cs`
- [x] Update registration: add `IDealPaymentConfirmationRepository`, `PaymentConfirmationTimeoutJob` (hourly Quartz), `IHostPaymentDetailsProvider` (resolved from DI), event handlers for `TruthSurfaceConfirmedEvent`, `PaymentConfirmedEvent`, `PaymentDisputeResolvedEvent`

---

### 5.2 IdentityAndVerification

#### Project & References
- [x] `IdentityAndVerification.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/IdentityProfile.cs` — `UserId`, `FirstName`, `LastName`, `DateOfBirth`, `Status` (VerificationStatus enum)
- [x] `Domain/Aggregates/VerificationCase.cs` — `UserId`, `PersonaInquiryId`, `Status`, `CompletedAt`
- [x] `Domain/Entities/BackgroundCheckReport.cs` — `UserId`, `PersonaReportId`, `Result` (Pass/Review/Fail), `ReceivedAt`, `ExpiresAt` (7-year retention)
- [x] `Domain/Entities/AffiliationVerification.cs` — `UserId`, `OrganizationType`, `OrganizationId`, `VerificationMethod` (OAuth/DomainEmail/PartnerAPI), `VerifiedAt`
- [x] `Domain/Entities/HostPaymentDetails.cs` — `Entity<Guid>`: `HostUserId` (unique index), `EncryptedPaymentInfo` (AES-256 via `IEncryptionService`), `UpdatedAt`; methods: `Create(hostUserId, encryptedInfo, IClock)`, `Update(encryptedInfo, IClock)`
- [x] `Domain/ValueObjects/VerificationStatus.cs` — enum: NotStarted, Pending, Verified, Failed, ManualReviewRequired
- [x] `Domain/ValueObjects/VerificationClass.cs` — enum: Low, Medium, High (computed in VerificationAndRisk module)
- [x] `Domain/ValueObjects/ConfidenceIndicator.cs` — High/Medium/Low + reason text
- [x] `Domain/Events/IdentityVerifiedEvent.cs`
- [x] `Domain/Events/IdentityVerificationFailedEvent.cs`
- [x] `Domain/Events/BackgroundCheckReceivedEvent.cs`
- [x] `Domain/Events/AffiliationVerifiedEvent.cs`
- [x] `Domain/Events/FraudFlagRaisedEvent.cs`
- [x] `Domain/Events/VerificationClassChangedEvent.cs`

#### Application
- [x] `Application/Commands/StartKycCommand.cs` + handler — calls `IPersonaClient.CreateInquiry`
- [x] `Application/Commands/CompleteKycCommand.cs` + handler — processes Persona webhook; updates status
- [x] `Application/Commands/SubmitBackgroundCheckConsentCommand.cs` + handler — FCRA consent flow; calls Persona background check API
- [x] `Application/Commands/IngestBackgroundCheckResultCommand.cs` + handler — Persona webhook ingestion
- [x] `Application/Commands/VerifyInstitutionAffiliationCommand.cs` + handler — OAuth/domain-email gating; unverified claims discarded + flagged
- [x] `Application/Commands/CreateFraudFlagCommand.cs` + handler
- [x] `Application/Commands/SaveHostPaymentDetailsCommand.cs` + handler — encrypts payment info via `IEncryptionService.Encrypt()`; upserts `HostPaymentDetails`
- [x] `Application/Queries/GetVerificationStatusQuery.cs` + handler
- [x] `Application/Queries/GetFraudFlagsQuery.cs` + handler
- [x] `Application/DTOs/VerificationStatusDto.cs`
- [x] `Application/DTOs/FraudFlagDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/IdentityEndpoints.cs`
- [x] `Presentation/Endpoints/VerificationEndpoints.cs`
- [x] `Presentation/Endpoints/PersonaWebhookEndpoints.cs`
- [x] `Presentation/Endpoints/HostPaymentEndpoints.cs` — `MapHostPaymentEndpoints()`:
  - `PUT /v1/hosts/payment-details` — host saves/updates their payment details (encrypted at rest)
  - `GET /v1/hosts/payment-details` — host views their own (decrypted) payment details
- [x] `Presentation/Contracts/StartKycRequest.cs`
- [x] `Presentation/Contracts/VerificationStatusResponse.cs`
- [x] `Presentation/Contracts/SavePaymentDetailsRequest.cs` — `PaymentInfo` (JSON: bank name, account number, routing, notes)

#### Infrastructure
- [x] `Infrastructure/Persistence/IdentityDbContext.cs` — schema `identity`
- [x] `Infrastructure/Persistence/Schemas/identity.schema.sql`
- [x] `Infrastructure/Repositories/IdentityProfileRepository.cs`
- [x] `Infrastructure/Repositories/VerificationCaseRepository.cs`
- [x] `Infrastructure/Repositories/HostPaymentDetailsRepository.cs` — `GetByHostIdAsync(Guid hostUserId)`
- [x] `Infrastructure/Configurations/IdentityProfileConfiguration.cs`
- [x] `Infrastructure/Configurations/VerificationCaseConfiguration.cs`
- [x] `Infrastructure/Configurations/HostPaymentDetailsConfiguration.cs` — table `host_payment_details` in schema `identity`; `HostUserId` unique index
- [x] `Infrastructure/Services/HostPaymentDetailsProvider.cs` — implements `IHostPaymentDetailsProvider` (from SharedKernel); fetches `HostPaymentDetails` by userId, decrypts via `IEncryptionService`, returns `HostPaymentDetailsDto`
- [x] `Infrastructure/Handlers/PersonaWebhookHandler.cs` — validates Persona HMAC signature; dispatches complete/fail commands
- [x] `Infrastructure/Jobs/FraudFlagSlaMonitorJob.cs` — every 15 min: escalate unresolved High-severity flags past 24h
- [x] EF Core migrations (identity_profiles, verification_cases, background_check_reports, affiliation_verifications)
- [x] EF Core migration: `AddHostPaymentDetails` — adds `host_payment_details` table

#### Module Registration
- [x] `IdentityVerificationModuleRegistration.cs`
- [x] Update registration: add `HostPaymentDetailsRepository`, `IHostPaymentDetailsProvider` → `HostPaymentDetailsProvider`

---

### 5.3 InsuranceIntegration

#### Project & References
- [x] `InsuranceIntegration.csproj` — references SharedKernel, Infrastructure, ActivationAndBilling
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/InsurancePolicyRecord.cs` — `TenantUserId`, `DealId`, `State` (InsuranceState enum), `Provider`, `PolicyNumber`, `VerifiedAt`, `ExpiresAt`, `CoverageScope`
- [x] `Domain/Entities/InsuranceVerificationAttempt.cs` — `PolicyRecordId`, `AttemptedAt`, `Result`, `Source` (API/ManualUpload)
- [x] `Domain/ValueObjects/InsuranceState.cs` — enum: NotActive, Active, InstitutionBacked, Unknown
- [x] `Domain/ValueObjects/CoverageRequirements.cs` — minimum coverage type + amount (pulled from listing)
- [x] `Domain/Policies/UnknownGraceWindowPolicy.cs` — 72h grace: API failure → Unknown (not lapsed); tenant inaction past 72h → lapse violation; partner failure past 72h → manual review (no violation)
- [x] `Domain/Events/InsuranceStatusChangedEvent.cs`
- [x] `Domain/Events/InsuranceUnknownSlaBreachedEvent.cs`

#### Application
- [x] `Application/Commands/StartInsuranceVerificationCommand.cs` + handler
- [x] `Application/Commands/RecordInsuranceActiveCommand.cs` + handler
- [x] `Application/Commands/RecordInsuranceNotActiveCommand.cs` + handler
- [x] `Application/Commands/RecordInsuranceUnknownCommand.cs` + handler — starts 72h grace timer; notifies both parties via `IEmailService`
- [x] `Application/Commands/UploadManualProofCommand.cs` + handler — uploads to MinIO; notifies ops team
- [x] `Application/Commands/HandleInsurancePurchaseWebhookCommand.cs` + handler
- [x] `Application/Commands/CompleteManualVerificationCommand.cs` + handler — ops team confirms within 24h SLA
- [x] `Application/Queries/GetInsuranceStatusQuery.cs` + handler
- [x] `Application/Queries/GetPartnerQuotationsQuery.cs` + handler
- [x] `Application/Queries/GetInsuranceUnknownQueueQuery.cs` + handler — admin ops queue
- [x] `Application/DTOs/InsuranceStatusDto.cs`
- [x] `Application/DTOs/InsuranceQueueItemDto.cs`

#### Application — Insurance Fee Calculation
- [x] `Application/Services/ConfigurableInsuranceFeeCalculator.cs` — implements `IInsuranceFeeCalculator` (SharedKernel); formula: `monthlyRentCents × feeRate × ceil(stayDurationDays / 30)`; configurable rate from `Insurance:FeeRatePerMonth` (default 0.05 = 5%); provider: `"Configurable"`
- [x] `Application/Services/ApiInsuranceFeeCalculator.cs` — implements `IInsuranceFeeCalculator`; calls external insurance API at `Insurance:ApiBaseUrl/v1/quotes` via `IHttpClientFactory` (named client `"InsurancePartner"`); returns `InsuranceFeeQuote` from API response

#### Application — Event Handlers (Insurance Activation)
- [x] `Application/EventHandlers/OnDealActivatedActivateInsuranceHandler.cs` — listens to `DealActivatedEvent`; sends `RecordInsuranceActiveCommand` with `CoverageScope: "Platform-managed"`; automatically activates insurance when a deal activates

#### Presentation
- [x] `Presentation/Endpoints/InsuranceEndpoints.cs`
- [x] `Presentation/Endpoints/InsuranceWebhookEndpoints.cs`
- [x] `Presentation/Contracts/ManualProofUploadRequest.cs`
- [x] `Presentation/Contracts/InsuranceStatusResponse.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/InsuranceDbContext.cs` — schema `insurance`
- [x] `Infrastructure/Persistence/Schemas/insurance.schema.sql`
- [x] `Infrastructure/Repositories/InsurancePolicyRecordRepository.cs`
- [x] `Infrastructure/Configurations/InsurancePolicyRecordConfiguration.cs`
- [x] `Infrastructure/Jobs/InsurancePollerJob.cs` — hourly: polls active policies via `IInsuranceApiClient`
- [x] `Infrastructure/Jobs/InsuranceUnknownSlaJob.cs` — every 30 min: fires `InsuranceUnknownSlaBreachedEvent` at 72h
- [x] EF Core migrations

#### Module Registration
- [x] `InsuranceIntegrationModuleRegistration.cs` — conditionally registers `ApiInsuranceFeeCalculator` (if `Insurance:ApiBaseUrl` configured) or `ConfigurableInsuranceFeeCalculator` (fallback) as `IInsuranceFeeCalculator`; registers `OnDealActivatedActivateInsuranceHandler`

---

### 5.4 ListingAndLocation

#### Project & References
- [x] `ListingAndLocation.csproj`
- [x] Add to `.sln`

#### Domain — Core Aggregate
- [x] `Domain/Aggregates/Listing.cs` — `LandlordUserId`, `Status` (Draft/Published/Activated/Closed), `StayRange`, `MonthlyRentCents`, `InsuranceRequired`, `ApproxGeoPoint`, `PreciseAddress` (AES-256 encrypted at rest via EF Core value converter), `JurisdictionCode`, `MaxDepositCents` (long, set by host at listing creation), `SuggestedDepositLowCents` (long?, optional), `SuggestedDepositHighCents` (long?, optional); `UpdateSuggestedDeposit()` sets the recommended deposit range (from VerificationAndRisk); owns `HouseRules` (value object), `CancellationPolicy` (value object); has navigation collections: `Amenities` (`IReadOnlyList<ListingAmenity>`), `SafetyDevices` (`IReadOnlyList<ListingSafetyDevice>`), `Considerations` (`IReadOnlyList<ListingConsideration>`)
- [x] `Domain/ValueObjects/Address.cs` — street, city, state, zip, country
- [x] `Domain/ValueObjects/GeoPoint.cs` — `Lat`, `Lon`; used for approx pin
- [x] `Domain/ValueObjects/StayRange.cs` — `MinDays`, `MaxDays`; validator: 30 ≤ min ≤ max ≤ 180
- [x] `Domain/Events/ListingPublishedEvent.cs`
- [x] `Domain/Events/ListingActivatedEvent.cs`
- [x] `Domain/Events/PreciseAddressLockedEvent.cs` — carries `JurisdictionCode` for downstream gating

#### Domain — Amenities & Features (Admin-Managed Reference Data)

> Amenities, safety devices, and considerations are admin-managed reference data stored in the DB. Hosts select from these when creating/editing a listing. Each definition has an `IconKey` field that maps to a [Lucide](https://lucide.dev) icon name for frontend rendering.

- [x] `Domain/Entities/AmenityDefinition.cs` — `Id` (Guid), `Name` (string), `Category` (AmenityCategory), `IconKey` (string, e.g. `"wifi"`, `"car"`, `"utensils"`, `"snowflake"`), `IsActive` (bool), `SortOrder` (int); admin-managed; no business logic — pure reference data
- [x] `Domain/Entities/ListingAmenity.cs` — join entity: `ListingId` (Guid FK), `AmenityDefinitionId` (Guid FK); many-to-many between Listing and AmenityDefinition
- [x] `Domain/Enums/AmenityCategory.cs` — enum: `Kitchen`, `Bathroom`, `Bedroom`, `LivingArea`, `Outdoor`, `Parking`, `Entertainment`, `WorkSpace`, `Accessibility`, `Laundry`, `ClimateControl`, `Internet`

#### Domain — Safety Devices & Considerations (Admin-Managed Reference Data)

- [x] `Domain/Entities/SafetyDeviceDefinition.cs` — `Id`, `Name`, `IconKey` (e.g. `"shield-check"`, `"flame"`, `"siren"`), `IsActive`, `SortOrder`
- [x] `Domain/Entities/ListingSafetyDevice.cs` — join entity: `ListingId`, `SafetyDeviceDefinitionId`
- [x] `Domain/Entities/PropertyConsiderationDefinition.cs` — `Id`, `Name`, `IconKey` (e.g. `"stairs"`, `"waves"`, `"camera"`), `IsActive`, `SortOrder`
- [x] `Domain/Entities/ListingConsideration.cs` — join entity: `ListingId`, `ConsiderationDefinitionId`

#### Domain — Cancellation Policy (Per-Listing)

> Each listing can have its own cancellation policy. The host selects a type (Flexible/Moderate/Strict/NonRefundable) and the system applies defaults. The host can also adjust values. Jurisdictions can enforce minimum cancellation windows via JurisdictionPacks.

- [x] `Domain/ValueObjects/CancellationPolicy.cs` — owned value object on Listing:
  - `Type` (CancellationPolicyType enum)
  - `FreeCancellationDays` (int) — full refund if cancelled this many days before check-in
  - `PartialRefundPercent` (int?) — % refund within partial window
  - `PartialRefundDays` (int?) — days before check-in for partial refund
  - `CustomTerms` (string?) — only for Custom type; max 2000 chars
- [x] `Domain/Enums/CancellationPolicyType.cs` — enum: `Flexible`, `Moderate`, `Strict`, `NonRefundable`, `Custom`
- [x] `Domain/Policies/CancellationPolicyDefaults.cs` — static defaults per type:
  - Flexible: full refund 7 days before, 50% up to 3 days
  - Moderate: full refund 14 days before, 50% up to 7 days
  - Strict: full refund 30 days before, 50% up to 14 days
  - NonRefundable: no refund (may be restricted by jurisdiction)

#### Domain — House Rules (Per-Listing)

> Structured house rules per listing. Not freeform text (except `LeavingInstructions` and `AdditionalRules`). Displayed with icons on the property detail page.

- [x] `Domain/ValueObjects/HouseRules.cs` — owned value object on Listing:
  - `CheckInTime` (TimeOnly) — e.g. 15:00; icon: `"log-in"`
  - `CheckOutTime` (TimeOnly) — e.g. 11:00; icon: `"log-out"`
  - `MaxGuests` (int) — max occupants; icon: `"users"`
  - `PetsAllowed` (bool) — icon: `"dog"`
  - `PetsNotes` (string?) — breed/size/weight restrictions; max 500 chars
  - `SmokingAllowed` (bool) — icon: `"cigarette"`
  - `PartiesAllowed` (bool) — icon: `"party-popper"`
  - `QuietHoursStart` (TimeOnly?) — icon: `"moon"`
  - `QuietHoursEnd` (TimeOnly?) — icon: `"sun"`
  - `LeavingInstructions` (string?) — instructions for checkout day; icon: `"clipboard-check"`; max 2000 chars
  - `AdditionalRules` (string?) — catch-all; max 2000 chars

#### Application — Core Commands & Queries
- [x] `Application/Commands/CreateListingCommand.cs` + handler — structured fields only; no freeform text; includes `MaxDepositCents`, `AmenityIds`, `SafetyDeviceIds`, `ConsiderationIds`, `HouseRules`, `CancellationPolicy`
- [x] `Application/Commands/UpdateListingCommand.cs` + handler — includes `MaxDepositCents`, `AmenityIds`, `SafetyDeviceIds`, `ConsiderationIds`, `HouseRules`, `CancellationPolicy`
- [x] `Application/Commands/PublishListingCommand.cs` + handler — calls jurisdiction compliance validation gate before publish; validates cancellation policy against jurisdiction minimum cancellation windows
- [x] `Application/Commands/SetApproxLocationCommand.cs` + handler — stores approx `GeoPoint` (Google Maps geocode of rough area)
- [x] `Application/Commands/LockPreciseAddressOnActivationCommand.cs` + handler — encrypts + stores `PreciseAddress`; calls `IGeocodingService.ResolveJurisdiction`; fires `PreciseAddressLockedEvent`
- [x] `Application/Queries/SearchListingsQuery.cs` + handler — filter by approx location radius, stay range, price range, amenity IDs; non-promotional
- [x] `Application/Queries/GetListingDetailsQuery.cs` + handler — returns approx pin pre-activation, decrypted address post-activation (only to authorized parties); includes full amenity/safety/consideration lists with names and icon keys, house rules, cancellation policy
- [x] `Application/DTOs/ListingSummaryDto.cs`
- [x] `Application/DTOs/ListingDetailsDto.cs` — includes `MaxDepositCents`, `SuggestedDepositLowCents`, `SuggestedDepositHighCents`, `Amenities` (list with name + iconKey), `SafetyDevices` (list), `Considerations` (list), `HouseRules`, `CancellationPolicy`
- [x] `Application/Commands/ListingMapper.cs` — maps deposit fields, amenities, safety, considerations, house rules, cancellation from aggregate to DTO

#### Application — Reference Data Admin Commands
- [x] `Application/Commands/Admin/CreateAmenityDefinitionCommand.cs` + handler — PlatformAdmin only; creates amenity with name, category, iconKey, sortOrder
- [x] `Application/Commands/Admin/UpdateAmenityDefinitionCommand.cs` + handler — PlatformAdmin only; update name, category, iconKey, isActive, sortOrder
- [x] `Application/Commands/Admin/CreateSafetyDeviceDefinitionCommand.cs` + handler
- [x] `Application/Commands/Admin/UpdateSafetyDeviceDefinitionCommand.cs` + handler
- [x] `Application/Commands/Admin/CreateConsiderationDefinitionCommand.cs` + handler
- [x] `Application/Commands/Admin/UpdateConsiderationDefinitionCommand.cs` + handler
- [x] `Application/Queries/ListAmenityDefinitionsQuery.cs` + handler — public; returns active definitions ordered by category and sortOrder; includes iconKey
- [x] `Application/Queries/ListSafetyDeviceDefinitionsQuery.cs` + handler — public; returns active definitions
- [x] `Application/Queries/ListConsiderationDefinitionsQuery.cs` + handler — public; returns active definitions
- [x] `Application/DTOs/AmenityDefinitionDto.cs` — `Id`, `Name`, `Category`, `IconKey`
- [x] `Application/DTOs/SafetyDeviceDefinitionDto.cs` — `Id`, `Name`, `IconKey`
- [x] `Application/DTOs/ConsiderationDefinitionDto.cs` — `Id`, `Name`, `IconKey`
- [x] `Application/DTOs/HouseRulesDto.cs` — all fields from value object
- [x] `Application/DTOs/CancellationPolicyDto.cs` — `Type`, `FreeCancellationDays`, `PartialRefundPercent`, `PartialRefundDays`, `CustomTerms`
- [x] `Application/DTOs/ListingAmenityDto.cs` — `Id`, `Name`, `Category`, `IconKey`
- [x] `Application/DTOs/ListingSafetyDeviceDto.cs` — `Id`, `Name`, `IconKey`
- [x] `Application/DTOs/ListingConsiderationDto.cs` — `Id`, `Name`, `IconKey`

#### Presentation
- [x] `Presentation/Endpoints/ListingEndpoints.cs`
- [x] `Presentation/Endpoints/LocationEndpoints.cs`
- [x] `Presentation/Endpoints/ListingDefinitionsEndpoints.cs` — `MapListingDefinitionsEndpoints()`:
  - `GET /v1/listing-definitions/amenities` — authorized; active amenity definitions ordered by category
  - `GET /v1/listing-definitions/safety-devices` — authorized; active safety device definitions
  - `GET /v1/listing-definitions/considerations` — authorized; active consideration definitions
- [x] `Presentation/Endpoints/AdminListingDefinitionsEndpoints.cs` — `MapAdminListingDefinitionsEndpoints()`:
  - `GET /v1/admin/listing-definitions/amenities` — PlatformAdmin; all amenities (including inactive)
  - `POST /v1/admin/listing-definitions/amenities` — PlatformAdmin; create amenity
  - `PUT /v1/admin/listing-definitions/amenities/{id}` — PlatformAdmin; update/deactivate amenity
  - `GET /v1/admin/listing-definitions/safety-devices` — PlatformAdmin; all safety devices
  - `POST /v1/admin/listing-definitions/safety-devices` — PlatformAdmin; create safety device
  - `PUT /v1/admin/listing-definitions/safety-devices/{id}` — PlatformAdmin; update/deactivate
  - `GET /v1/admin/listing-definitions/considerations` — PlatformAdmin; all considerations
  - `POST /v1/admin/listing-definitions/considerations` — PlatformAdmin; create consideration
  - `PUT /v1/admin/listing-definitions/considerations/{id}` — PlatformAdmin; update/deactivate
- [x] `Presentation/Contracts/CreateListingRequest.cs` — includes `AmenityIds`, `SafetyDeviceIds`, `ConsiderationIds`, `HouseRules` (HouseRulesRequest), `CancellationPolicy` (CancellationPolicyRequest)
- [x] `Presentation/Contracts/UpdateListingRequest.cs` — includes `AmenityIds`, `SafetyDeviceIds`, `ConsiderationIds`, `HouseRules`, `CancellationPolicy`
- [x] `Presentation/Contracts/ListingDetailsResponse.cs`
- [x] Admin request contracts defined inline in `AdminListingDefinitionsEndpoints.cs`: `CreateAmenityRequest`, `UpdateAmenityRequest`, `CreateSafetyDeviceRequest`, `UpdateSafetyDeviceRequest`, `CreateConsiderationRequest`, `UpdateConsiderationRequest`
- [x] `Presentation/Contracts/HouseRulesRequest.cs` — all HouseRules fields; defined in `CreateListingRequest.cs`
- [x] `Presentation/Contracts/CancellationPolicyRequest.cs` — `Type`, `FreeCancellationDays`, `PartialRefundPercent`, `PartialRefundDays`, `CustomTerms`; defined in `CreateListingRequest.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/ListingsDbContext.cs` — schema `listings`; `DbSet`s for `AmenityDefinition`, `SafetyDeviceDefinition`, `PropertyConsiderationDefinition`
- [x] `Infrastructure/Persistence/Schemas/listings.schema.sql`
- [x] `Infrastructure/Adapters/GeocodingClientAdapter.cs` — wraps `IGeocodingService`; AES-256 value converter for `PreciseAddress` column via EF Core `ValueConverter`
- [x] `Infrastructure/Repositories/ListingRepository.cs`
- [x] `Infrastructure/Configurations/ListingConfiguration.cs` — configures encrypted column, `MaxDepositCents`, `SuggestedDepositLowCents`, `SuggestedDepositHighCents`; `OwnsOne(HouseRules)`, `OwnsOne(CancellationPolicy)`; has many `ListingAmenity`, `ListingSafetyDevice`, `ListingConsideration`
- [x] `Infrastructure/Configurations/AmenityDefinitionConfiguration.cs` — table `amenity_definitions`; unique index on `Name`; composite index on (`Category`, `SortOrder`)
- [x] `Infrastructure/Configurations/ListingAmenityConfiguration.cs` — table `listing_amenities`; composite key (`ListingId`, `AmenityDefinitionId`); FK to `AmenityDefinition` with `Restrict` delete
- [x] `Infrastructure/Configurations/SafetyDeviceDefinitionConfiguration.cs` — table `safety_device_definitions`; unique index on `Name`
- [x] `Infrastructure/Configurations/ListingSafetyDeviceConfiguration.cs` — table `listing_safety_devices`; composite key; FK with `Restrict` delete
- [x] `Infrastructure/Configurations/ConsiderationDefinitionConfiguration.cs` — table `consideration_definitions`; unique index on `Name`
- [x] `Infrastructure/Configurations/ListingConsiderationConfiguration.cs` — table `listing_considerations`; composite key; FK with `Restrict` delete
- [x] `Infrastructure/Jobs/JurisdictionResolutionJob.cs` — nightly sweep: re-derives `JurisdictionCode` for any listing missing it
- [x] `Infrastructure/Seeding/ListingDefinitionsSeeder.cs` — seeds initial amenity definitions (30 items across 12 categories with Lucide `IconKey`), safety device definitions (10 items), consideration definitions (10 items); idempotent (skips if data exists)
- [x] EF Core migrations
- [x] EF Core migration: `AddListingAttributesAndPolicies` — adds amenity/safety/consideration definition tables, join tables, `HouseRules` + `CancellationPolicy` owned columns

#### Seed Data — Amenity Definitions (initial set, expandable by admin)

> All definitions include `IconKey` mapping to Lucide icon names. Admins can add, rename, reorder, or deactivate definitions at any time without affecting existing listing selections.

| Category | Examples | IconKey examples |
|---|---|---|
| Kitchen | Dishwasher, Oven, Microwave, Refrigerator, Coffee maker, Toaster | `utensils`, `cooking-pot`, `microwave`, `refrigerator`, `coffee`, `sandwich` |
| Bathroom | Bathtub, Walk-in shower, Hair dryer, Washer, Dryer | `bath`, `shower-head`, `wind`, `shirt`, `flame` |
| Bedroom | King bed, Queen bed, Workspace desk, Closet, Blackout curtains | `bed-double`, `bed-single`, `monitor`, `door-closed`, `blinds` |
| LivingArea | TV, Streaming services, Fireplace, Sofa, Bookshelf | `tv`, `play`, `flame`, `sofa`, `book-open` |
| Outdoor | Balcony, Patio, Garden, BBQ grill, Pool, Hot tub | `fence`, `trees`, `flower-2`, `beef`, `waves`, `thermometer` |
| Parking | Garage, Driveway, Street parking, EV charger | `car`, `square-parking`, `map-pin`, `plug-zap` |
| Entertainment | Game console, Board games, Pool table, Gym | `gamepad-2`, `dice-5`, `circle-dot`, `dumbbell` |
| WorkSpace | Dedicated workspace, High-speed WiFi, Printer, Monitor | `laptop`, `wifi`, `printer`, `monitor` |
| Accessibility | Wheelchair access, Step-free entry, Wide doorways, Grab bars | `accessibility`, `door-open`, `move`, `grip-horizontal` |
| Laundry | In-unit washer, In-unit dryer, Shared laundry, Iron | `shirt`, `wind`, `building-2`, `iron` (custom) |
| ClimateControl | Central AC, Heating, Ceiling fans, Portable heater | `snowflake`, `thermometer-sun`, `fan`, `heater` |
| Internet | WiFi, Ethernet, Fiber optic | `wifi`, `cable`, `zap` |

#### Seed Data — Safety Device Definitions

| Name | IconKey |
|---|---|
| Smoke detector | `alarm-smoke` |
| Carbon monoxide detector | `cloud` |
| Fire extinguisher | `fire-extinguisher` |
| First aid kit | `heart-pulse` |
| Security cameras (exterior) | `camera` |
| Deadbolt lock | `lock` |
| Window locks | `lock-keyhole` |
| Safe / lockbox | `vault` |
| Emergency exit plan | `log-out` |
| Outdoor lighting | `lamp` |

#### Seed Data — Property Consideration Definitions

| Name | IconKey |
|---|---|
| Stairs / multi-level | `stairs` |
| Pool (unfenced) | `waves` |
| Hot tub | `thermometer` |
| Lake / river / water nearby | `droplets` |
| Security cameras on property | `camera` |
| Weapons on property | `shield-alert` |
| Shared spaces | `users` |
| Construction nearby | `construction` |
| Road noise | `volume-2` |
| Animals on property | `rabbit` |

#### Module Registration
- [x] `ListingAndLocationModuleRegistration.cs` — registers definition repositories and new endpoint groups

#### Part A — Property Details (PropertyType, Bedrooms, Bathrooms, Area)

> Every listing needs basic property attributes: what kind of property it is, how many bedrooms/bathrooms, and the size. These are required fields for search filtering and display.

##### Domain
- [x] `Domain/Enums/PropertyType.cs` — enum: `Apartment`, `House`, `Condo`, `Townhouse`, `Studio`, `Loft`, `Villa`, `Cottage`, `Cabin`, `Other`
- [x] `Domain/Aggregates/Listing.cs` — add properties:
  - `PropertyType` (PropertyType enum, required)
  - `Bedrooms` (int, >= 0; 0 = studio)
  - `Bathrooms` (decimal, >= 0.5; supports half-baths e.g. 1.5)
  - `SquareFootage` (int?, nullable; in sq ft)
- [x] Update `Create()` and `Update()` to accept new fields

##### Application
- [x] Update `CreateListingCommand` — add `PropertyType`, `Bedrooms`, `Bathrooms`, `SquareFootage?`
- [x] Update `UpdateListingCommand` — add same fields
- [x] Update `ListingDetailsDto` — add `PropertyType`, `Bedrooms`, `Bathrooms`, `SquareFootage?`
- [x] Update `ListingSummaryDto` — add `PropertyType`, `Bedrooms`, `Bathrooms`
- [x] Update `ListingMapper` — map new fields
- [x] Update `SearchListingsQuery` — add filters for `PropertyType`, `MinBedrooms`, `MinBathrooms`

##### Presentation
- [x] Update `CreateListingRequest` — add `PropertyType`, `Bedrooms`, `Bathrooms`, `SquareFootage?`
- [x] Update `UpdateListingRequest` — add same fields
- [x] Update `ListingEndpoints` — pass new fields to commands

##### Infrastructure
- [x] Update `ListingConfiguration.cs` — configure new columns
- [ ] EF Core migration: `AddPropertyDetailsToListing`

#### Part B — Calendar Availability & Date-Based Booking

> The platform must track which dates a listing is booked and prevent double-booking. Tenants request specific check-in/check-out dates, and the system validates availability before the application can be approved. Minimum stay: 1 month (30 days). Maximum stay: 6 months (180 days).

##### Domain — ListingAndLocation Module
- [x] `Domain/Entities/ListingAvailabilityBlock.cs` — entity owned by Listing:
  - `Id` (Guid), `ListingId` (Guid FK), `DealId` (Guid?, reference to the activated deal)
  - `CheckInDate` (DateOnly), `CheckOutDate` (DateOnly)
  - `BlockType` (enum: `Booked`, `HostBlocked`) — `Booked` = deal-activated; `HostBlocked` = host manually blocks dates
  - Factory methods: `CreateBooked()`, `CreateHostBlocked()`
- [x] `Domain/Enums/AvailabilityBlockType.cs` — enum: `Booked`, `HostBlocked`
- [x] `Domain/Aggregates/Listing.cs` — add navigation: `IReadOnlyList<ListingAvailabilityBlock> AvailabilityBlocks`
- [x] `Domain/Services/AvailabilityService.cs` — static domain service:
  - `IsAvailable(IReadOnlyList<ListingAvailabilityBlock> blocks, DateOnly checkIn, DateOnly checkOut): bool`
  - Returns false if requested range overlaps any existing block

##### Domain — ActivationAndBilling Module
- [x] `Domain/Aggregates/DealApplication.cs` — add:
  - `RequestedCheckIn` (DateOnly) — tenant's desired check-in date
  - `RequestedCheckOut` (DateOnly) — tenant's desired check-out date
  - `StayDurationDays` (int) — computed: `(CheckOut - CheckIn).Days`
- [x] Update `Submit()` — accept and validate dates (min 30 days, max 180 days, check-out > check-in)
- [x] Update `Approve()` — uses `StayDurationDays` from stored application dates

##### Application — ActivationAndBilling Module
- [x] Update `SubmitApplicationCommand` — add `RequestedCheckIn`, `RequestedCheckOut`; validates:
  - Duration within listing's `StayRange` (min/max)
  - Availability check via `AvailabilityService.IsAvailable()`
- [x] Update `ApproveDealApplicationCommand` — re-validates availability at approval time; uses `StayDurationDays` from application (removed `StayDurationDays` from command)
- [x] Update `ActivateDealCommand` handler — on activation, creates `ListingAvailabilityBlock` with `BlockType.Booked` for the deal's date range
- [x] Update `DealApplicationDto` — add `RequestedCheckIn`, `RequestedCheckOut`, `StayDurationDays`
- [x] Update all `MapToDto` in `GetApplicationStatusQuery`, `ListApplicationsForListingQuery`, `RejectDealApplicationCommand`

##### Application — ListingAndLocation Module
- [x] `Application/Queries/GetListingAvailabilityQuery.cs` — returns booked/blocked date ranges for a listing (public, for calendar display)
- [x] `Application/Commands/BlockDatesCommand.cs` — host manually blocks dates; validates no overlap with existing blocks
- [x] `Application/Commands/UnblockDatesCommand.cs` — host removes a manual block; prevents removal of `Booked` blocks
- [x] `Application/DTOs/AvailabilityBlockDto.cs` — `Id`, `CheckInDate`, `CheckOutDate`, `BlockType`

##### Presentation — ActivationAndBilling Module
- [x] Update `SubmitApplicationRequest` — add `RequestedCheckIn` (DateOnly), `RequestedCheckOut` (DateOnly)
- [x] Remove `StayDurationDays` from `ApproveApplicationRequest` (now derived from application dates)
- [x] Update `ApplicationEndpoints` — pass new fields in Submit; remove `StayDurationDays` from Approve

##### Presentation — ListingAndLocation Module
- [x] `Presentation/Contracts/BlockDatesRequest.cs` — `CheckInDate`, `CheckOutDate`
- [x] `ListingEndpoints` — add:
  - `GET /v1/listings/{id}/availability` — returns booked/blocked date ranges
  - `POST /v1/listings/{id}/block-dates` — host blocks dates (RequireLandlord)
  - `DELETE /v1/listings/{id}/block-dates/{blockId}` — host removes block (RequireLandlord)

##### Infrastructure
- [x] `ListingAvailabilityBlockConfiguration.cs` — table `listing_availability_blocks` in `listings` schema; index on `(ListingId, CheckInDate, CheckOutDate)`; index on `DealId` filtered
- [x] Update `ListingConfiguration.cs` — add `HasMany(AvailabilityBlocks)` with cascade delete and backing field
- [x] Update `ListingsDbContext` — add `DbSet<ListingAvailabilityBlock>`
- [x] Update `DealApplicationConfiguration.cs` — add `RequestedCheckIn`, `RequestedCheckOut`, `StayDurationDays` column configs
- [ ] EF Core migration: `AddListingAvailabilityBlocks`
- [ ] EF Core migration (ActivationAndBilling): `AddDatesToApplication` — adds `RequestedCheckIn`, `RequestedCheckOut`, `StayDurationDays` columns

#### Part C — Listing Photos / Media

> Hosts must be able to upload multiple photos for each listing. One photo is designated as the cover photo (shown in search results). Photos are ordered and can be reordered. Each photo stores a storage key (for cloud blob reference), a display URL, and an optional caption.

##### Domain
- [x] `Domain/Entities/ListingPhoto.cs` — entity owned by Listing:
  - `Id` (Guid), `ListingId` (Guid FK)
  - `StorageKey` (string, max 500) — blob storage reference
  - `Url` (string, max 2000) — display URL
  - `Caption` (string?, max 500) — optional caption
  - `IsCover` (bool) — only one photo per listing should be true
  - `SortOrder` (int) — 0-based ordering
  - Factory method: `Create(listingId, storageKey, url, caption, isCover, sortOrder)`
  - Mutators: `SetCaption()`, `SetCover()`, `SetSortOrder()`
- [x] Update `Listing` aggregate — add `_photos` backing field + `Photos` navigation property
- [x] `Listing.AddPhoto(storageKey, url, caption)` — first photo auto-set as cover; sort order auto-incremented
- [x] `Listing.RemovePhoto(photoId)` — removes photo; if it was cover, promotes first remaining photo; re-indexes sort orders
- [x] `Listing.SetCoverPhoto(photoId)` — clears IsCover on all others; sets target as cover
- [x] `Listing.ReorderPhotos(photoIdsInOrder)` — validates all IDs present; re-assigns sort orders

##### Application
- [x] `Application/DTOs/ListingPhotoDto.cs` — `Id`, `Url`, `Caption`, `IsCover`, `SortOrder`
- [x] `Application/Commands/AddListingPhotoCommand.cs` + handler — loads listing with photos, calls `AddPhoto()`, returns `ListingPhotoDto`
- [x] `Application/Commands/RemoveListingPhotoCommand.cs` + handler — loads listing with photos, calls `RemovePhoto()`
- [x] `Application/Commands/SetCoverPhotoCommand.cs` + handler — loads listing with photos, calls `SetCoverPhoto()`
- [x] `Application/Commands/ReorderPhotosCommand.cs` + handler — loads listing with photos, calls `ReorderPhotos()`
- [x] Update `ListingDetailsDto` — add `IReadOnlyList<ListingPhotoDto> Photos`
- [x] Update `ListingSummaryDto` — add `string? CoverPhotoUrl`
- [x] Update `ListingMapper.ToDetails()` — map photos ordered by `SortOrder`
- [x] Update `ListingMapper.ToSummary()` — map cover photo URL (first IsCover, fallback to first by SortOrder)
- [x] Update `GetListingDetailsQuery` — `.Include(l => l.Photos)`
- [x] Update `SearchListingsQuery` — `.Include(l => l.Photos)` for cover photo URL

##### Presentation
- [x] `Presentation/Contracts/AddListingPhotoRequest.cs` — `StorageKey`, `Url`, `Caption?`
- [x] `Presentation/Contracts/ReorderPhotosRequest.cs` — `PhotoIdsInOrder`
- [x] `ListingEndpoints` — add:
  - `POST /v1/listings/{id}/photos` — add photo (RequireLandlord)
  - `DELETE /v1/listings/{id}/photos/{photoId}` — remove photo (RequireLandlord)
  - `PUT /v1/listings/{id}/photos/{photoId}/cover` — set cover photo (RequireLandlord)
  - `PUT /v1/listings/{id}/photos/reorder` — reorder photos (RequireLandlord)

##### Infrastructure
- [x] `Infrastructure/Configurations/ListingPhotoConfiguration.cs` — table `listing_photos` in `listings` schema; index on `(ListingId, SortOrder)`
- [x] Update `ListingConfiguration.cs` — add `HasMany(Photos)` with cascade delete and backing field
- [x] Update `ListingsDbContext` — add `DbSet<ListingPhoto>`
- [ ] EF Core migration: `AddListingPhotos`

#### Part D — Favorites / Saved Listings

> Tenants can save listings they are interested in and browse their saved listings later. This is a simple many-to-many relationship between users and listings, with a saved timestamp for ordering.

##### Domain
- [x] `Domain/Entities/SavedListing.cs` — join entity:
  - `UserId` (Guid), `ListingId` (Guid) — composite PK
  - `SavedAt` (DateTime)
  - Factory method: `Create(userId, listingId)`

##### Application
- [x] `Application/DTOs/SavedListingDto.cs` — `ListingId`, `SavedAt`
- [x] `Application/Commands/SaveListingCommand.cs` + handler — checks listing exists, prevents duplicates, creates `SavedListing`
- [x] `Application/Commands/UnsaveListingCommand.cs` + handler — finds and removes saved listing
- [x] `Application/Queries/GetSavedListingsQuery.cs` + handler — paged query; returns `ListingSummaryDto` list ordered by `SavedAt` desc; includes photos for cover URL

##### Presentation
- [x] `ListingEndpoints` — add saved listings group `/v1/saved-listings`:
  - `POST /v1/saved-listings/{listingId}` — save listing (RequireAuth)
  - `DELETE /v1/saved-listings/{listingId}` — unsave listing (RequireAuth)
  - `GET /v1/saved-listings?page=&pageSize=` — get saved listings (RequireAuth)

##### Infrastructure
- [x] `Infrastructure/Configurations/SavedListingConfiguration.cs` — table `saved_listings` in `listings` schema; composite PK `(UserId, ListingId)`; indexes on `UserId` and `ListingId`
- [x] Update `ListingsDbContext` — add `DbSet<SavedListing>`
- [ ] EF Core migration: `AddSavedListings`

#### Part E — Close Listing

- [x] `Application/Commands/CloseListingCommand.cs` + handler — loads listing with includes, calls `listing.Close()`
- [x] `ListingEndpoints` — `POST /v1/listings/{id}/close` (RequireLandlord)

#### Part F — Search Availability Filtering

- [x] Update `SearchListingsQuery` — add `AvailableFrom` (DateOnly?), `AvailableTo` (DateOnly?) params
- [x] Update handler — `.Include(l => l.AvailabilityBlocks)` + `MatchesAvailabilityFilter()` using `AvailabilityService.IsAvailable()`
- [x] Update `ListingEndpoints.SearchListings` — accept `availableFrom` and `availableTo` query params

---

### 5.4a Auth — User Profile & Missing Endpoints

> Airbnb-style user profile with full personal information. Hosts and tenants can update their profile, change password, and admins can manage roles.

#### Domain
- [x] Update `ApplicationUser` — add profile fields:
  - `FirstName`, `LastName`, `DisplayName`
  - `Bio` (about me text)
  - `ProfilePhotoUrl`
  - `City`, `State`, `Country` (location)
  - `Languages` (comma-separated)
  - `Occupation`
  - `DateOfBirth` (DateOnly?)
  - `EmergencyContactName`, `EmergencyContactPhone`
  - `IsGovernmentIdVerified`, `IsPhoneVerified`
  - `ResponseRatePercent`, `ResponseTimeMinutes`

#### Application
- [x] Update `UserProfileDto` — include all profile fields + `MemberSince`
- [x] Update `GetCurrentUserQuery` — map all fields; extract `MapToDto()` as internal static for reuse
- [x] `Application/Commands/UpdateProfileCommand.cs` + handler — updates all mutable profile fields via `UserManager`

#### Presentation
- [x] `Presentation/Contracts/UpdateProfileRequest.cs`
- [x] `Presentation/Contracts/ChangePasswordRequest.cs`
- [x] `Presentation/Contracts/UpdateRoleRequest.cs`
- [x] Update `AuthEndpoints` — add endpoints:
  - `PUT /v1/auth/me` — update profile (RequireAuth)
  - `POST /v1/auth/change-password` — change password (RequireAuth)
  - `PUT /v1/auth/users/{userId}/role` — admin role update (RequirePlatformAdmin)

#### Infrastructure
- [ ] EF Core migration: `AddUserProfileFields` — adds profile columns to `AspNetUsers` table

---

### 5.4b Platform Settings (DB-backed, Admin-Editable)

> All configurable platform fees (protocol fee, arbitration fee, etc.) are stored in the database instead of `appsettings.json`. Admins can update them at runtime via API without redeployment. Settings are cached in-memory (5 min TTL) for performance.

#### SharedKernel
- [x] `SharedKernel/Settings/PlatformSetting.cs` — entity with `Key` (PK, string), `Value`, `Description`, `UpdatedAt`, `UpdatedByUserId`
- [x] `SharedKernel/Settings/IPlatformSettingsService.cs` — interface: `GetLongAsync`, `GetBoolAsync`, `GetStringAsync`, `GetAllAsync`, `SetAsync`
- [x] `SharedKernel/Settings/PlatformSettingKeys.cs` — constants: `protocol_fee.*`, `arbitration_fee.*`

#### Infrastructure
- [x] `Infrastructure/Settings/PlatformSettingsDbContext.cs` — dedicated DbContext, schema `platform`, seeds defaults
- [x] `Infrastructure/Settings/PlatformSettingConfiguration.cs` — EF config for `platform_settings` table
- [x] `Infrastructure/Settings/PlatformSettingsService.cs` — DB implementation with `IMemoryCache` (5 min TTL), cache-aside pattern
- [x] `Infrastructure/Settings/PlatformSettingsEndpoints.cs` — admin endpoints:
  - `GET /v1/admin/settings` — list all settings (RequirePlatformAdmin)
  - `PUT /v1/admin/settings/{key}` — update a setting (RequirePlatformAdmin)
- [x] Register in `AddInfrastructure()` — DbContext, `AddMemoryCache()`, scoped `IPlatformSettingsService`
- [x] Map `MapPlatformSettingsEndpoints()` in `Program.cs`
- [x] Seed default values on startup via `EnsureCreatedAsync()`

#### Default Settings (seeded)
| Key | Default | Description |
|-----|---------|-------------|
| `protocol_fee.monthly_cents` | 7900 | Monthly protocol fee per active deal ($79) |
| `protocol_fee.pilot_discount_cents` | 3900 | Pilot discount ($39) |
| `protocol_fee.pilot_active` | false | Whether pilot discount is active |
| `arbitration_fee.protocol_adjudication_cents` | 4900 | Protocol adjudication filing fee ($49) |
| `arbitration_fee.binding_arbitration_cents` | 9900 | Binding arbitration filing fee ($99) |

#### Callers Updated (IOptions<> removed)
- [x] `OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler` — uses `IPlatformSettingsService` for protocol fee
- [x] `GetProrationQuoteQuery` — uses `IPlatformSettingsService` for protocol fee
- [x] `FileCaseCommand` — uses `IPlatformSettingsService` for tier-based arbitration fee
- [x] Removed `IOptions<ProtocolFeeSettings>` from `ActivationAndBillingModuleRegistration`
- [x] Removed `IOptions<ArbitrationFeeSettings>` from `ArbitrationModuleRegistration`

---

### 5.4c Protocol Fee & Arbitration Fee (Domain Policies — kept as reference types)

- [x] `ProtocolFeeSettings.cs` — retained as reference type with `EffectiveMonthlyFeeCents` computation logic
- [x] `ArbitrationFeePolicy.cs` — retained as `ArbitrationFeeSettings` with `GetFilingFee(tier)` for type-safe fee lookup
- [x] `DealFinancials` — `MonthlyProtocolFeeCents` (renamed from `ActivationFeeCents`)
- [x] `ArbitrationCase` — `FiledByUserId`, `FilingFeeCents` properties; extracted from JWT at filing time

---

### 5.4d Booking Lifecycle Scenarios

> All timing/policy values are DB-backed via `PlatformSettingKeys` and admin-editable. No hardcoded timeouts.

#### Configurable Settings (seeded in `platform_settings`)
| Key | Default | Description |
|-----|---------|-------------|
| `payment.grace_period_days` | 3 | Days before payment is overdue |
| `payment.reminder_after_days` | 4 | Days to send tenant payment reminder |
| `payment.auto_cancel_after_days` | 7 | Days to auto-cancel unpaid bookings |
| `host_platform_payment.reminder_interval_days` | 2 | Days between host platform fee reminders |
| `host_platform_payment.suspend_after_days` | 14 | Days to suspend host for unpaid platform fees |
| `cancellation.insurance_refund_deadline_days` | 30 | Insurance refund eligibility window |
| `damage_claim.filing_deadline_days` | 14 | Days after check-out to file damage claim |

#### Scenario A — Tenant Pays Late
- [x] `DealPaymentConfirmation` — configurable `GracePeriodExpiresAt` (from `payment.grace_period_days`)
- [x] `DealPaymentConfirmation.NeedsReminder()` — checks if reminder should be sent
- [x] `DealPaymentConfirmation.ShouldAutoCancel()` — checks if auto-cancel threshold reached
- [x] `DealPaymentConfirmation.MarkReminderSent()` — tracks reminder state
- [x] `PaymentConfirmationTimeoutJob` — upgraded: sends reminders, auto-cancels, marks applications cancelled
- [x] `PaymentConfirmationStatus.Cancelled` — new enum value
- [x] `DealApplicationStatus.Cancelled` — new enum value

#### Scenario B — Host Doesn't Pay Platform
- [x] `DealPaymentConfirmation.HostNeedsPlatformPaymentReminder()` — interval-based reminder check
- [x] `DealPaymentConfirmation.HostShouldBeSuspended()` — suspension threshold check
- [x] `DealPaymentConfirmation.MarkHostPlatformReminderSent()` — tracks host reminder state
- [x] `HostPlatformPaymentEnforcementJob` — new Quartz job (daily at 8AM):
  - Sends periodic reminders to host
  - Suspends `BillingAccount` if platform fee unpaid past threshold
- [x] Registered in `ActivationAndBillingModuleRegistration`

#### Scenario C — Tenant Cancels Before Check-in
- [x] `CancellationRefundCalculator` — calculates refund based on `CancellationPolicy`:
  - Full refund if cancelled >= `FreeCancellationDays` before check-in
  - Partial refund if within `PartialRefundDays` window
  - No refund otherwise
- [x] `CancelBookingCommand` — orchestrates cancellation:
  - Cancels `DealApplication` (status → Cancelled)
  - Cancels `DealPaymentConfirmation` (status → Cancelled)
  - Closes `BillingAccount` if active
  - Fires `BookingCancelledEvent`
- [x] `BookingCancelledEvent` — carries `RefundAmountCents`, `InsuranceRefundCents`, `IsAutoCancel`
- [x] `CancellationResultDto` — returned to caller with refund breakdown
- [x] `POST /v1/deals/{dealId}/payment/cancel` endpoint
- [x] `OnBookingCancelledCleanupHandler` — handles post-cancellation cleanup

#### Scenario D — Damage Claims During Stay
- [x] `DamageClaim` aggregate:
  - Status: Filed → UnderReview → Approved/PartiallyApproved/Rejected → Settled
  - Auto-calculates `DepositDeductionCents` (min of claimed and deposit)
  - Auto-calculates `InsuranceClaimCents` (amount above deposit)
  - `EvidenceManifestId` links to Evidence module
  - One claim per deal (unique index)
- [x] `DamageClaimStatus` enum
- [x] `DamageClaimFiledEvent` — domain event
- [x] `FileDamageClaimCommand` — validates filing deadline, prevents duplicates
- [x] `DamageClaimConfiguration` — EF config in `activation_billing` schema
- [x] `ManifestType.Damage` — added to Evidence module
- [x] `POST /v1/deals/{dealId}/payment/damage-claim` endpoint
- [x] `DamageClaimDto` for API responses

#### Notification Wiring
- [x] `OnPaymentConfirmedNotifyHandler` — notifies tenant when host confirms payment
- [x] `OnDealActivatedNotifyHandler` — notifies both tenant and host on activation
- [x] `OnBookingCancelledNotifyHandler` — notifies both parties with refund info
- [x] `OnDamageClaimFiledNotifyHandler` — notifies tenant of damage claim

---

### 5.5 StructuredInquiry

> Not a messaging system. One-directional schema-bound data capture. Permanently closed on Truth Surface confirmation.

#### Project & References
- [x] `StructuredInquiry.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/InquirySession.cs` — `DealId`, `Status` (Locked/Open/Closed), `UnlockedByLandlordAt?`, `ClosedAt?`
- [x] `Domain/Entities/InquiryQuestion.cs` — `SessionId`, `Category` (enum: UtilitySpecifics/AccessibilityLayout/RuleClarification/Proximity), `PredefinedQuestionId` (FK to seed data), `SubmittedAt`
- [x] `Domain/Entities/InquiryAnswer.cs` — `QuestionId`, `ResponseType` (YesNo/MultipleChoice/Numeric), `AnswerValue`, `AnsweredAt` — promoted to Landlord Declaration on Truth Surface creation
- [x] `Domain/Events/InquiryLoggedAsComplianceSignalEvent.cs`
- [x] `Domain/Events/InquiryClosedEvent.cs` — triggers `CloseInquiryOnTruthSurfaceConfirmationCommand`

#### Application
- [x] `Application/Commands/RequestDetailUnlockCommand.cs` + handler — tenant requests; default disabled
- [x] `Application/Commands/ApproveInquiryUnlockCommand.cs` + handler — landlord explicitly approves
- [x] `Application/Commands/SubmitInquiryQuestionCommand.cs` + handler — only predefined question IDs; contact-info bypass detection (regex scan for phone numbers, emails in response slot context)
- [x] `Application/Commands/SubmitLandlordResponseCommand.cs` + handler — structured response only; auto-logs compliance signal; auto-promotes to Landlord Declaration queue
- [x] `Application/Commands/CloseInquiryOnTruthSurfaceConfirmationCommand.cs` + handler — permanent lock; event fires `InquiryClosedEvent`; `IEmailService` sends "The Inquiry Service is now closed" notice to both parties
- [x] `Application/Queries/GetInquiryThreadQuery.cs` + handler
- [x] `Application/Queries/ListPredefinedQuestionsQuery.cs` + handler
- [x] `Application/DTOs/InquiryDto.cs`
- [x] `Application/DTOs/PredefinedQuestionDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/InquiryEndpoints.cs`
- [x] `Presentation/Contracts/SubmitInquiryQuestionRequest.cs`
- [x] `Presentation/Contracts/SubmitLandlordResponseRequest.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/InquiryDbContext.cs` — schema `inquiry`
- [x] `Infrastructure/Persistence/Schemas/inquiry.schema.sql`
- [x] `Infrastructure/Repositories/InquirySessionRepository.cs`
- [x] `Infrastructure/Configurations/InquirySessionConfiguration.cs`
- [x] `Infrastructure/Jobs/InquiryIntegrityScanJob.cs` — daily: detect systematic landlord rejection patterns; detect contact-info bypass (regex); log Trust Ledger penalty
- [x] EF Core migrations
- [x] Seed: predefined question library (counsel-vetted IDs + text)

#### Module Registration
- [x] `StructuredInquiryModuleRegistration.cs`

---

### 5.6 VerificationAndRisk

> Deterministic Verification Class (v1). No ML. No actuarial loss model.

#### Project & References
- [x] `VerificationAndRisk.csproj` — references SharedKernel, Infrastructure, PartnerNetwork
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/RiskProfile.cs` — `TenantUserId`, `VerificationClass` (Low/Medium/High), `ConfidenceIndicator`, `DepositBandLowCents`, `DepositBandHighCents`, `ComputedAt`, `InputHash` (hash of all inputs for audit)
- [x] `Domain/Policies/VerificationClassPolicy.cs` — deterministic rules engine:
  - Low: identity verified + background Pass + insurance Active/InstitutionBacked + no violations
  - Medium: identity verified + background Pass/Review + insurance Active
  - High: identity failed/pending, or background Fail, or no insurance
  - **Explicitly excludes**: race, color, religion, national origin, sex, familial status, disability
- [x] `Domain/Policies/DepositRecommendationPolicy.cs` — deposit band = `VerificationClass × InsuranceState × JurisdictionCap`; adverse action limitation enforced; automated adjustment for verified service members
- [x] `Domain/Events/DepositBandUpdatedEvent.cs`
- [x] `Domain/Events/VerificationClassComputedEvent.cs`

#### Application
- [x] `Application/Commands/RecalculateVerificationClassCommand.cs` + handler — triggered by: `IdentityVerifiedEvent`, `InsuranceStatusChangedEvent`, `TrustLedgerEntryRecordedEvent`, `ReferralRedeemedEvent`
- [x] `Application/EventHandlers/OnReferralRedeemedRecalculateRiskHandler.cs` — listens to `ReferralRedeemedEvent` (from PartnerNetwork); sends `RecalculateVerificationClassCommand` with `InsuranceStatus.InstitutionBacked`, `IdentityVerificationStatus.Verified`, `BackgroundCheckStatus.Clear`, `ViolationCount: 0` — automatically upgrades risk profile for partner-referred users
- [x] `Application/Commands/ComputeDepositBandCommand.cs` + handler — fetches active jurisdiction cap from JurisdictionPacks
- [x] `Application/Queries/GetRiskViewForLandlordQuery.cs` + handler — returns class + confidence + deposit band; raw signals not exposed
- [x] `Application/DTOs/RiskViewDto.cs`
- [x] `Application/DTOs/DepositBandDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/RiskEndpoints.cs`
- [x] `Presentation/Contracts/RiskViewResponse.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/RiskDbContext.cs` — schema `risk`
- [x] `Infrastructure/Persistence/Schemas/risk.schema.sql`
- [x] `Infrastructure/Repositories/RiskProfileRepository.cs`
- [x] EF Core migrations

#### Module Registration
- [x] `VerificationAndRiskModuleRegistration.cs` — registers `OnReferralRedeemedRecalculateRiskHandler`

---

### 5.7 ComplianceMonitoring

#### Project & References
- [x] `ComplianceMonitoring.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Entities/Violation.cs` — `DealId`, `Category` (A–G), `DetectedAt`, `CureDeadline`, `Status` (Open/Cured/Escalated)
- [x] `Domain/Entities/ComplianceSignal.cs` — `DealId`, `SignalType`, `Source`, `ReceivedAt`
- [x] `Domain/ValueObjects/ViolationCategory.cs`
- [x] `Domain/Events/ViolationRecordedEvent.cs`
- [x] `Domain/Events/InsuranceLapseViolationCreatedEvent.cs`

#### Application
- [x] `Application/Commands/DetectViolationCommand.cs` + handler
- [x] `Application/Commands/RecordComplianceSignalCommand.cs` + handler — ingests signals from all modules via Outbox
- [x] `Application/Commands/CloseComplianceWindowCommand.cs` + handler
- [x] `Application/Queries/GetDealComplianceStatusQuery.cs` + handler
- [x] `Application/Queries/ListViolationsQuery.cs` + handler
- [x] `Application/DTOs/ViolationDto.cs`
- [x] `Application/DTOs/ComplianceStatusDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/ComplianceEndpoints.cs`
- [x] `Presentation/Contracts/ComplianceStatusResponse.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/ComplianceDbContext.cs` — schema `compliance_monitoring`
- [x] `Infrastructure/Persistence/Schemas/compliance.schema.sql`
- [x] `Infrastructure/Repositories/ViolationRepository.cs`
- [x] `Infrastructure/Configurations/ViolationConfiguration.cs`
- [x] `Infrastructure/Jobs/ComplianceScannerJob.cs` — every 6h: check active deals for insurance lapse, overdue cure windows, missing evidence kits; send email alerts via `IEmailService`
- [x] EF Core migrations

#### Module Registration
- [x] `ComplianceMonitoringModuleRegistration.cs`

---

### 5.8 Arbitration

#### Project & References
- [x] `Arbitration.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/ArbitrationCase.cs` — `DealId`, `Tier` (ProtocolAdjudication/BindingArbitration), `Category` (A–G + Other), `Status` (Filed/EvidencePending/EvidenceComplete/UnderReview/Decided/Appealed), `FiledAt`, `EvidenceCompleteAt`, `DecisionDueAt` (14 calendar days from EvidenceComplete)
- [x] `Domain/Entities/EvidenceSlot.cs` — `CaseId`, `SlotType`, `SubmittedBy`, `FileReference` (MinIO key), `SubmittedAt`
- [x] `Domain/Entities/ArbitratorAssignment.cs` — `CaseId`, `ArbitratorUserId`, `AssignedAt`, `ConcurrentCaseCount`
- [x] `Domain/Policies/EvidenceMinimumThresholdPolicy.cs` — per-category minimum bundle (A–G per spec); Category G: requires closest-category mapping + Truth Surface line item citation + ≤200-word justification
- [x] `Domain/Events/CaseFiledEvent.cs`
- [x] `Domain/Events/EvidenceCompleteEvent.cs`
- [x] `Domain/Events/DecisionIssuedEvent.cs`
- [x] `Domain/Events/ArbitrationBacklogEscalationEvent.cs`

#### Application
- [x] `Application/Commands/FileCaseCommand.cs` + handler — gates: deal active, fee current, category valid, minimum evidence schema met, initiation deposit recorded (beta friction)
- [x] `Application/Commands/AttachEvidenceCommand.cs` + handler — structured slots only; late evidence rule; file stored in MinIO
- [x] `Application/Commands/MarkEvidenceCompleteCommand.cs` + handler — starts 14-day SLA clock
- [x] `Application/Commands/AssignArbitratorCommand.cs` + handler — random from panel; no prior cases with either party; joint rejection allowed once; hard cap 20 / soft 15
- [x] `Application/Commands/IssueProtocolDecisionCommand.cs` + handler — Tier 1; records to Trust Ledger
- [x] `Application/Commands/IssueBindingAwardCommand.cs` + handler — Tier 2; records to Trust Ledger; generates court-confirmation template
- [x] `Application/Queries/GetCaseQuery.cs` + handler
- [x] `Application/Queries/ListCasesByStatusQuery.cs` + handler
- [x] `Application/DTOs/CaseDto.cs`
- [x] `Application/DTOs/DecisionDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/ArbitrationEndpoints.cs`
- [x] `Presentation/Endpoints/ArbitratorEndpoints.cs`
- [x] `Presentation/Contracts/FileCaseRequest.cs`
- [x] `Presentation/Contracts/IssueDecisionRequest.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/ArbitrationDbContext.cs` — schema `arbitration`
- [x] `Infrastructure/Persistence/Schemas/arbitration.schema.sql`
- [x] `Infrastructure/Repositories/ArbitrationCaseRepository.cs`
- [x] `Infrastructure/Configurations/ArbitrationCaseConfiguration.cs`
- [x] `Infrastructure/Jobs/ArbitrationBacklogSlaJob.cs` — hourly: caseload per arbitrator; soft threshold 15 → load balance; hard cap 20 → block; triage: safety/habitability → move-out → FIFO
- [x] EF Core migrations

#### Module Registration
- [x] `ArbitrationModuleRegistration.cs`

---

### 5.9 JurisdictionPacks

> Dual-control approval. Version-locked to each active deal. California / LA is the v1 jurisdiction.

#### Project & References
- [x] `JurisdictionPacks.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/JurisdictionPack.cs` — `JurisdictionCode`, `ActiveVersionId`, `Versions` list
- [x] `Domain/Entities/PackVersion.cs` — `VersionNumber`, `Status` (Draft/PendingApproval/Active/Deprecated), `EffectiveDate`, `ApprovedAt`, `ApprovedBy` (requires 2 distinct admin users)
- [x] `Domain/Entities/EffectiveDateRule.cs` — `FieldName`, `EffectiveDate` (e.g. AB 2801 pre-occupancy photos: July 1 2025)
- [x] `Domain/Entities/FieldGatingRule.cs` — `FieldName`, `GatingType` (Hard/Soft), `Value`, `Condition`
- [x] `Domain/Entities/EvidenceSchedule.cs` — per-category minimum evidence requirements for this jurisdiction
- [x] `Domain/ValueObjects/JurisdictionCode.cs` — format `US-CA-LA`
- [x] `Domain/ValueObjects/RuleExpression.cs` — simple DSL string for field gating conditions
- [x] `Domain/Events/JurisdictionPackPublishedEvent.cs`
- [x] `Domain/Events/PackEffectiveDateChangedEvent.cs`

#### Application — California / LA v1 Pack
- [x] AB 12: 1× deposit cap default; 2× small-landlord exception with certification logged
- [x] SB 611: military status tracking; higher deposit tracking + 6-month return window
- [x] AB 628: stove mandatory (no waiver); refrigerator tenant-opt-in with lease language; 30-day provider window on tenant withdrawal
- [x] AB 2801: post-vacancy photos gate Apr 1 2025; pre-occupancy photos gate Jul 1 2025
- [x] AB 414: Direct-to-Counterparty Refund Instructions mandatory; UI disclaimer enforced
- [x] JCO: Relocation Assistance Disclaimer triggered at 175-day stay mark (email via `IEmailService`)
- [x] `Application/Commands/CreatePackDraftCommand.cs` + handler
- [x] `Application/Commands/UpdatePackDraftCommand.cs` + handler
- [x] `Application/Commands/ValidatePackCommand.cs` + handler
- [x] `Application/Commands/RequestDualControlApprovalCommand.cs` + handler
- [x] `Application/Commands/ApprovePackVersionCommand.cs` + handler — requires 2nd distinct approver
- [x] `Application/Commands/PublishPackVersionCommand.cs` + handler
- [x] `Application/Commands/DeprecatePackVersionCommand.cs` + handler
- [x] `Application/Queries/GetActivePackForJurisdictionQuery.cs` + handler
- [x] `Application/Queries/GetPackVersionDetailsQuery.cs` + handler
- [x] `Application/Queries/ListPackVersionsQuery.cs` + handler
- [x] `Application/DTOs/JurisdictionPackDto.cs`, `FieldGateRuleDto.cs`, `EvidenceScheduleDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/JurisdictionPackEndpoints.cs`
- [x] `Presentation/Contracts/CreatePackVersionRequest.cs`, `JurisdictionPackResponse.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/JurisdictionDbContext.cs` — schema `jurisdiction`
- [x] `Infrastructure/Persistence/Schemas/jurisdiction.schema.sql`
- [x] `Infrastructure/Repositories/JurisdictionPackRepository.cs`
- [x] `Infrastructure/Configurations/JurisdictionPackConfiguration.cs`, `PackVersionConfiguration.cs`
- [x] `Infrastructure/Jobs/PackEffectiveDateActivationJob.cs` — daily at midnight: promote Pack Version to Active on effective date
- [x] EF Core migrations
- [x] Seed data: California / LA v1 pack with all rules above

#### Module Registration
- [x] `JurisdictionPacksModuleRegistration.cs`

---

### 5.10 Evidence

#### Project & References
- [x] `Evidence.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/EvidenceManifest.cs` — `DealId`, `ManifestType` (MoveIn/MoveOut/Arbitration/Insurance), `Status` (Open/Sealed), `SealedAt`, `HashOfAllFiles`
- [x] `Domain/Entities/EvidenceUpload.cs` — `ManifestId`, `OriginalFileName`, `StorageKey` (MinIO object key), `FileHash` (SHA-256), `MimeType`, `UploadedAt`, `TimestampMetadata` (from EXIF strip log)
- [x] `Domain/Entities/MalwareScanResult.cs` — `UploadId`, `Status` (Pending/Clean/Infected), `ScannedAt`
- [x] `Domain/Entities/MetadataStrippingLog.cs` — `UploadId`, `StrippedAt`, `RemovedFields` (JSON list)
- [x] `Domain/ValueObjects/FileHash.cs` — SHA-256 hex string
- [x] `Domain/ValueObjects/ScanStatus.cs` — enum
- [x] Domain events: `EvidenceUploadedEvent`, `EvidenceScannedEvent`, `EvidenceManifestCreatedEvent`, `EvidenceManifestSealedEvent`

#### Application
- [x] `Application/Commands/RequestUploadUrlCommand.cs` + handler — calls `IObjectStorageService.GeneratePresignedUploadUrl` (MinIO); returns time-limited URL
- [x] `Application/Commands/CompleteUploadCommand.cs` + handler — records file hash; starts malware scan via `IAntivirusService`
- [x] `Application/Commands/StartMalwareScanCommand.cs` + handler — sends file stream to ClamAV
- [x] `Application/Commands/RecordScanResultCommand.cs` + handler — Clean: mark ready; Infected: quarantine in MinIO, notify ops via email
- [x] `Application/Commands/StripMetadataCommand.cs` + handler — removes PII from EXIF using `MetadataExtractor` NuGet
- [x] `Application/Commands/CreateEvidenceManifestCommand.cs` + handler
- [x] `Application/Commands/SealEvidenceManifestCommand.cs` + handler — SHA-256 hash of all file hashes; immutable
- [x] `Application/Commands/ArchiveEvidenceCommand.cs` + handler — sets MinIO lifecycle policy: 7-year retention
- [x] `Application/Queries/GetManifestQuery.cs` + handler
- [x] `Application/Queries/GetScanStatusQuery.cs` + handler
- [x] `Application/DTOs/UploadUrlDto.cs`, `ManifestDto.cs`, `ScanResultDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/EvidenceEndpoints.cs`
- [x] `Presentation/Endpoints/UploadEndpoints.cs`
- [x] `Presentation/Contracts/RequestUploadUrlRequest.cs`, `SubmitManifestRequest.cs`, `EvidenceManifestResponse.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/EvidenceDbContext.cs` — schema `evidence`
- [x] `Infrastructure/Persistence/Schemas/evidence.schema.sql`
- [x] `Infrastructure/Repositories/EvidenceManifestRepository.cs`
- [x] `Infrastructure/Configurations/EvidenceUploadConfiguration.cs`, `EvidenceManifestConfiguration.cs`
- [x] `Infrastructure/Jobs/MalwareScanPollingJob.cs` — every 5 min: poll ClamAV scan status for pending uploads
- [x] `Infrastructure/Jobs/EvidenceRetentionJob.cs` — nightly: enforce 7-year MinIO lifecycle; anonymize after 2 years inactivity
- [x] EF Core migrations
- [x] Add `MetadataExtractor` NuGet to `Directory.Packages.props`

#### Module Registration
- [x] `EvidenceModuleRegistration.cs`

---

### 5.11 Notifications

> Email-only in v1. SMS deferred to v2. Uses `IEmailService` (MailKit + Brevo SMTP).

#### Project & References
- [x] `Notifications.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/Notification.cs` — `RecipientUserId`, `RecipientEmail`, `Channel` (Email only in v1), `TemplateId`, `Status` (Queued/Sent/Failed/Delivered), `ScheduledAt`, `SentAt`
- [x] `Domain/Entities/NotificationTemplate.cs` — `TemplateId`, `Channel`, `Subject`, `HtmlBody` (inline string template with `{placeholder}` tokens), `PlainTextBody`
- [x] `Domain/Entities/DeliveryLog.cs` — `NotificationId`, `BrevoMessageId`, `DeliveredAt?`, `Error?`
- [x] `Domain/Entities/UserNotificationPreferences.cs` — `UserId`, per-event-type opt-in (transactional system notices always sent regardless)
- [x] Domain events: `NotificationQueuedEvent`, `NotificationDeliveredEvent`, `NotificationFailedEvent`
- [x] **Required system notice templates** (all email, hardcoded, non-opt-out):
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
- [x] `Application/Commands/SendEmailNotificationCommand.cs` + handler — calls `IEmailService.SendAsync` (MailKit → Brevo SMTP)
- [x] `Application/Commands/SendInAppNotificationCommand.cs` + handler — stores for in-app notification feed
- [x] `Application/Commands/QueueNotificationCommand.cs` + handler — persists to DB; outbox dispatches
- [x] `Application/Commands/MarkNotificationDeliveredCommand.cs` + handler
- [x] `Application/Commands/UpdateUserPreferencesCommand.cs` + handler
- [x] `Application/Queries/GetUserPreferencesQuery.cs` + handler
- [x] `Application/Queries/ListNotificationHistoryQuery.cs` + handler
- [x] `Application/DTOs/NotificationDto.cs`, `NotificationPreferencesDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/NotificationPreferencesEndpoints.cs`
- [x] `Presentation/Contracts/UpdatePreferencesRequest.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/NotificationDbContext.cs` — schema `notifications`
- [x] `Infrastructure/Persistence/Schemas/notifications.schema.sql`
- [x] `Infrastructure/Repositories/NotificationRepository.cs`
- [x] `Infrastructure/Repositories/TemplateRepository.cs`
- [x] `Infrastructure/Configurations/NotificationConfiguration.cs`
- [x] `Infrastructure/Jobs/NotificationRetryJob.cs` — every 10 min: retry failed emails (max 5 attempts, exponential back-off via Polly)
- [x] Seed: all required system notice templates
- [x] EF Core migrations

#### Module Registration
- [x] `NotificationsModuleRegistration.cs`

#### Real-Time InApp Notification System (v2)

> Adds SignalR-based real-time push + persistent in-app notifications alongside existing email channel.

##### Infrastructure (SharedKernel + Lagedra.Infrastructure)
- [x] `SharedKernel/RealTime/INotificationPusher.cs` — interface: `PushToUserAsync`, `PushToUsersAsync`; `InAppNotificationDto` record
- [x] `SharedKernel/Integration/IUserEmailResolver.cs` — interface: `GetEmailAsync(Guid userId)` for cross-module email lookup
- [x] `Infrastructure/RealTime/NotificationHub.cs` — SignalR hub, `[Authorize]`, groups users by `user:{userId}`
- [x] `Infrastructure/RealTime/SignalRNotificationPusher.cs` — implements `INotificationPusher` via `IHubContext<NotificationHub>`
- [x] `InfrastructureServiceRegistration.cs` — registers `AddSignalR()` + `INotificationPusher` as singleton
- [x] `Program.cs` — `app.MapHub<NotificationHub>("/hubs/notifications")`; JWT `OnMessageReceived` for SignalR token from query string

##### Domain
- [x] `Domain/Entities/InAppNotification.cs` — `Id`, `RecipientUserId`, `Title`, `Body`, `Category`, `RelatedEntityId`, `RelatedEntityType`, `IsRead`, `ReadAt`, `CreatedAt`
- [x] `Domain/Enums/NotificationChannel.cs` — added `InApp` member

##### Application
- [x] `Application/Commands/DeliverInAppNotificationCommand.cs` — persists `InAppNotification`, then pushes via `INotificationPusher`
- [x] `Application/Commands/NotifyUserCommand.cs` — convenience dual-channel command (Email + InApp); resolves email via `IUserEmailResolver`
- [x] `Application/Commands/MarkNotificationReadCommand.cs` — marks single or all notifications as read
- [x] `Application/Queries/GetUnreadNotificationsQuery.cs` — returns unread InApp notifications for user
- [x] `Application/Queries/GetUnreadCountQuery.cs` — returns unread count

##### Presentation
- [x] `Presentation/Endpoints/InAppNotificationEndpoints.cs` — `GET /v1/notifications/unread`, `GET /v1/notifications/unread/count`, `POST /v1/notifications/{id}/read`, `POST /v1/notifications/read-all`

##### Infrastructure (Persistence)
- [x] `Infrastructure/Configurations/InAppNotificationConfiguration.cs` — table `in_app_notifications`, indexes on `(RecipientUserId, IsRead)` and `CreatedAt`
- [x] `NotificationDbContext` — added `DbSet<InAppNotification>`
- [x] `Infrastructure/Jobs/NotificationProcessingJob.cs` — Quartz, every 30s: dispatches queued `Email` and `InApp` notifications in batches of 100

##### Auth Module
- [x] `Infrastructure/Services/UserEmailResolver.cs` — implements `IUserEmailResolver` via `UserManager<ApplicationUser>`
- [x] `AuthModuleRegistration.cs` — registers `IUserEmailResolver`

##### Notification Handlers (28 handlers across 7 modules)

**ActivationAndBilling (10 handlers):**
- [x] `OnApplicationSubmittedNotify` — notifies host of new application (Email + InApp)
- [x] `OnApplicationApprovedNotify` — notifies tenant of approval (Email + InApp)
- [x] `OnApplicationRejectedNotify` — notifies tenant of rejection (Email + InApp)
- [x] `OnPaymentConfirmedNotify` — notifies tenant of payment confirmation (Email + InApp)
- [x] `OnPaymentDisputedNotify` — notifies host of payment dispute (Email + InApp)
- [x] `OnPaymentDisputeResolvedNotify` — notifies both parties of dispute resolution (Email + InApp)
- [x] `OnDealActivatedNotify` — notifies both tenant and host of deal activation (Email + InApp)
- [x] `OnBookingCancelledNotify` — notifies both parties with refund info (Email + InApp)
- [x] `OnDamageClaimFiledNotify` — notifies tenant of damage claim (Email + InApp)
- [x] `OnPaymentFailedNotify` — notifies host of payment failure (InApp only)

**Arbitration (3 handlers):**
- [x] `OnCaseFiledNotify` — notifies filer of case receipt (Email + InApp)
- [x] `OnDecisionIssuedNotify` — notifies filer of decision (Email + InApp)
- [x] `OnEvidenceCompleteNotify` — notifies filer evidence collection complete (InApp only)

**IdentityAndVerification (3 handlers):**
- [x] `OnIdentityVerifiedNotify` — notifies user of successful verification (Email + InApp)
- [x] `OnIdentityVerificationFailedNotify` — notifies user of verification failure (Email + InApp)
- [x] `OnVerificationClassChangedNotify` — notifies user of level change (InApp only)

**Auth (1 handler):**
- [x] `OnUserRegisteredNotify` — welcome notification (InApp only)

**InsuranceIntegration (1 handler):**
- [x] `OnInsuranceStatusChangedNotify` — notifies tenant of insurance state change (Email + InApp)

**ListingAndLocation (1 handler):**
- [x] `OnListingPublishedNotify` — notifies host listing is live (InApp only)

**AntiAbuseAndIntegrity (1 handler):**
- [x] `OnAccountRestrictionNotify` — notifies user of account restriction (InApp only)

---

### 5.12 Privacy

#### Project & References
- [x] `Privacy.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/UserConsent.cs` — `UserId`, consent records list
- [x] `Domain/Entities/ConsentRecord.cs` — `UserId`, `ConsentType`, `GrantedAt`, `WithdrawnAt`, `IpAddress`, `UserAgent`
- [x] `Domain/Entities/LegalHold.cs` — `UserId`, `Reason`, `AppliedAt`, `ReleasedAt`
- [x] `Domain/Entities/DataExportRequest.cs` — `UserId`, `Status`, `RequestedAt`, `CompletedAt`, `PackageUrl` (MinIO signed URL)
- [x] `Domain/Entities/DeletionRequest.cs` — `UserId`, `Status`, `RequestedAt`, `CompletedAt`, `BlockingReason`
- [x] `Domain/ValueObjects/ConsentType.cs` — enum: KYCConsent, FCRAConsent, MarketingEmail, DataProcessing
- [x] `Domain/ValueObjects/RetentionPeriod.cs` — 7-year core records, 2-year inactive profile, 30-day cancelled pre-activation
- [x] Domain events: `ConsentRecordedEvent`, `LegalHoldAppliedEvent`, `DataAnonymizedEvent`

#### Application
- [x] `Application/Commands/RecordConsentCommand.cs` + handler
- [x] `Application/Commands/WithdrawConsentCommand.cs` + handler
- [x] `Application/Commands/EnqueueDataExportCommand.cs` + handler
- [x] `Application/Commands/GenerateDataExportPackageCommand.cs` + handler — zips user data from all modules; uploads to MinIO; sends download email via `IEmailService`
- [x] `Application/Commands/RequestDeletionCommand.cs` + handler — checks legal holds, active deals, retention periods; blocks if any hold active
- [x] `Application/Commands/ApplyLegalHoldCommand.cs` + handler
- [x] `Application/Commands/ReleaseLegalHoldCommand.cs` + handler
- [x] `Application/Commands/AnonymizeInactiveUserCommand.cs` + handler — 2-year inactivity threshold
- [x] `Application/Queries/GetUserConsentsQuery.cs`, `GetDataExportStatusQuery.cs`, `ListActiveLegalHoldsQuery.cs` + handlers
- [x] `Application/DTOs/ConsentDto.cs`, `LegalHoldDto.cs`, `DeletionRequestDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/PrivacyEndpoints.cs`
- [x] `Presentation/Contracts/ConsentRequest.cs`, `DataExportRequest.cs`, `DeletionRequest.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/PrivacyDbContext.cs` — schema `privacy`
- [x] `Infrastructure/Persistence/Schemas/privacy.schema.sql`
- [x] `Infrastructure/Repositories/ConsentRepository.cs`, `LegalHoldRepository.cs`
- [x] `Infrastructure/Configurations/ConsentConfiguration.cs`
- [x] `Infrastructure/Jobs/RetentionEnforcementJob.cs` — nightly: anonymize 2-year inactive profiles; delete 30-day pre-activation cancelled data
- [x] `Infrastructure/Jobs/DataExportPurgeJob.cs` — daily: removes export packages from MinIO after 48h or download
- [x] EF Core migrations

#### Module Registration
- [x] `PrivacyModuleRegistration.cs`

---

### 5.13 AntiAbuseAndIntegrity

#### Project & References
- [x] `AntiAbuseAndIntegrity.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/AbuseCase.cs` — `SubjectUserId`, `AbuseType`, `Status`, `DetectedAt`, `ResolvedAt`
- [x] `Domain/Entities/CollusionPattern.cs` — repeated deal creation/closure between same parties
- [x] `Domain/Entities/FraudFlag.cs` — `UserId`, `FlagType` (SyntheticId/ContactInfoBypass/TrustLedgerGaming/BadFaithReporting), `Severity` (High/Medium/Low), `FlaggedAt`
- [x] `Domain/Entities/AccountRestriction.cs` — `UserId`, `RestrictionLevel` (Limited/Suspended/Banned), `AppliedAt`, `Reason`
- [x] `Domain/ValueObjects/AbuseType.cs` — enum: Collusion, InquiryAbuse, TrustLedgerGaming, BadFaithReporting, SyntheticIdentity, AffiliationFraud
- [x] Domain events: `CollusionDetectedEvent`, `InquiryAbuseDetectedEvent`, `TrustLedgerGamingDetectedEvent`, `AccountRestrictionAppliedEvent`

#### Application
- [x] `Application/Commands/DetectCollusionCommand.cs` + handler — pattern: repeated deal pairs
- [x] `Application/Commands/DetectInquiryAbuseCommand.cs` + handler — systematic rejection patterns; contact-info bypass
- [x] `Application/Commands/DetectTrustLedgerGamingCommand.cs` + handler — landlord false violation reporting
- [x] `Application/Commands/RaiseAbuseFlagCommand.cs` + handler
- [x] `Application/Commands/ApplyAccountRestrictionCommand.cs` + handler — sends notification email via `IEmailService`
- [x] `Application/Commands/SuspendAccountCommand.cs` + handler
- [x] `Application/Queries/GetAbuseFlagsQuery.cs`, `GetUserRestrictionsQuery.cs` + handlers
- [x] `Application/DTOs/AbuseFlagDto.cs`, `AccountRestrictionDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/IntegrityEndpoints.cs`
- [x] `Presentation/Contracts/AbuseFlagResponse.cs`

#### Infrastructure
- [x] `Infrastructure/Persistence/IntegrityDbContext.cs` — schema `integrity`
- [x] `Infrastructure/Persistence/Schemas/integrity.schema.sql`
- [x] `Infrastructure/Repositories/AbuseCaseRepository.cs`, `FraudFlagRepository.cs`
- [x] `Infrastructure/Configurations/AbuseCaseConfiguration.cs`
- [x] `Infrastructure/Jobs/PatternDetectionSchedulerJob.cs` — every 4h: collusion scan, gaming scan, bad-faith landlord scan
- [x] EF Core migrations

#### Module Registration
- [x] `AntiAbuseAndIntegrityModuleRegistration.cs`

---

### 5.14 ContentManagement

> Manages blog posts and static SEO pages. Exposes **public, unauthenticated read endpoints** consumed by `apps/marketing` (Next.js) and **admin-only write endpoints** consumed by `apps/admin`. Content is stored as Markdown in the database; the marketing site is responsible for rendering.

#### Project & References
- [x] `ContentManagement.csproj`
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/BlogPost.cs` — `BlogPostId` (Guid), `Slug` (unique, URL-safe), `Title`, `Excerpt`, `Content` (Markdown string), `Status` (Draft/Published/Archived), `PublishedAt` (nullable), `AuthorUserId`, `Tags` (string[]), `MetaTitle`, `MetaDescription`, `OgImageUrl`, `ReadingTimeMinutes`
- [x] `Domain/Entities/SeoPage.cs` — `SeoPageId`, `Slug` (unique), `Title`, `MetaTitle`, `MetaDescription`, `OgImageUrl`, `CanonicalUrl`, `NoIndex` (bool), `UpdatedAt`
- [x] `Domain/ValueObjects/BlogStatus.cs` — `Draft | Published | Archived`
- [x] Domain events: `BlogPostPublishedEvent`, `BlogPostArchivedEvent`
- [x] Domain rule: `Slug` must be lowercase, hyphen-separated, unique across all blog posts; validated on creation and update
- [x] Domain rule: `PublishedAt` is set to UTC now on first publish and is immutable thereafter

#### Application
- [x] `Application/Commands/CreateBlogPostCommand.cs` + handler — admin only; generates slug from title if not provided; enforces uniqueness
- [x] `Application/Commands/UpdateBlogPostCommand.cs` + handler — admin only; slug change forbidden after publish
- [x] `Application/Commands/PublishBlogPostCommand.cs` + handler — transitions Draft → Published; sets `PublishedAt`
- [x] `Application/Commands/ArchiveBlogPostCommand.cs` + handler — transitions Published → Archived
- [x] `Application/Commands/UpsertSeoPageCommand.cs` + handler — admin only; create or update SEO page by slug
- [x] `Application/Queries/GetPublishedBlogPostsQuery.cs` + handler — public; paginated list; filter by tag; returns summary DTOs (no full content)
- [x] `Application/Queries/GetBlogPostBySlugQuery.cs` + handler — public; returns full content; 404 if not Published
- [x] `Application/Queries/GetSeoPageBySlugQuery.cs` + handler — public; returns meta fields for a static page
- [x] `Application/Queries/GetAllBlogPostsAdminQuery.cs` + handler — admin only; all statuses; paginated
- [x] `Application/Queries/GetSitemapEntriesQuery.cs` + handler — public; returns all published post slugs + publishedAt for sitemap generation
- [x] `Application/DTOs/BlogPostSummaryDto.cs` — id, slug, title, excerpt, tags, publishedAt, readingTimeMinutes, ogImageUrl
- [x] `Application/DTOs/BlogPostDetailDto.cs` — all fields including full Markdown content
- [x] `Application/DTOs/SeoPageDto.cs`
- [x] `Application/DTOs/SitemapEntryDto.cs` — slug, publishedAt, changeFreq, priority

#### Presentation
- [x] `Presentation/Endpoints/BlogEndpoints.cs` — public:
  - `GET /api/v1/blog` — paginated list (query: `page`, `pageSize`, `tag`)
  - `GET /api/v1/blog/{slug}` — single post by slug
  - `GET /api/v1/blog/sitemap` — sitemap entries (called by Next.js `sitemap.ts`)
- [x] `Presentation/Endpoints/SeoPageEndpoints.cs` — public:
  - `GET /api/v1/pages/{slug}` — SEO page meta by slug
- [x] `Presentation/Endpoints/AdminBlogEndpoints.cs` — all require `PlatformAdmin` role:
  - `POST /api/v1/admin/blog` — create draft
  - `PUT /api/v1/admin/blog/{id}` — update draft
  - `POST /api/v1/admin/blog/{id}/publish`
  - `POST /api/v1/admin/blog/{id}/archive`
  - `GET /api/v1/admin/blog` — all posts (admin view)
  - `PUT /api/v1/admin/pages/{slug}` — upsert SEO page

#### Infrastructure
- [x] `Infrastructure/Persistence/ContentDbContext.cs` — schema `content`
- [x] `Infrastructure/Persistence/Schemas/content.schema.sql`
- [x] `Infrastructure/Repositories/BlogPostRepository.cs`, `SeoPageRepository.cs`
- [x] `Infrastructure/Configurations/BlogPostConfiguration.cs` — `tags` stored as PostgreSQL text array (`character varying[]`)
- [x] EF Core migrations

#### Module Registration
- [x] `ContentManagementModuleRegistration.cs`

### 5.15 PartnerNetwork

> Manages partner organizations (relocation/tech companies), their members, referral links, and direct reservations. Partners can bypass high deposit and credit check requirements for their referred users.

#### Project & References
- [x] `PartnerNetwork.csproj` — references SharedKernel, Infrastructure
- [x] Add to `.sln`

#### Domain
- [x] `Domain/Aggregates/PartnerOrganization.cs` — `Name`, `Type` (PartnerOrganizationType), `ContactEmail`, `Status` (PartnerOrganizationStatus: PendingVerification → Verified → Suspended), `AdminUserId`, `VerifiedAt`, `SuspendedAt`; methods: `Create()`, `Verify(IClock)`, `Suspend(IClock)`; raises `PartnerOrganizationVerifiedEvent`, `PartnerOrganizationSuspendedEvent`
- [x] `Domain/Entities/PartnerMember.cs` — `OrganizationId`, `UserId`, `Role` (PartnerMemberRole: Admin/Member), `JoinedAt`
- [x] `Domain/Entities/ReferralLink.cs` — `OrganizationId`, `Code` (unique), `MaxRedemptions`, `CurrentRedemptions`, `ExpiresAt`, `IsActive`; methods: `Redeem(userId, IClock)`, `Deactivate()`; raises `ReferralRedeemedEvent`
- [x] `Domain/Entities/ReferralRedemption.cs` — `ReferralLinkId`, `RedeemedByUserId`, `RedeemedAt`
- [x] `Domain/Entities/DirectReservation.cs` — `OrganizationId`, `HostUserId`, `GuestName`, `GuestEmail`, `ListingId`, `CheckIn`, `CheckOut`, `Status`; method: `LinkDealApplication(dealApplicationId)`
- [x] `Domain/Enums/PartnerOrganizationType.cs` — `Relocation`, `Tech`, `Other`
- [x] `Domain/Enums/PartnerOrganizationStatus.cs` — `PendingVerification`, `Verified`, `Suspended`
- [x] `Domain/Enums/PartnerMemberRole.cs` — `Admin`, `Member`
- [x] `Domain/Events/PartnerOrganizationVerifiedEvent.cs`
- [x] `Domain/Events/PartnerOrganizationSuspendedEvent.cs`
- [x] `Domain/Events/ReferralRedeemedEvent.cs` — `ReferralLinkId`, `RedeemedByUserId`, `OrganizationId`
- [x] `Domain/Events/DirectReservationCreatedEvent.cs`
- [x] `Domain/Interfaces/IPartnerOrganizationRepository.cs` — `GetByIdAsync`, `AddAsync`, `Update`, `UnitOfWork`

#### Application
- [x] `Application/Commands/RegisterPartnerOrganizationCommand.cs` + handler — self-registration; creates `PartnerOrganization` in `PendingVerification` status
- [x] `Application/Commands/VerifyPartnerOrganizationCommand.cs` + handler — PlatformAdmin only; transitions to `Verified`
- [x] `Application/Commands/AddPartnerMemberCommand.cs` + handler — adds a user as member to an organization
- [x] `Application/Commands/GenerateReferralLinkCommand.cs` + handler — generates unique referral code with usage limit and expiry
- [x] `Application/Commands/RedeemReferralLinkCommand.cs` + handler — validates code, increments redemption count, fires `ReferralRedeemedEvent` via `IEventBus`
- [x] `Application/Commands/CreateDirectReservationCommand.cs` + handler — partner creates reservation for employee/guest
- [x] `Application/Queries/GetPartnerOrganizationQuery.cs` + handler
- [x] `Application/Queries/ListPartnerMembersQuery.cs` + handler
- [x] `Application/Queries/ListReferralLinksQuery.cs` + handler
- [x] `Application/DTOs/PartnerOrganizationDto.cs`
- [x] `Application/DTOs/PartnerMemberDto.cs`
- [x] `Application/DTOs/ReferralLinkDto.cs`
- [x] `Application/DTOs/DirectReservationDto.cs`

#### Presentation
- [x] `Presentation/Endpoints/PartnerEndpoints.cs` — `MapPartnerEndpoints()`:
  - `POST /v1/partners` — register partner organization
  - `GET /v1/partners/{id}` — get organization details
  - `POST /v1/partners/{id}/verify` — PlatformAdmin only; verify organization
  - `POST /v1/partners/{id}/members` — add member to organization
  - `GET /v1/partners/{id}/members` — list members
  - `POST /v1/partners/{id}/referral-links` — generate referral link
  - `GET /v1/partners/{id}/referral-links` — list referral links
  - `POST /v1/partners/{id}/reservations` — create direct reservation
  - `POST /v1/referral/{code}/redeem` — redeem referral code (auto-verifies user)
- [x] `Presentation/Contracts/RegisterPartnerRequest.cs` — `Name`, `Type`, `ContactEmail`
- [x] `Presentation/Contracts/AddMemberRequest.cs` — `UserId`, `Role`
- [x] `Presentation/Contracts/GenerateReferralLinkRequest.cs` — `MaxRedemptions`, `ExpiresAt`
- [x] `Presentation/Contracts/CreateReservationRequest.cs` — `HostUserId`, `GuestName`, `GuestEmail`, `ListingId`, `CheckIn`, `CheckOut`

#### Infrastructure
- [x] `Infrastructure/Persistence/PartnerDbContext.cs` — schema `partner_network`, extends `BaseDbContext`
- [x] `Infrastructure/Persistence/PartnerDbContextFactory.cs` — `IDesignTimeDbContextFactory`
- [x] `Infrastructure/Configurations/PartnerOrganizationConfiguration.cs`
- [x] `Infrastructure/Configurations/PartnerMemberConfiguration.cs`
- [x] `Infrastructure/Configurations/ReferralLinkConfiguration.cs` — `Code` unique index
- [x] `Infrastructure/Configurations/ReferralRedemptionConfiguration.cs`
- [x] `Infrastructure/Configurations/DirectReservationConfiguration.cs`
- [x] `Infrastructure/Services/PartnerMembershipProvider.cs` — implements `IPartnerMembershipProvider` (SharedKernel); queries `PartnerDbContext.Members` by userId; returns partner organization ID or null
- [x] EF Core migrations

#### Module Registration
- [x] `PartnerNetworkModuleRegistration.cs` — DbContext, outbox, `IPartnerMembershipProvider` → `PartnerMembershipProvider`, MediatR handlers

---

## Phase 6 — API Gateway (`Lagedra.ApiGateway`)

### 6.1 Project Setup

- [x] Update `Lagedra.ApiGateway.csproj` — reference `Lagedra.Auth` + all 15 modules (including PartnerNetwork) + `Lagedra.TruthSurface` + `Lagedra.Compliance` + `Lagedra.Infrastructure`
- [x] Configure `Program.cs` — register all modules via `AddXxx(configuration)`, middleware pipeline (CORS, Authentication, Authorization), OpenAPI/Swagger with JWT Bearer security scheme, seed data on startup via `AuthDataSeeder.SeedAsync()`

### 6.2 Middleware

- [ ] `Middleware/AuthMiddleware.cs` — validate JWT Bearer token; extract `UserId`, `Role` into `HttpContext`; enforce `IsActive=true`
- [ ] `Middleware/ConsentMiddleware.cs` — verify `KYCConsent` + `DataProcessing` consents exist for data-processing endpoints
- [ ] `Middleware/IdempotencyMiddleware.cs` — `Idempotency-Key` header; cache response in memory/DB for 24h
- [ ] `Middleware/RateLimitingMiddleware.cs` — per-user monthly dispute cap (beta); use `System.Threading.RateLimiting`
- [x] `CorrelationIdMiddleware` — reads/generates `X-Correlation-Id`, pushes to Serilog `LogContext`, adds to response headers (implemented in `Lagedra.Infrastructure.Observability`)
- [x] `GlobalExceptionHandlerMiddleware` — catches unhandled exceptions, returns RFC 7807 Problem Details JSON (implemented in `Lagedra.Infrastructure.Observability`)

### 6.3 Endpoint Mapping (Minimal API)

> **Architecture decision:** No controllers in API Gateway. Each module defines its own Minimal API endpoints (e.g., `MapAuthEndpoints()`, `MapPartnerEndpoints()`). `Program.cs` calls each module's `Map*Endpoints()` extension method.

- [x] `app.MapAuthEndpoints()` — Auth (register, login, verify-email, resend-verification, refresh, logout, forgot/reset-password, me)
- [x] `app.MapTruthSurfaceEndpoints()` — TruthSurface (create, confirm, reconfirm, get, verify)
- [x] `app.MapApplicationEndpoints()` — Applications (submit, approve, reject, get, list)
- [x] `app.MapActivationEndpoints()` — Activation (activate deal)
- [x] `app.MapBillingEndpoints()` — Billing (status, proration quote)
- [x] `app.MapPaymentConfirmationEndpoints()` — Payment (details, status, confirm, confirm-platform-payment, dispute, resolve)
- [x] `app.MapListingEndpoints()` — Listings (create, update, publish, search, get)
- [x] `app.MapLocationEndpoints()` — Location (set approx, lock precise)
- [x] `app.MapIdentityEndpoints()` — Identity (start KYC, status)
- [x] `app.MapHostPaymentEndpoints()` — Host payment details (save, get)
- [x] `app.MapVerificationEndpoints()` — Verification (persona webhook)
- [x] `app.MapInsuranceEndpoints()` — Insurance (status, manual proof, quotes)
- [x] `app.MapInquiryEndpoints()` — Inquiry (unlock, questions, responses)
- [x] `app.MapRiskEndpoints()` — Risk (landlord risk view)
- [x] `app.MapComplianceMonitoringEndpoints()` — Compliance monitoring
- [x] `app.MapArbitrationEndpoints()` — Arbitration (file, evidence, mark complete, get, list)
- [x] `app.MapArbitratorEndpoints()` — Arbitrator (assign, decision, award)
- [x] `app.MapEvidenceEndpoints()` — Evidence (manifest, seal)
- [x] `app.MapUploadEndpoints()` — Uploads (request URL, complete)
- [x] `app.MapJurisdictionPackEndpoints()` — Jurisdiction packs
- [x] `app.MapNotificationPreferencesEndpoints()` — Notifications (history, preferences)
- [x] `app.MapPrivacyEndpoints()` — Privacy (consent, export, deletion)
- [x] `app.MapIntegrityEndpoints()` — Anti-abuse (flags, restrictions)
- [x] `app.MapBlogEndpoints()` — Blog (public read)
- [x] `app.MapSeoPageEndpoints()` — SEO pages (public read)
- [x] `app.MapAdminBlogEndpoints()` — Blog admin (CRUD, publish, archive)
- [x] `app.MapPartnerEndpoints()` — Partners (register, verify, members, referrals, reservations)
- [x] `app.MapListingDefinitionsEndpoints()` — Listing definitions (amenities, safety devices, considerations) — public read
- [x] `app.MapAdminListingDefinitionsEndpoints()` — Listing definitions admin (CRUD for amenities, safety, considerations)

### 6.4 Authentication & Authorization

- [x] ASP.NET Identity + JWT Bearer configured in `Lagedra.Auth`; `Program.cs` calls `app.UseAuthentication()` + `app.UseAuthorization()`
- [ ] Authorization policies: `RequireLandlord`, `RequireTenant`, `RequireArbitrator`, `RequirePlatformAdmin`, `RequireInsurancePartner`, `RequireInstitutionPartner`
- [ ] Stripe webhook endpoint: no auth; validates `Stripe-Signature` header via `StripeService`
- [ ] Persona webhook endpoint: no auth; validates `Persona-Signature` header via `PersonaClient`

### 6.5 API Configuration

- [ ] API versioning via `Asp.Versioning.Mvc` — route prefix `v1`
- [x] OpenAPI / Swagger with JWT Bearer security scheme (configured in `Program.cs`)
- [ ] `tools/openapi/lagedra.openapi.yaml` — generated from Swagger
- [ ] `tools/postman/Lagedra.postman_collection.json`
- [ ] `tools/postman/Lagedra.postman_environment.local.json`
- [ ] Global error handling — `ProblemDetails` (RFC 7807) via `app.UseExceptionHandler`
- [ ] FluentValidation integration — `AddFluentValidationAutoValidation()`; validators for all request models
- [x] CORS policy — `"Frontend"` policy configured; `app.UseCors("Frontend")`

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
- [ ] `PaymentConfirmationTimeoutJob` (ActivationAndBilling) — hourly
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

- [x] Move `src/Lagedra.Web/` → `apps/web/`; update `docker-compose.yml`
- [x] `pnpm` workspace member
- [x] `vite.config.ts`: path alias `@`, proxy `/api` → API gateway, `@react-google-maps/api` loaded via browser API key
- [x] `tsconfig.json`: strict mode, path aliases
- [x] ESLint (eslint-config-react-app or custom) + Prettier
- [x] Install: `react-router-dom`, `@tanstack/react-query`, `zustand`, `axios`
- [x] Install: `tailwindcss`, `@tailwindcss/forms`, `@tailwindcss/typography`, `shadcn/ui` (CLI init)
- [x] Install: `@react-google-maps/api` — Google Maps JavaScript API wrapper
- [x] Install: `@stripe/stripe-js`, `@stripe/react-stripe-js` — Stripe Elements
- [x] Install: `react-hook-form`, `zod`, `@hookform/resolvers` — form validation
- [x] `.env.example`: `VITE_API_BASE_URL`, `VITE_GOOGLE_MAPS_API_KEY`, `VITE_STRIPE_PUBLISHABLE_KEY`

### 8.2 App Shell

- [x] `src/main.tsx` — providers: `QueryClientProvider`, `AuthProvider`, `RouterProvider`
- [x] `src/app/App.tsx` — router outlet, global error boundary
- [x] `src/app/routes.tsx` — role-based lazy routes
- [x] `src/app/auth/AuthProvider.tsx` — JWT storage (`localStorage`), refresh logic, Zustand slice
- [x] `src/app/auth/RequireAuth.tsx` — redirects to login if no valid token
- [x] `src/app/auth/roles.ts` — role constants matching backend enums
- [x] `src/app/layout/Shell.tsx`, `Nav.tsx`, `Footer.tsx`, `ErrorBoundary.tsx`
- [x] `src/app/config.ts` — reads `VITE_*` env vars

### 8.3 API Client

- [x] `src/api/http.ts` — Axios instance: `Authorization: Bearer`, `X-Correlation-Id`, auto-refresh on 401
- [x] `src/api/endpoints.ts` — typed endpoint map
- [x] `src/api/types.ts` — DTOs synced with `packages/contracts`

### 8.4 UI Primitives (shadcn/ui base)

- [x] shadcn/ui components initialized: `Button`, `Card`, `Modal/Dialog`, `Table`, `Input`, `Select`, `Checkbox`, `RadioGroup`, `Badge`, `Alert`, `Skeleton`, `Pagination`, `Separator`, `Tabs`
- [x] Install `lucide-react` — Lucide icon library (included with shadcn/ui); used for dynamic icon rendering from `iconKey` strings stored in DB; `DynamicIcon` component maps `iconKey` → `<LucideIcon />` with fallback
- [x] `src/components/shared/Loader.tsx`
- [x] `src/components/shared/EmptyState.tsx`
- [x] `src/components/shared/FormError.tsx`

### 8.5 Feature Modules

#### Auth Pages
- [x] `features/auth/pages/LoginPage.tsx` — email + password form; `POST /v1/auth/login`
- [x] `features/auth/pages/RegisterPage.tsx` — email + password + role selection (Landlord/Tenant only)
- [x] `features/auth/pages/VerifyEmailPage.tsx` — token from URL param
- [x] `features/auth/pages/ForgotPasswordPage.tsx`
- [x] `features/auth/pages/ResetPasswordPage.tsx`
- [x] `features/auth/services/authApi.ts`

#### Listings
- [x] `features/listings/pages/SearchPage.tsx` — filter by stay range, price, approx location, amenities (multi-select checkbox); minimal non-promotional
- [x] `features/listings/pages/ListingDetailPage.tsx` — structured fields only; Google Map with approx pin pre-activation; precise address post-activation; amenities grid with Lucide icons grouped by category; safety devices list with icons; considerations list with icons; house rules section with icons; cancellation policy summary with type badge
- [x] `features/listings/pages/CreateListingPage.tsx` — fully structured form; jurisdiction-gated fields (AB 628 stove/fridge checkboxes for CA); amenity picker (grouped by category, checkbox grid), safety device picker, consideration picker, house rules form (time pickers, toggles), cancellation policy selector (type dropdown with auto-filled defaults, adjustable values)
- [x] `features/listings/components/ListingCard.tsx` — shows top 3–5 amenity icons as badges
- [x] `features/listings/components/ListingForm.tsx` — uses `react-hook-form` + `zod`; field visibility driven by jurisdiction
- [x] `features/listings/components/LocationPicker.tsx` — `@react-google-maps/api` `GoogleMap` + `Marker`; approx vs. precise state toggle
- [x] `features/listings/components/AmenityGrid.tsx` — renders amenities grouped by category; each item: Lucide icon + name; responsive grid (2–4 columns)
- [x] `features/listings/components/SafetyDeviceList.tsx` — renders safety devices with Lucide icons; check-mark style
- [x] `features/listings/components/ConsiderationList.tsx` — renders considerations with Lucide icons; warning style
- [x] `features/listings/components/HouseRulesSection.tsx` — structured display: check-in/out times, guest count, pet/smoking/party policies with icons and status (Allowed/Not Allowed)
- [x] `features/listings/components/CancellationPolicySummary.tsx` — type badge + refund window summary; tooltip with full details
- [x] `features/listings/components/DynamicIcon.tsx` — renders Lucide icon by `iconKey` string; fallback icon for unknown keys; used by all listing attribute displays
- [x] `features/listings/hooks/useListings.ts`, `useListingDetail.ts`, `useListingDefinitions.ts` — TanStack Query; `useListingDefinitions` fetches amenity/safety/consideration definitions for forms
- [x] `features/listings/services/listingApi.ts`

#### Applications
- [x] `features/applications/pages/ApplicationsPage.tsx` — landlord inbox; shows tenant's Verification Class, insurance state, deposit band
- [x] `features/applications/pages/ApplicationDetailPage.tsx` — full risk view; approve/reject actions
- [x] `features/applications/components/ApplicationCard.tsx`
- [x] `features/applications/components/ApplicationForm.tsx` — Persona KYC consent, document upload, affiliation declaration, military status checkbox
- [x] `features/applications/services/applicationApi.ts`

#### Structured Inquiry
- [x] `features/inquiry/pages/InquiryThreadPage.tsx` — question/answer history; hard-disabled UI after Truth Surface confirmation
- [x] `features/inquiry/components/InquiryQuestion.tsx` — predefined question dropdown only
- [x] `features/inquiry/components/InquiryResponseForm.tsx` — structured Yes/No / multi-choice / numeric
- [x] `features/inquiry/services/inquiryApi.ts`

#### Truth Surface
- [x] `features/truth-surface/pages/TruthSurfaceConfirmationPage.tsx` — line-by-line display; per-line confirm checkboxes; Platform Disclaimers acknowledgement (required before submit)
- [x] `features/truth-surface/components/TruthSnapshotViewer.tsx` — immutable snapshot; cryptographic proof display
- [x] `features/truth-surface/components/ConfirmButton.tsx` — disabled until all checkboxes ticked
- [x] Hard-coded system notice: "The Inquiry Service is now closed. All confirmed details are recorded in the Truth Surface" — displayed in-page after confirmation
- [x] `features/truth-surface/services/truthSurfaceApi.ts`

#### Activation & Billing
- [x] `features/activation-billing/pages/BillingPage.tsx` — current invoice, prorated amount, Stripe Elements payment method card
- [x] `features/activation-billing/pages/PaymentMethodPage.tsx` — `@stripe/react-stripe-js` `CardElement` or `PaymentElement`
- [x] Non-custodial disclaimer displayed: "Lagedra does not receive, transmit, or hold these funds. These instructions are provided solely to facilitate your direct settlement."
- [x] `features/activation-billing/services/billingApi.ts`

#### Compliance
- [x] `features/compliance/pages/ComplianceStatusPage.tsx` — violations list, cure windows, insurance lapse alerts

#### Arbitration
- [x] `features/arbitration/pages/CaseListPage.tsx` — case list with status/tier
- [x] `features/arbitration/pages/CaseDetailPage.tsx` — evidence slots, timeline, decision display
- [x] `features/arbitration/components/EvidenceUpload.tsx` — structured slot upload; calls presigned URL; file hash displayed
- [x] `features/arbitration/components/CaseTimeline.tsx`
- [x] `features/arbitration/services/arbitrationApi.ts`

#### Trust Ledger
- [x] `features/trust-ledger/pages/TrustLedgerPage.tsx` — public pseudonymized view; full detail for involved parties only
- [x] `features/trust-ledger/components/LedgerEntryList.tsx`

#### Evidence
- [x] `features/evidence/services/evidenceApi.ts` — presigned URL request, upload completion

#### Notifications (In-App)
- [x] `features/notifications/pages/NotificationsPage.tsx` — in-app notification history (email sent = shown here too)
- [x] `features/notifications/services/notificationApi.ts`

#### Profile
- [x] `features/profile/pages/ProfilePage.tsx` — identity status, affiliation, insurance status, Verification Class
- [x] `features/profile/pages/VerificationStatusPage.tsx` — Persona KYC progress, background check status
- [x] `features/profile/services/profileApi.ts`

### 8.6 Utils

- [x] `src/utils/format.ts` — date (US locale), money (cents → `$XX.XX`), percentage formatters
- [x] `src/utils/validation.ts` — shared `zod` schemas (email, password, stay range)

---

## Phase 9 — Admin App (`apps/admin`)

### 9.1 Project Setup

- [x] Create `apps/admin/` with Vite + React + TypeScript + Tailwind + shadcn/ui + Zustand
- [x] Separate auth context (admin role only: `PlatformAdmin`)
- [x] `apps/admin/package.json`, `vite.config.ts`, `tsconfig.json`, `.env.example`
- [x] Install same core packages as `apps/web` (excluding Google Maps and Stripe Elements)

### 9.2 Admin Pages (Ops Dashboard)

- [x] `pages/InsuranceUnknownQueue.tsx` — deals in "Status: Unknown"; manual verification portal; 24h SLA countdown indicator
- [x] `pages/FraudFlags.tsx` — fraud flags by severity (High/Medium/Low); review/resolve workflow; 24h/72h SLA display
- [x] `pages/ArbitrationBacklog.tsx` — caseload per arbitrator; SLA status; triage view; overflow assignment button
- [x] `pages/JurisdictionPackVersions.tsx` — draft/pending/active packs; dual-control approval workflow (2nd approver view)
- [x] `pages/EvidenceReview.tsx` — malware scan queue; infected file quarantine; manual evidence review
- [x] `pages/AuditSearch.tsx` — full audit event log; filter by user, event type, date range
- [x] `pages/ManualVerification.tsx` — Persona KYC manual review fallback queue; ≤ 24h SLA
- [x] `pages/ComplianceViolations.tsx` — all violations across all deals; filter by category/status
- [x] `pages/UserRestrictions.tsx` — account restrictions/suspensions/bans; manage restrictions
- [x] `pages/DualControlApprovals.tsx` — pending pack approvals requiring 2nd approver
- [x] `pages/BlogPosts.tsx` — full CRUD for blog posts; status badge (Draft/Published/Archived); publish/archive actions; Markdown preview pane (react-markdown)
- [x] `pages/BlogPostEditor.tsx` — rich Markdown editor (`@uiw/react-md-editor` or plain `<textarea>`); slug preview; meta fields (metaTitle, metaDescription, ogImageUrl); tag input; estimated reading time display
- [x] `pages/SeoPages.tsx` — list and edit static SEO page meta (e.g. `/how-it-works`, `/pricing`, `/about`); noIndex toggle
- [x] `pages/ListingDefinitions.tsx` — manage amenity definitions (grouped by category), safety device definitions, consideration definitions; CRUD with icon picker (Lucide icon name selector), sort order, active/inactive toggle; preview rendered icon next to name

### 9.3 Admin API Client

- [x] `api/http.ts`, `api/endpoints.ts`, `api/types.ts` — admin-scoped; all admin endpoints require `PlatformAdmin` role

---

## Phase 10 — Shared Packages (`packages/`)

### 10.1 UI Component Library (`packages/ui`)

- [x] `packages/ui/package.json`
- [x] Re-exports from shadcn/ui + custom overrides: `Button`, `Input`, `Modal`, `Select`, `Checkbox`, `RadioGroup`, `DatePicker`, `Badge`, `Alert`
- [x] `src/index.ts` — barrel export

### 10.2 Shared Contracts (`packages/contracts`)

- [x] `packages/contracts/package.json`
- [x] `src/enums.ts` — `VerificationClass`, `InsuranceState`, `ViolationCategory`, `DealStatus`, `ArbitrationTier`, `NotificationType`, `UserRole`, `ConsentType`
- [x] `src/dtos.ts` — all DTO types (synced manually with backend, or generated via NSwag)
- [x] `src/events.ts` — domain event type definitions for any future cross-app usage
- [x] `src/index.ts`

### 10.3 Test Utilities (`packages/test-utils`)

- [x] `packages/test-utils/package.json`
- [x] `src/renderWithProviders.tsx` — React Testing Library wrapper with all providers (QueryClient, Router, Auth)
- [x] `src/mockServer.ts` — MSW handler setup for all API endpoints
- [x] `src/index.ts`

---

## Phase 10.5 — Marketing Site (`apps/marketing`)

> Public-facing, SEO-first website built with **Next.js 15 (App Router)**. Handles the landing page, blog, static content pages, and all SEO infrastructure. The authenticated product lives in `apps/web`; this app is entirely unauthenticated and optimized for search indexing, Core Web Vitals, and content delivery.
>
> Architecture principle: `apps/marketing` is a **standalone Next.js app** — it calls the public, unauthenticated read endpoints of the `ContentManagement` module for blog data, and has its own `next.config.ts`, `Dockerfile`, and Nginx routing (`/blog`, `/how-it-works`, etc.).

### 10.5.1 Project Setup

- [x] Bootstrap `apps/marketing/` with Next.js 15 App Router + TypeScript: `npx create-next-app@latest marketing --ts --tailwind --eslint --app --src-dir`
- [x] `apps/marketing/package.json` — workspace member
- [x] `apps/marketing/next.config.ts` — `output: 'standalone'` for Docker; `NEXT_PUBLIC_API_URL` env var; image domains whitelist (for OG images)
- [x] `apps/marketing/tsconfig.json`
- [x] `apps/marketing/.env.example` — `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_SITE_URL`, `NEXT_PUBLIC_GA_MEASUREMENT_ID`
- [x] Install additional packages: `react-markdown`, `remark-gfm`, `rehype-highlight` (Markdown rendering), `@vercel/og` or `next/og` (OG image generation)
- [x] Configure Tailwind CSS + shadcn/ui (`npx shadcn@latest init`) — same design system as `apps/web`
- [x] `apps/marketing/src/lib/api.ts` — typed fetch wrapper calling `NEXT_PUBLIC_API_URL`

### 10.5.2 Layout & Navigation

- [x] `src/app/layout.tsx` — root layout with `<html lang="en">`; global `<GoogleTagManager>` (or GA script); default `metadata` export
- [x] `src/components/Header.tsx` — top nav: logo, How It Works, Pricing, Blog, FAQ, "Get Started" CTA (links to `apps/web`)
- [x] `src/components/Footer.tsx` — links, legal, social, copyright
- [x] `src/components/MobileNav.tsx` — hamburger menu for mobile

### 10.5.3 Pages

- [x] `src/app/page.tsx` — **Home / Landing Page**
  - Hero: value proposition headline, CTA ("Protect Your Rental")
  - How it works (3-step explainer)
  - Trust signals / stats
  - Feature highlights (Trust Ledger, Verification Class, Arbitration)
  - FAQ accordion
  - Footer CTA
  - Full static generation (`export const dynamic = 'force-static'`)
- [x] `src/app/how-it-works/page.tsx` — detailed product walkthrough; static
- [x] `src/app/pricing/page.tsx` — protocol fee breakdown; static
- [x] `src/app/about/page.tsx` — mission, team, founding story; static
- [x] `src/app/contact/page.tsx` — contact form (sends to `IEmailService` via a `/api/contact` Next.js Route Handler)
- [x] `src/app/faq/page.tsx` — accordion FAQ; static
- [x] `src/app/legal/terms/page.tsx` — Terms of Service; static Markdown render
- [x] `src/app/legal/privacy/page.tsx` — Privacy Policy; static Markdown render

### 10.5.4 Blog

- [x] `src/app/blog/page.tsx` — **Blog List Page**
  - `fetch` from `GET /api/v1/blog` (with `next: { revalidate: 300 }` — ISR every 5 min)
  - Pagination, tag filter
  - Post card: title, excerpt, tags, publishedAt, reading time, OG image thumbnail
- [x] `src/app/blog/[slug]/page.tsx` — **Blog Post Detail Page**
  - `fetch` from `GET /api/v1/blog/{slug}` (ISR; `revalidate: 300`)
  - `generateStaticParams()` — pre-renders all published posts at build time
  - `generateMetadata()` — dynamic `title`, `description`, `openGraph` from post meta fields
  - Renders `content` (Markdown) via `react-markdown` + `remark-gfm` + `rehype-highlight`
  - Structured data: JSON-LD `Article` schema (author, publishedAt, headline, image)
  - Related posts section (same tags)
- [x] `src/app/blog/tag/[tag]/page.tsx` — **Tag Filter Page**; `generateStaticParams()` for all known tags

### 10.5.5 SEO Infrastructure

- [x] `src/app/sitemap.ts` — **Dynamic Sitemap** (Next.js App Router built-in):
  - Static pages: `/`, `/how-it-works`, `/pricing`, `/about`, `/faq`, `/contact`, `/blog`
  - Dynamic blog posts: fetched from `GET /api/v1/blog/sitemap` at build/revalidation time
  - Outputs `<url>`, `<loc>`, `<lastmod>`, `<changefreq>`, `<priority>` for each
- [x] `src/app/robots.ts` — **Robots.txt** (Next.js built-in): `User-agent: *`, `Allow: /`, `Disallow: /api/`, `Sitemap: https://lagedra.com/sitemap.xml`
- [x] `src/app/og/route.tsx` — **OG Image Route Handler** (Next.js `ImageResponse`): generates dynamic OG images for blog posts from title + slug params
- [x] `src/app/rss.xml/route.ts` — **RSS Feed Route Handler**: fetches all published posts; returns `application/rss+xml` with proper `<channel>` and `<item>` entries
- [x] `src/app/api/contact/route.ts` — **Contact Form API Route**: validates body (zod), calls `POST /api/v1/contact` on backend (which uses `IEmailService`); rate-limited
- [x] Default `metadata` in `layout.tsx`: `title.template`, `description`, `openGraph` (site name, type, locale), `twitter` (card type)
- [x] JSON-LD Organization schema on homepage (`<script type="application/ld+json">`)
- [x] Canonical URL set on every page via `alternates.canonical` in metadata

### 10.5.6 Performance & Core Web Vitals

- [x] All images via `next/image` with explicit `width`/`height` or `fill` — prevents CLS
- [x] Fonts via `next/font/google` — eliminates FOIT/FOUT, self-hosted at build time
- [x] No layout shift from dynamic content: skeleton loaders or static fallbacks
- [x] `next.config.ts`: `compress: true`, bundle analyzer script (`@next/bundle-analyzer`)

### 10.5.7 Dockerfile & Nginx Routing

- [x] `apps/marketing/Dockerfile` — multi-stage: `node:20-alpine` build + standalone output copy
- [x] Nginx `deploy/nginx/nginx.conf` — route `/blog`, `/how-it-works`, `/pricing`, `/about`, `/contact`, `/faq`, `/legal` → marketing service (port 3001); all other non-API routes → web app (port 3000); `/api` → backend (port 5000)

---

## Phase 11 — Testing

### 11.1 Architecture Tests (`tests/Lagedra.Tests.Architecture`)

- [x] `Lagedra.Tests.Architecture.csproj` — `NetArchTest.Rules`; references all module + gateway projects
- [x] `ModuleDependencyTests.cs` — no module references another module directly (only SharedKernel, Infrastructure, TruthSurface, Compliance, JurisdictionPacks)
- [x] `DomainLayerTests.cs` — Domain has no reference to Infrastructure, EF Core, or HTTP
- [x] `ApplicationLayerTests.cs` — Application has no reference to Infrastructure or EF Core
- [x] `NamingConventionTests.cs` — aggregate roots extend `AggregateRoot<>`, value objects extend `ValueObject`, events implement `IDomainEvent`
- [x] `ApiGatewayHasNoBusinessLogicTests.cs` — controllers only inject `IMediator`; no direct domain or repository injection
- [x] `NoProtectedClassAttributesInRiskPolicyTests.cs` — `VerificationClassPolicy` references no protected-class attribute names

### 11.2 Unit Tests (`tests/Lagedra.Tests.Unit`)

- [x] `Lagedra.Tests.Unit.csproj` — xUnit, FluentAssertions, NSubstitute, Bogus
- [x] Auth: `JwtTokenServiceTests.cs`, `RefreshTokenServiceTests.cs`, `RegisterUserTests.cs`
- [x] SharedKernel: `AggregateRootTests.cs`, `ValueObjectTests.cs`, `ResultTests.cs`
- [x] TruthSurface: `TruthSnapshotTests.cs`, `HashingTests.cs`, `MerkleTreeTests.cs`
- [x] Compliance: `TrustLedgerEntryTests.cs` (append-only), `ViolationTests.cs`
- [x] ActivationAndBilling: `BillingPolicyTests.cs` ($79 proration formula), `DealApplicationTests.cs`, `BillingAccountTests.cs`
- [x] IdentityAndVerification: `VerificationStatusTests.cs`, `AffiliationVerificationTests.cs`
- [x] InsuranceIntegration: `UnknownGraceWindowPolicyTests.cs` (72h, API failure vs. tenant inaction)
- [x] ListingAndLocation: `StayRangeTests.cs` (30–180 validation), `ListingTests.cs`
- [x] StructuredInquiry: `InquirySessionTests.cs` (lock on Truth Surface confirmation), `ContactInfoBypassDetectionTests.cs`
- [x] VerificationAndRisk: `VerificationClassPolicyTests.cs` (all inputs, no protected-class), `DepositRecommendationPolicyTests.cs` (jurisdiction cap, adverse action)
- [x] ComplianceMonitoring: `ViolationCategoryTests.cs` (A–G + Other mapping)
- [x] Arbitration: `EvidenceMinimumThresholdPolicyTests.cs`, `ArbitrationCaseTests.cs` (SLA clock, cap enforcement)
- [x] JurisdictionPacks: `FieldGatingRuleTests.cs`, `CaliforniaPackTests.cs` (AB 12, SB 611, AB 628, AB 2801, AB 414, JCO)
- [x] Evidence: `EvidenceManifestTests.cs` (seal invariant), `FileHashTests.cs`
- [x] Privacy: `DeletionRequestTests.cs` (legal hold blocking), `RetentionPeriodTests.cs`
- [x] AntiAbuseAndIntegrity: `CollusionPatternTests.cs`, `InquiryAbuseDetectionTests.cs`
- [x] ContentManagement: `BlogPostTests.cs` (slug uniqueness, Draft→Published transition, PublishedAt immutability), `SeoPageTests.cs`

### 11.3 Integration Tests (`tests/Lagedra.Tests.Integration`)

- [x] `Lagedra.Tests.Integration.csproj` — `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing`; real PostgreSQL via Docker
- [x] `AuthIntegrationTests.cs` — register → verify email → login → refresh → revoke
- [x] `TruthSurfaceIntegrationTests.cs` — full snapshot confirmation, hash verification
- [x] `BillingActivationIntegrationTests.cs` — application → approval → deal activation (Stripe mocked)
- [x] `InsuranceIntegrationTests.cs` — API failure → Unknown → 72h → manual upload flow
- [x] `ArbitrationIntegrationTests.cs` — full case filing through decision
- [x] `PrivacyIntegrationTests.cs` — deletion request blocked by legal hold
- [x] `JurisdictionPackIntegrationTests.cs` — LA pack field gating end-to-end
- [x] `StripeWebhookIntegrationTests.cs` — payment succeeded / failed / chargeback webhook handling
- [x] `PersonaWebhookIntegrationTests.cs` — KYC complete / fail / background check result
- [x] `ContentManagementIntegrationTests.cs` — create draft → publish → public GET by slug → 404 for archived

### 11.4 Frontend Tests (`apps/web`)

- [x] Vitest + React Testing Library configured
- [x] MSW (`mockServiceWorker.js`) for API mocking
- [x] Tests: `LoginPage.test.tsx`, `TruthSurfaceConfirmationPage.test.tsx`, `LocationPicker.test.tsx`, `BillingPolicy.test.ts` (proration formula)

### 11.5 Marketing Site Tests (`apps/marketing`)

- [x] Vitest configured (Next.js compatible — `@vitejs/plugin-react`, jsdom)
- [x] `BlogList.test.tsx` — renders post cards from mocked API response; pagination
- [x] `BlogPostPage.test.tsx` — renders Markdown content; shows 404 for unknown slug (MSW mock)
- [x] `Sitemap.test.ts` — sitemap entries include all published slugs; static pages always present
- [x] `RssFeed.test.ts` — valid XML output; correct `<item>` count matches published posts

---

## Phase 12 — Documentation

### 12.1 Architecture Decision Records (`docs/decisions/`)

- [x] `ADR-0001-modular-monolith.md` — why modular monolith over microservices at launch
- [x] `ADR-0002-schema-per-module.md` — PostgreSQL schema-per-module isolation
- [x] `ADR-0003-truth-surface-signing.md` — HMAC-SHA256 + canonical JSON + Merkle tree
- [x] `ADR-0004-outbox-required.md` — Transactional Outbox for domain event reliability
- [x] `ADR-0005-evidence-immutable-manifest.md` — sealed manifest + SHA-256 file hashing
- [x] `ADR-0006-aspnet-identity.md` — ASP.NET Identity over Auth0 (cost, control, VPS-friendly)
- [x] `ADR-0007-mailkit-brevo.md` — MailKit + Brevo over SendGrid (EU GDPR, cost, control)
- [x] `ADR-0008-minio-clamav.md` — self-hosted MinIO + ClamAV for VPS deployment
- [x] `ADR-0009-deal-in-activation.md` — Deal lifecycle owns in ActivationAndBilling module
- [x] `ADR-0010-google-maps-persona-stripe.md` — confirmed third-party provider decisions
- [x] `ADR-0011-nextjs-marketing-site.md` — why Next.js (App Router + ISR) for `apps/marketing` over Vite SPA (SEO, sitemap, OG images, Core Web Vitals)

### 12.2 Architecture Docs (`docs/architecture/`)

- [x] `00-context.md` — system context, actors, external systems
- [x] `01-modular-monolith.md` — module boundary rules, communication patterns, outbox
- [x] `02-truth-surface.md` — cryptographic proof model, confirmation flow, inquiry lock
- [x] `03-eventing-outbox.md` — domain event lifecycle, outbox processor, Quartz.NET
- [x] `04-data-retention-privacy.md` — retention periods, anonymization, legal holds, GDPR/CCPA/FCRA
- [x] `05-evidence-storage.md` — MinIO upload flow, ClamAV scan, metadata strip, sealing
- [x] `06-arbitration-engine.md` — tier system, evidence schema, SLA, backlog controls
- [x] `07-jurisdiction-packs.md` — versioning, dual-control, effective-date rules, LA v1
- [x] `08-anti-abuse.md` — abuse detection patterns, Trust Ledger gaming, inquiry integrity
- [x] `09-observability.md` — Serilog, OpenTelemetry, health checks
- [x] `10-access-control-rbac.md` — ASP.NET Identity roles, JWT claims, permissions matrix

### 12.3 Runbooks (`docs/runbooks/`)

- [x] `insurance-unknown-queue.md` — ops procedure, 24h SLA, escalation
- [x] `arbitration-backlog-incident.md` — incident declaration, triage, overflow, daily reporting
- [x] `fraud-flag-triage.md` — severity classification, 24h/72h SLA, Security + Legal escalation
- [x] `pii-incident-response.md` — breach notification, regulatory reporting, user communication

---

## Phase 13 — Deployment (VPS + Docker Compose + Nginx)

### 13.1 Dockerfile

- [x] Multi-stage `Dockerfile`:
  - Stage 1: `mcr.microsoft.com/dotnet/sdk:8.0` — `dotnet restore` + `dotnet publish -c Release`
  - Stage 2: `mcr.microsoft.com/dotnet/aspnet:8.0` — non-root user; copy published output
- [x] `.dockerignore` — exclude `bin/`, `obj/`, `.git/`, `node_modules/`

### 13.2 docker-compose.yml (Primary Deployment)

- [x] Services: `postgres`, `minio`, `clamav`, `api` (Lagedra.ApiGateway), `worker` (Lagedra.Worker), `web` (Nginx serving built React app), `admin` (Nginx serving built admin app), `marketing` (Next.js standalone — port 3001)
- [x] `minio` service: `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, volume mount, console at `:9001`
- [x] `clamav` service: `clamav/clamav:latest`, freshclam enabled, health check
- [x] Environment variables via `.env` file (never commit secrets)
- [x] Health check for all services

### 13.3 Nginx (`deploy/nginx/`)

- [x] `nginx.conf` — upstream API gateway, upstream web, upstream admin; gzip; security headers (CSP, HSTS, X-Frame-Options)
- [x] `sites-enabled/lagedra.conf` — TLS (Let's Encrypt / Certbot); reverse proxy `/api` → API gateway; `/` → web; `/admin` → admin app
- [x] `tools/scripts/certbot-renew.sh` — renew Let's Encrypt certificates

### 13.4 CI/CD — GitHub Actions

- [x] `.github/workflows/ci.yml` — on PR: `dotnet format --verify-no-changes` + `dotnet test` (all three test projects) + `pnpm lint` + `pnpm test`
- [x] `.github/workflows/cd.yml` — on merge to `main`: `docker build` + `docker push` to GitHub Container Registry + SSH deploy to VPS via `appleboy/ssh-action`
- [x] Architecture test gate: `Lagedra.Tests.Architecture` must pass before merge
- [x] Secret scanning: GitHub's built-in secret scanning enabled on repo
- [x] Container image scan: Trivy action on built image

### 13.5 Environment Files

- [x] `deploy/env/local.env` — all vars documented with example values (no real secrets)
- [x] `deploy/env/staging.env` — documented placeholders
- [x] `deploy/env/prod.env` — documented placeholders
- [x] Required env vars documented:
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

- [x] `deploy/k8s/` — Kubernetes manifests (namespace, deployments, services, ingress, configmap, secrets template, PostgreSQL statefulset)
- [x] `deploy/terraform/` — IaC modules for VPS provider / cloud migration

---

## Phase 14 — External Integrations

> All integrations read-only or webhook-based. Platform never holds/transmits/guarantees funds.

### 14.1 Stripe (Protocol Fee Billing)

- [x] `Stripe.net` NuGet integrated in `Lagedra.Infrastructure`
- [x] Stripe products/prices configured: "Protocol Fee - Standard" ($79/month), "Protocol Fee - Institutional Partner" ($39/month)
- [x] `CreateStripeCustomerCommand` — creates Stripe Customer for landlord on first deal
- [x] `ActivateDealCommand` — creates Stripe Subscription (prorated from activation date)
- [x] `StopBillingCommand` — cancels Stripe Subscription at period end
- [x] Webhook endpoint (no auth): validates `Stripe-Signature`; dispatches `invoice.paid`, `invoice.payment_failed`, `charge.dispute.created`
- [x] Frontend: Stripe Elements (`PaymentElement`) for payment method capture; publishable key from `VITE_STRIPE_PUBLISHABLE_KEY`

### 14.2 Google Maps Platform (Geocoding + Maps)

- [x] **Backend**: `GoogleMapsGeocodingService` — `HttpClient` calls to `https://maps.googleapis.com/maps/api/geocode/json`; parses address components for `city`, `county`, `state`, `country`; derives `JurisdictionCode` (e.g. `US-CA-LA`); API key from env; Polly retry
- [x] **Frontend**: `@react-google-maps/api`; `GoogleMap` component; `Marker` for approx pin; `useJsApiLoader` with API key from `VITE_GOOGLE_MAPS_API_KEY`; Maps JavaScript API enabled in Google Cloud Console
- [x] Google Cloud Console: enable Geocoding API, Address Validation API, Maps JavaScript API; restrict API key by domain

### 14.3 Persona (KYC + Background Check)

- [x] `PersonaClient` — `HttpClient` to `https://withpersona.com/api/v1/`; `Authorization: Bearer {PERSONA_API_KEY}`; Polly retry
- [x] KYC Inquiry Template configured in Persona dashboard: liveness check + government ID + selfie
- [x] Background Check Report configured in Persona: FCRA-compliant; criminal, sex offender, global watchlist
- [x] Webhook endpoint: validates `Persona-Signature` header (HMAC-SHA256); dispatches Complete/Fail commands
- [x] Manual review queue: high-risk flags → ops dashboard (admin `ManualVerification.tsx` page)

### 14.4 MailKit + Brevo SMTP (Email)

- [x] `MailKitEmailService` — `MailKit.Net.Smtp.SmtpClient`; connects to `smtp-relay.brevo.com:587`; STARTTLS; authenticates with Brevo API key as password
- [x] `MimeMessage` built with `MimeKit`; `From: noreply@lagedra.com`; HTML + plain-text parts
- [x] All 12 required system notice templates implemented as inline C# string templates
- [x] Polly retry: 3 attempts, 2s / 4s / 8s exponential back-off on SMTP errors
- [x] Brevo dashboard: sender domain verified (SPF + DKIM), bounce handling configured

### 14.5 MinIO (Object Storage — Evidence + Exports)

- [x] MinIO running in Docker (self-hosted, S3-compatible)
- [x] `AWSSDK.S3` (`AmazonS3Client`) configured with MinIO endpoint
- [x] Buckets: `evidence` (7-year lifecycle policy), `exports` (48h lifecycle)
- [x] Presigned upload URLs: 15-minute expiry
- [x] Presigned download URLs: 1-hour expiry
- [x] Server-side encryption: MinIO SSE-S3 enabled
- [x] MinIO Console accessible at `:9001` (admin only)

### 14.6 ClamAV (Antivirus)

- [x] ClamAV running in Docker: `clamav/clamav:latest`; freshclam virus database auto-update
- [x] `ClamAvService` — TCP socket scan via `nClam` NuGet (or REST if using `clamav/clamav-rest`); returns Clean/Infected
- [x] Infected files: quarantined to `evidence/quarantine/` prefix; never served to users; ops notified via email
- [x] Add `nClam` to `Directory.Packages.props`

### 14.7 Insurance API (MGA Partner — TBD)

- [x] `InsuranceApiClient` implemented as stub; `IInsuranceApiClient` contract defined
- [x] Webhook endpoint implemented; handles policy change events
- [x] 72h grace window + manual fallback fully implemented (does not depend on real integration)
- [x] Real MGA partner integration to be wired when LOI converts to contract

---

## Phase 15 — Beta Readiness Gate

> **Public beta may begin ONLY when ALL 7 conditions are marked complete.**

- [x] **Gate 1 — Identity Verification** — Persona KYC + background check fully implemented; liveness, document auth, synthetic ID detection; manual review queue in admin dashboard operational; ≤ 24h manual review SLA confirmed
- [x] **Gate 2 — Insurance Integration** — Insurance API stub + manual fallback tested end-to-end; Active / NotActive / Unknown states handled; 72h grace window verified (both API-failure path and tenant-inaction path); manual proof upload portal functional; 24h SLA confirmed
- [x] **Gate 3 — Truth Surface Integrity** — HMAC-SHA256 signing + canonical JSON hashing verified on all snapshots; append-only audit log integrity confirmed; `SnapshotVerificationJob` passing; `InquiryClosed=true` included in hash; Merkle tree partial proof working
- [x] **Gate 4 — Jurisdiction Pack** — California / LA v1 pack unit tested for all field gating (AB 12, SB 611, AB 628, AB 2801, AB 414, JCO); dual-control approval workflow functional; effective-date rules verified; CaliforniaPackTests all green
- [x] **Gate 5 — Arbitration Panel** — Minimum 3 arbitrators signed, onboarded, and trained on protocol; reserve capacity pool contracted; 14-day decision SLA agreement in place; caseload cap (20 hard / 15 soft) implemented and tested; backlog escalation email automation working
- [x] **Gate 6 — Monitoring Dashboard** — Admin app live with all 10 ops pages; insurance unknown queue, fraud flags, arbitration backlog, jurisdiction pack versions all functional; email alerts for all SLA breaches working; Serilog logs to file with rotation
- [x] **Gate 7 — Incident Response** — Tabletop simulation completed: insurance partner outage, arbitration backlog spike, PII breach, fraud flag surge; at least one live drill; RCA process documented in `docs/runbooks/`

---

## KPIs & Pilot Metrics (Instrument Before Beta)

- [x] **Deposit Reduction Delta (DRD)** — median % change in deposit vs. landlord baseline; per deal at activation; segmented by Verification Class + insurance state; logged to Trust Ledger
- [x] **Deposit Adoption Rate** — % of eligible deals (Low/Medium Risk + Active/InstitutionBacked) where landlord sets deposit within recommended band or lower
- [x] **Dispute Rate** — cases filed per 100 active deal-months
- [x] **Median Resolution Time** — case filed → decision issued
- [x] **Recovery Evidence Rate** — % of cases where evidence meets minimum schema on first submission
- [x] **Protocol Fee Churn Rate** — % of deals that lapse on fee within first billing cycle
- [x] **Publish quarterly transparency report** — anonymized: DRD by class, dispute rate, resolution time, evidence rate

---

## Pilot Programs

- [x] **Deposit Reduction Guarantee Pilot** — first 100 Low Risk + Insured tenants; if landlord accepts recommended deposit and covered loss occurs, facilitate supplemental insurance claim; track DRD as primary KPI
- [x] **Institutional Partner Pilot** — first 12 months: $39/month for Verified Institutional Partners; track activation volume from institutional channel
- [x] **Phase 1.5 Invite-Only Public Beta** — California only; waitlist-gated; concurrent with institutional pilot; stress-test user flows with real non-institutional users

---

---

## Soft Delete — Cross-Module Migrations

> After `Entity<TId>` implements `ISoftDeletable`, every entity table gets `IsDeleted` (bool, default false) + `DeletedAt` (DateTime?, nullable).
> Run one migration per DbContext/module.

- [x] `auth` schema — `AspNetUsers` (ApplicationUser soft delete)
- [x] `listings` schema — Listing, ListingAmenity, ListingSafetyDevice, ListingConsideration, AmenityDefinition, SafetyDeviceDefinition, PropertyConsiderationDefinition
- [x] `activation_billing` schema — DealApplication, DealPaymentConfirmation, BillingAccount
- [x] `identity_verification` schema — IdentityProfile, VerificationCase
- [x] `insurance` schema — InsurancePolicyRecord
- [x] `inquiry` schema — InquirySession
- [x] `arbitration` schema — ArbitrationCase
- [x] `evidence` schema — EvidenceManifest
- [x] `notifications` schema — Notification
- [x] `partner_network` schema — PartnerOrganization
- [x] `jurisdiction` schema — JurisdictionPack
- [x] `risk` schema — RiskProfile
- [x] `content` schema — BlogPost
- [x] `integrity` schema — AbuseCase
- [x] `privacy` schema — UserConsent
- [x] `truth_surface` schema — TruthSnapshot
- [x] `compliance` schema — Compliance
- [x] `compliance_monitoring` schema — ComplianceMonitoring

---

*Last updated: 2026-02-27. Technology stack locked. Update checkboxes as work is completed.*
*Auth profile fields, protocol fee configuration, arbitration filing fee, real-time notification system, and soft delete migrations added.*
*Each `[ ]` → `[x]` is a step toward a defensible, enforceable, institution-grade mid-term rental protocol.*
