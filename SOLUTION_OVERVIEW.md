# Lagedra — Solution Overview & API Documentation

> **Lagedra** is an institution-grade mid-term rental trust protocol. It provides a legally defensible, cryptographically sealed framework for landlords and tenants engaging in mid-term (30–365 day) residential rentals. The platform handles everything from listing creation and tenant verification through deal activation, payment confirmation, compliance monitoring, and dispute resolution.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Technology Stack](#technology-stack)
3. [Solution Structure](#solution-structure)
4. [Projects](#projects)
5. [API Endpoint Reference](#api-endpoint-reference)
6. [Business Flows](#business-flows)
   - [Tenant Registration & Verification Flow](#1-tenant-registration--verification-flow)
   - [Landlord Onboarding & Listing Flow](#2-landlord-onboarding--listing-flow)
   - [Booking Flow (Application → Deal Activation)](#3-booking-flow-application--deal-activation)
   - [Structured Inquiry Flow](#4-structured-inquiry-flow)
   - [Truth Surface Flow](#5-truth-surface-flow)
   - [Payment Confirmation Flow](#6-payment-confirmation-flow)
   - [Billing & Subscription Flow](#7-billing--subscription-flow)
   - [Insurance Flow](#8-insurance-flow)
   - [Compliance & Trust Ledger Flow](#9-compliance--trust-ledger-flow)
   - [Arbitration Flow](#10-arbitration-flow)
   - [Evidence Management Flow](#11-evidence-management-flow)
   - [Damage Claim Flow](#12-damage-claim-flow)
   - [Booking Cancellation Flow](#13-booking-cancellation-flow)
   - [Privacy & Consent Flow](#14-privacy--consent-flow)
   - [Notification Flow](#15-notification-flow)
   - [Jurisdiction Pack Lifecycle](#16-jurisdiction-pack-lifecycle)
   - [Anti-Abuse & Integrity Flow](#17-anti-abuse--integrity-flow)
   - [Partner Network Flow](#18-partner-network-flow)
   - [Content Management Flow](#19-content-management-flow)
7. [Background Jobs](#background-jobs)
8. [Middleware Pipeline](#middleware-pipeline)
9. [Cross-Module Communication](#cross-module-communication)
10. [Domain Event Catalog](#domain-event-catalog)

---

## Architecture Overview

Lagedra is a **modular monolith** following **Clean Architecture** principles. Each module is self-contained with its own Domain, Application, Infrastructure, and Presentation layers, communicating through well-defined cross-module interfaces and a transactional outbox pattern for domain events.

```
┌──────────────────────────────────────────────────────────────┐
│                     Lagedra.ApiGateway                        │
│     (Minimal API endpoints, middleware, Swagger, auth)        │
├──────────────────────────────────────────────────────────────┤
│  Auth  │ TruthSurface │ Compliance │ Module 1..N             │
│  ┌──┐  │   ┌──┐       │   ┌──┐     │   ┌──┐                 │
│  │P │  │   │P │       │   │P │     │   │P │  Presentation   │
│  │A │  │   │A │       │   │A │     │   │A │  Application    │
│  │D │  │   │D │       │   │D │     │   │D │  Domain         │
│  │I │  │   │I │       │   │I │     │   │I │  Infrastructure │
│  └──┘  │   └──┘       │   └──┘     │   └──┘                 │
├──────────────────────────────────────────────────────────────┤
│              Lagedra.Infrastructure                           │
│  (EF Core, Stripe, Persona, MinIO, ClamAV, Quartz, Email)    │
├──────────────────────────────────────────────────────────────┤
│              Lagedra.SharedKernel                              │
│  (AggregateRoot, ValueObject, Events, Integration interfaces) │
├──────────────────────────────────────────────────────────────┤
│              Lagedra.Worker                                    │
│  (Quartz scheduler, 21 background jobs, outbox dispatch)      │
└──────────────────────────────────────────────────────────────┘
                          │
                    PostgreSQL 16
              (schema-per-module, single DB)
```

### Key Architectural Patterns

- **CQRS** — Commands (writes) and Queries (reads) separated via MediatR
- **Domain-Driven Design** — Aggregate roots, entities, value objects, domain events
- **Transactional Outbox** — Domain events are persisted to an outbox table within the same transaction, then dispatched asynchronously
- **Cross-Module Interfaces** — Modules communicate through interfaces defined in SharedKernel (e.g., `IUserStatusProvider`, `IConsentChecker`)
- **Cryptographic Sealing** — Truth Surfaces are hashed and digitally signed to create tamper-evident records

---

## Technology Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10, ASP.NET Core 10 |
| Database | PostgreSQL 16 (schema-per-module) |
| ORM | Entity Framework Core 9 + Npgsql 9 |
| CQRS/Bus | MediatR 12 (in-process) |
| Auth | ASP.NET Identity + JWT (self-hosted) with refresh tokens |
| Email | MailKit + MimeKit via Brevo SMTP relay |
| Payments | Stripe (`Stripe.net`) — protocol fee only |
| Maps/Geocoding | Google Maps Platform (Geocoding + Address Validation APIs) |
| KYC/Identity | Persona (liveness, document auth, synthetic ID detection) |
| Background Jobs | Quartz.NET 3.x with PostgreSQL persistent job store |
| Object Storage | MinIO (self-hosted, S3-compatible) |
| Antivirus | ClamAV (self-hosted, Docker) |
| Observability | Serilog + OpenTelemetry |
| Validation | FluentValidation (MediatR pipeline behavior) |
| Caching | In-memory (`IMemoryCache` via `ICacheService`) |
| Real-time | SignalR (notification hub) |
| API Docs | Swagger/OpenAPI + Postman collection |
| Frontend | React 19 + Vite + TypeScript + Tailwind + shadcn/ui |
| Testing | xUnit + FluentAssertions + NSubstitute + Bogus + TestContainers |
| Deployment | Docker Compose + Nginx (VPS) |
| CI/CD | GitHub Actions |

---

## Solution Structure

```
Lagedra.sln
├── src/
│   ├── Lagedra.SharedKernel/          # Base classes, interfaces, contracts
│   ├── Lagedra.Infrastructure/        # Cross-cutting: EF, email, Stripe, storage, middleware
│   ├── Lagedra.Auth/                  # Authentication & user management
│   ├── Lagedra.TruthSurface/         # Cryptographic deal sealing
│   ├── Lagedra.Compliance/           # Violations, trust ledger, compliance signals
│   ├── Lagedra.Modules/
│   │   ├── ActivationAndBilling/     # Deals, applications, payments, billing, damage claims
│   │   ├── ListingAndLocation/       # Listings, search, location, amenities, photos
│   │   ├── IdentityAndVerification/  # KYC, fraud flags, background checks, host payments
│   │   ├── InsuranceIntegration/     # Insurance verification, partner quotes
│   │   ├── StructuredInquiry/        # Pre-deal Q&A between tenant and landlord
│   │   ├── VerificationAndRisk/      # Risk scoring, deposit band calculation
│   │   ├── ComplianceMonitoring/     # Active deal compliance scanning
│   │   ├── Arbitration/              # Dispute resolution, evidence, decisions
│   │   ├── Evidence/                 # File upload, manifests, malware scanning
│   │   ├── JurisdictionPacks/        # Legal rule packs per jurisdiction
│   │   ├── Notifications/           # Email + in-app notifications, preferences
│   │   ├── Privacy/                  # GDPR consent, data export, deletion, legal holds
│   │   ├── AntiAbuseAndIntegrity/   # Fraud detection, collusion, restrictions
│   │   ├── ContentManagement/       # Blog, SEO pages
│   │   └── PartnerNetwork/          # Partner orgs, referrals, direct reservations
│   ├── Lagedra.ApiGateway/          # ASP.NET Core web host, middleware pipeline
│   └── Lagedra.Worker/              # Quartz background job host
├── tools/
│   ├── scripts/                      # Dev scripts (db-migrate, dev-up/down, test, lint)
│   ├── openapi/                      # OpenAPI spec + generation script
│   └── postman/                      # Postman collection + environment
└── docker-compose.yml
```

---

## Projects

### Lagedra.SharedKernel

Foundation layer with zero external dependencies. Provides:

- **Base domain classes**: `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject`, `IDomainEvent`, `ISoftDeletable`
- **Result pattern**: `Result`, `Result<T>`, `Error` for operation outcomes
- **Cross-module interfaces**: `IUserStatusProvider`, `IConsentChecker`, `IKycProvider`, `IUserEmailResolver`, `IHostPaymentDetailsProvider`, `IPartnerMembershipProvider`, `IVerificationSignalProvider`, `IUserInsuranceStatusProvider`, `IEvidenceManifestProvider`, `IUserVerificationFlagUpdater`, `IHostVerificationProvider`, `IUserViolationCountProvider`, `IDealApplicationStatusProvider`, `IHostProfileProvider`
- **Service contracts**: `ICacheService`, `IEmailService`, `IEventBus`, `IClock`, `IPlatformSettingsService`, `IEncryptionService`, `ICryptographicSigner`, `IHashingService`, `INotificationPusher`

### Lagedra.Infrastructure

Cross-cutting implementation layer:

- **Middleware**: AuthMiddleware, ConsentMiddleware, RateLimitingSetup, IdempotencyMiddleware, CorrelationIdMiddleware, GlobalExceptionHandlerMiddleware
- **External integrations**: Stripe, Persona, MinIO, ClamAV, Google Maps, MailKit/Brevo
- **Eventing**: InMemoryEventBus, OutboxProcessor, OutboxInterceptor, OutboxDispatcher
- **MediatR behaviors**: ValidationBehavior, UnhandledExceptionBehavior, LoggingBehavior
- **Caching**: InMemoryCacheService, CacheKeys
- **Security**: HashingService, EncryptionService, CryptographicSigner, DataProtection
- **Settings**: PlatformSettingsService (DB-backed admin-editable settings)
- **Real-time**: SignalRNotificationPusher, NotificationHub
- **Observability**: Health checks, audit interceptor, soft-delete interceptor

### Lagedra.Auth

Self-hosted authentication with ASP.NET Identity + JWT:

- User registration with email verification
- Login (email/password + external providers: Google, Apple, Microsoft)
- JWT access tokens + rotating refresh tokens
- Role-based authorization (Tenant, Landlord, Arbitrator, PlatformAdmin, InsurancePartner, InstitutionPartner)
- User profile management, password change/reset
- Admin user management

### Lagedra.TruthSurface

Cryptographic deal sealing — the core protocol primitive:

- Creates a canonical snapshot of deal terms (rent, deposit, rules, jurisdiction pack version)
- Both parties (landlord + tenant) must confirm
- On dual confirmation, the snapshot is hashed (SHA-256) and digitally signed
- Produces a tamper-evident, timestamped proof of agreed terms
- Supports superseding (re-confirmation when jurisdiction pack updates)

### Lagedra.Compliance

Violation tracking and trust scoring:

- Record, resolve, dismiss, and escalate compliance violations
- Public trust ledger for user reputation (append-only)
- Compliance signal processing (background job converts signals into violations or ledger entries)
- Categories: NonPayment, UnauthorizedOccupants, PropertyDamage, RuleViolation, InsuranceLapse, EarlyTermination

### Lagedra.Modules/ActivationAndBilling

The largest module — handles the entire deal lifecycle:

- **Applications**: Tenants submit, landlords approve/reject
- **Deal activation**: Creates billing account with Stripe subscription
- **Payment confirmation**: Host confirms tenant payment, tenant can dispute
- **Billing**: Invoicing, proration, reconciliation
- **Damage claims**: File, approve, reject, partially approve with deposit deduction
- **Cancellation**: With refund calculation

### Lagedra.Modules/ListingAndLocation

Property listing management:

- Full CRUD for listings with rich property details
- Photo management (upload, reorder, cover photo)
- Availability/date blocking
- Geolocation (approximate for search, precise address locked on activation)
- Advanced search with filters (location, price, amenities, dates, property type)
- Saved listings and collections
- Price history tracking
- Admin-managed definitions (amenities, safety devices, property considerations)

### Lagedra.Modules/IdentityAndVerification

KYC and identity verification:

- Persona-powered KYC (liveness check, document authentication)
- Background check consent and processing
- Institution affiliation verification
- Fraud flag management with SLA monitoring
- Host payment details (encrypted storage)

### Lagedra.Modules/InsuranceIntegration

Renter's insurance verification:

- API-based insurance status polling
- Manual proof upload for tenants with existing policies
- Partner insurance purchase webhook
- Unknown status SLA monitoring
- Insurance requirement enforcement

### Lagedra.Modules/StructuredInquiry

Pre-deal structured Q&A:

- Tenant requests detail unlock; landlord approves
- Predefined question categories (Utilities, Accessibility, Rules, Proximity)
- Structured response types (Yes/No, Multiple Choice, Numeric)
- Session closes when Truth Surface is confirmed
- Integrity scanning for abuse detection

### Lagedra.Modules/VerificationAndRisk

Risk scoring and deposit calculation:

- Aggregates signals from identity verification, background checks, insurance status, violation history
- Computes verification class (Low/Medium/High) with confidence indicator
- Calculates deposit band recommendations for landlords

### Lagedra.Modules/ComplianceMonitoring

Active deal compliance scanning:

- Periodic compliance scans across active deals
- Signal recording and processing
- Violation detection with cure deadlines
- Insurance lapse monitoring

### Lagedra.Modules/Arbitration

Dispute resolution:

- Two-tier system: Protocol Adjudication (platform-mediated) and Binding Arbitration (formal)
- Structured evidence collection with evidence slots
- Arbitrator assignment with concurrent case load tracking
- Binding awards and protocol decisions
- Appeal process
- Backlog SLA monitoring

### Lagedra.Modules/Evidence

Cryptographic evidence management:

- Evidence manifests (collections of files for a specific purpose)
- Presigned upload URLs via MinIO
- File hash verification (SHA-256)
- Automated malware scanning via ClamAV
- Manifest sealing (immutable after sealed)
- Metadata stripping
- Retention enforcement

### Lagedra.Modules/JurisdictionPacks

Legal rule packs per jurisdiction:

- Jurisdiction-specific rules (e.g., US-CA, US-CA-LA, GB-ENG)
- Dual-control approval workflow (two approvers required)
- Versioned packs with effective dates
- Deposit cap rules, field gating rules, evidence schedules
- Automatic activation on effective date

### Lagedra.Modules/Notifications

Multi-channel notification system:

- Email notifications via MailKit/Brevo
- In-app notifications via SignalR
- User notification preferences (opt-in/out per event type)
- Notification retry with backoff
- Unread count and mark-as-read

### Lagedra.Modules/Privacy

GDPR/privacy compliance:

- Consent management (KYCConsent, FCRAConsent, MarketingEmail, DataProcessing)
- Data export requests (async processing)
- Account deletion requests (with blocking reason support)
- Legal holds (prevent deletion during active disputes)
- Retention enforcement (background job)

### Lagedra.Modules/AntiAbuseAndIntegrity

Fraud and abuse detection:

- Collusion detection between users
- Inquiry abuse detection
- Trust ledger gaming detection
- Account restrictions (tiered levels)
- Pattern detection (background job)

### Lagedra.Modules/ContentManagement

Public content:

- Blog with draft/publish/archive lifecycle
- SEO page management
- Sitemap generation
- Admin CRUD

### Lagedra.Modules/PartnerNetwork

Institutional partnerships:

- Partner organization registration and verification
- Member management
- Referral link generation and redemption
- Direct reservation creation

### Lagedra.ApiGateway

The HTTP entry point:

- Hosts all module endpoints as Minimal APIs
- Configures middleware pipeline, authentication, CORS
- Swagger/OpenAPI documentation
- API versioning
- Health check endpoints
- SignalR notification hub

### Lagedra.Worker

Background processing host:

- Quartz.NET with PostgreSQL persistent job store
- 21 registered background jobs across all modules
- Outbox event dispatch (every 10 seconds)
- Health monitoring with alert emails for critical job failures
- Graceful shutdown with job drain

---

## API Endpoint Reference

### Auth (`/v1/auth`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/auth/register` | Anonymous | Register new user |
| POST | `/v1/auth/login` | Anonymous | Login with email/password |
| POST | `/v1/auth/external-login` | Anonymous | Login with Google/Apple/Microsoft |
| POST | `/v1/auth/refresh` | Anonymous | Refresh access token |
| POST | `/v1/auth/logout` | Required | Revoke refresh token |
| GET | `/v1/auth/verify-email` | Anonymous | Verify email via token |
| POST | `/v1/auth/resend-verification` | Anonymous | Resend verification email |
| POST | `/v1/auth/forgot-password` | Anonymous | Send password reset email |
| POST | `/v1/auth/reset-password` | Anonymous | Reset password with token |
| GET | `/v1/auth/me` | Required | Get current user profile |
| PUT | `/v1/auth/me` | Required | Update profile |
| POST | `/v1/auth/change-password` | Required | Change password |
| PUT | `/v1/auth/users/{userId}/role` | PlatformAdmin | Update user role |
| GET | `/v1/auth/users` | PlatformAdmin | List users (paginated) |

### Listings (`/v1/listings`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/listings` | Landlord | Create listing |
| PUT | `/v1/listings/{id}` | Landlord | Update listing |
| POST | `/v1/listings/{id}/publish` | Landlord | Publish listing |
| POST | `/v1/listings/{id}/close` | Landlord | Close listing |
| GET | `/v1/listings/{id}` | Anonymous | Get listing details |
| GET | `/v1/listings` | Anonymous | Search listings (with filters) |
| GET | `/v1/listings/{id}/similar` | Anonymous | Get similar listings |
| GET | `/v1/listings/{id}/share-url` | Anonymous | Get shareable URL |
| GET | `/v1/listings/{id}/price-history` | Anonymous | Get price history |
| GET | `/v1/listings/{id}/availability` | Anonymous | Get availability calendar |
| POST | `/v1/listings/{id}/block-dates` | Landlord | Block dates |
| DELETE | `/v1/listings/{id}/block-dates/{blockId}` | Landlord | Unblock dates |
| POST | `/v1/listings/{id}/photos` | Landlord | Add photo |
| DELETE | `/v1/listings/{id}/photos/{photoId}` | Landlord | Remove photo |
| PUT | `/v1/listings/{id}/photos/{photoId}/cover` | Landlord | Set cover photo |
| PUT | `/v1/listings/{id}/photos/reorder` | Landlord | Reorder photos |
| POST | `/v1/listings/{id}/approx-location` | Landlord | Set approximate location |
| POST | `/v1/listings/{id}/lock-address` | Landlord | Lock precise address |

### Saved Listings (`/v1/saved-listings`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/saved-listings/{listingId}` | Required | Save listing |
| DELETE | `/v1/saved-listings/{listingId}` | Required | Unsave listing |
| GET | `/v1/saved-listings` | Required | Get saved listings |
| POST | `/v1/saved-listings/{listingId}/collections/{collectionId}` | Required | Add to collection |
| DELETE | `/v1/saved-listings/{listingId}/collections` | Required | Remove from collection |
| POST | `/v1/saved-listings/collections` | Required | Create collection |
| GET | `/v1/saved-listings/collections` | Required | List collections |
| GET | `/v1/saved-listings/collections/{collectionId}` | Required | Get collection listings |

### Listing Definitions (`/v1/listing-definitions`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/listing-definitions/amenities` | Anonymous | List amenities |
| GET | `/v1/listing-definitions/safety-devices` | Anonymous | List safety devices |
| GET | `/v1/listing-definitions/considerations` | Anonymous | List considerations |
| POST | `/v1/admin/listing-definitions/amenities` | PlatformAdmin | Create amenity |
| PUT | `/v1/admin/listing-definitions/amenities/{id}` | PlatformAdmin | Update amenity |
| POST | `/v1/admin/listing-definitions/safety-devices` | PlatformAdmin | Create safety device |
| PUT | `/v1/admin/listing-definitions/safety-devices/{id}` | PlatformAdmin | Update safety device |
| POST | `/v1/admin/listing-definitions/considerations` | PlatformAdmin | Create consideration |
| PUT | `/v1/admin/listing-definitions/considerations/{id}` | PlatformAdmin | Update consideration |

### Applications (`/v1/applications`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/applications` | Required | Submit application for listing |
| POST | `/v1/applications/{id}/approve` | Required | Approve application (landlord) |
| POST | `/v1/applications/{id}/reject` | Required | Reject application |
| GET | `/v1/applications/{id}` | Required | Get application status |
| GET | `/v1/applications/listing/{listingId}` | Required | List applications for listing |

### Deals & Activation (`/v1/deals`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/deals/{dealId}/activate` | Required | Activate deal (start billing) |

### Billing (`/v1/deals`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/deals/{dealId}/billing` | Required | Get billing status |
| GET | `/v1/deals/{dealId}/proration-quote` | Required | Get proration quote |
| POST | `/v1/deals/{dealId}/stop-billing` | Required | Stop billing |

### Payment Confirmation (`/v1/deals/{dealId}/payment`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/deals/{dealId}/payment/details` | Required | Get payment details (host bank info) |
| GET | `/v1/deals/{dealId}/payment/status` | Required | Get payment confirmation status |
| POST | `/v1/deals/{dealId}/payment/confirm` | Required | Host confirms tenant payment |
| POST | `/v1/deals/{dealId}/payment/confirm-platform-payment` | Required | Host confirms platform fee payment |
| POST | `/v1/deals/{dealId}/payment/dispute` | Required | Tenant disputes payment (rate-limited: 3/month) |
| POST | `/v1/deals/{dealId}/payment/cancel` | Required | Cancel booking |
| POST | `/v1/deals/{dealId}/payment/damage-claim` | Required | File damage claim |
| POST | `/v1/admin/deals/{dealId}/resolve-payment-dispute` | Required | Admin resolves dispute |

### Damage Claims (`/v1/deals/{dealId}/damage-claims`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| PUT | `/v1/deals/{dealId}/damage-claims/{claimId}/approve` | PlatformAdmin | Approve claim |
| PUT | `/v1/deals/{dealId}/damage-claims/{claimId}/reject` | PlatformAdmin | Reject claim |
| PUT | `/v1/deals/{dealId}/damage-claims/{claimId}/partial-approve` | PlatformAdmin | Partially approve claim |

### Truth Surface (`/v1/truth-surface`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/truth-surface` | Required | Create deal snapshot |
| POST | `/v1/truth-surface/{snapshotId}/confirm` | Required | Confirm snapshot (landlord or tenant) |
| POST | `/v1/truth-surface/{snapshotId}/reconfirm` | Required | Supersede with new snapshot |
| GET | `/v1/truth-surface/{snapshotId}` | Required | Get snapshot |
| GET | `/v1/truth-surface/{snapshotId}/verify` | Required | Verify cryptographic proof |

### Identity & KYC (`/v1/identity`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/identity/kyc/start` | Required | Start KYC verification |
| POST | `/v1/identity/kyc/complete` | Required | Complete KYC |
| GET | `/v1/identity/status` | Required | Get verification status |
| POST | `/v1/verification/background-check/consent` | Required | Submit background check consent |
| POST | `/v1/verification/affiliation` | Required | Verify institution affiliation |
| POST | `/v1/verification/fraud-flag` | Required | Create fraud flag |
| GET | `/v1/verification/fraud-flags` | Required | Get fraud flags |
| PUT | `/v1/hosts/payment-details` | Required | Save host payment details |
| GET | `/v1/hosts/payment-details` | Required | Get host payment details |

### Insurance (`/v1/deals/{dealId}/insurance`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/deals/{dealId}/insurance` | Required | Get insurance status |
| POST | `/v1/deals/{dealId}/insurance/verify` | Required | Start insurance verification |
| POST | `/v1/deals/{dealId}/insurance/manual-proof` | Required | Upload manual proof |

### Structured Inquiry (`/v1/inquiries`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/inquiries/{dealId}/unlock-request` | Required | Request detail unlock (tenant) |
| POST | `/v1/inquiries/{dealId}/approve-unlock` | Required | Approve unlock (landlord) |
| POST | `/v1/inquiries/{dealId}/questions` | Required | Submit question |
| POST | `/v1/inquiries/{dealId}/answers` | Required | Submit answer (landlord) |
| POST | `/v1/inquiries/{dealId}/close` | Required | Close inquiry |
| GET | `/v1/inquiries/{dealId}` | Required | Get inquiry thread |
| GET | `/v1/inquiries/predefined-questions` | Required | List predefined questions |

### Risk (`/v1/risk`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/risk/{tenantUserId}` | Required | Get risk view for landlord |
| POST | `/v1/risk/{tenantUserId}/recalculate` | Required | Recalculate verification class |

### Compliance (`/v1/compliance`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/compliance/violations` | Landlord | Record violation |
| GET | `/v1/compliance/violations` | Required | Get violations for deal |
| PUT | `/v1/compliance/violations/{id}/resolve` | PlatformAdmin | Resolve violation |
| PUT | `/v1/compliance/violations/{id}/dismiss` | PlatformAdmin | Dismiss violation |
| PUT | `/v1/compliance/violations/{id}/escalate` | Landlord | Escalate violation |
| GET | `/v1/compliance/ledger/user/{userId}` | Required | Get user trust ledger |
| GET | `/v1/compliance/ledger/deal/{dealId}` | Required | Get deal ledger |

### Compliance Monitoring (`/v1/deals/{dealId}/compliance`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/deals/{dealId}/compliance` | Required | Get deal compliance status |
| GET | `/v1/deals/{dealId}/compliance/violations` | Required | List monitored violations |
| POST | `/v1/deals/{dealId}/compliance/signal` | Required | Record compliance signal |

### Arbitration (`/v1/arbitration`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/arbitration/cases` | Required | File arbitration case (rate-limited: 3/month) |
| POST | `/v1/arbitration/cases/{caseId}/evidence` | Required | Attach evidence |
| POST | `/v1/arbitration/cases/{caseId}/evidence-complete` | Required | Mark evidence collection complete |
| POST | `/v1/arbitration/cases/{caseId}/assign` | Required | Assign arbitrator |
| POST | `/v1/arbitration/cases/{caseId}/decision` | Required | Issue decision |
| PUT | `/v1/arbitration/cases/{caseId}/close` | Arbitrator | Close case |
| POST | `/v1/arbitration/cases/{caseId}/appeal` | Required | Appeal decision |
| GET | `/v1/arbitration/cases/{caseId}` | Required | Get case details |
| GET | `/v1/arbitration/cases` | Required | List cases by status |
| GET | `/v1/arbitrators/{userId}/cases` | Required | Get arbitrator's cases |

### Evidence (`/v1/evidence`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/evidence/manifests` | Required | Create evidence manifest |
| POST | `/v1/evidence/manifests/{id}/seal` | Required | Seal manifest |
| GET | `/v1/evidence/manifests/{id}` | Required | Get manifest |
| POST | `/v1/evidence/uploads/request-url` | Required | Request presigned upload URL |
| POST | `/v1/evidence/uploads/{id}/complete` | Required | Complete upload |
| GET | `/v1/evidence/uploads/{id}/scan` | Required | Get malware scan status |

### Jurisdiction Packs (`/v1/jurisdiction-packs`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/jurisdiction-packs` | Required | Create pack draft |
| PUT | `/v1/jurisdiction-packs/{id}/versions/{versionId}` | Required | Update draft version |
| POST | `/v1/jurisdiction-packs/{id}/versions/{versionId}/request-approval` | Required | Request dual-control approval |
| POST | `/v1/jurisdiction-packs/{id}/versions/{versionId}/approve` | Required | Approve version |
| POST | `/v1/jurisdiction-packs/{id}/versions/{versionId}/publish` | Required | Publish version |
| POST | `/v1/jurisdiction-packs/{id}/versions/{versionId}/deprecate` | Required | Deprecate version |
| GET | `/v1/jurisdiction-packs/{code}` | Required | Get active pack by jurisdiction code |
| GET | `/v1/jurisdiction-packs/{id}/versions` | Required | List versions |
| GET | `/v1/jurisdiction-packs/{id}/versions/{versionId}` | Required | Get version details |

### Notifications (`/v1/notifications`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/notifications/preferences/{userId}` | Required | Get notification preferences |
| PUT | `/v1/notifications/preferences/{userId}` | Required | Update preferences |
| GET | `/v1/notifications/history/{userId}` | Required | Get notification history |
| GET | `/v1/notifications/unread` | Required | Get unread notifications |
| GET | `/v1/notifications/unread/count` | Required | Get unread count |
| POST | `/v1/notifications/{notificationId}/read` | Required | Mark notification read |
| POST | `/v1/notifications/read-all` | Required | Mark all read |

### Privacy (`/v1/privacy`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/privacy/consent` | Required | Record consent |
| DELETE | `/v1/privacy/consent/{type}` | Required | Withdraw consent |
| GET | `/v1/privacy/consents/{userId}` | Required | Get user consents |
| POST | `/v1/privacy/export` | Required | Request data export |
| GET | `/v1/privacy/export/{id}` | Required | Get export status |
| POST | `/v1/privacy/deletion` | Required | Request account deletion |
| POST | `/v1/privacy/legal-holds` | Required | Apply legal hold |
| DELETE | `/v1/privacy/legal-holds/{id}` | Required | Release legal hold |
| GET | `/v1/privacy/legal-holds` | Required | List active legal holds |

### Anti-Abuse & Integrity (`/v1/integrity`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/v1/integrity/flags/{userId}` | Required | Get abuse flags |
| GET | `/v1/integrity/restrictions/{userId}` | Required | Get user restrictions |
| POST | `/v1/integrity/detect/collusion` | Required | Detect collusion |
| POST | `/v1/integrity/restrict` | Required | Apply account restriction |

### Content Management

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/v1/blog` | Anonymous | List published blog posts |
| GET | `/api/v1/blog/{slug}` | Anonymous | Get blog post by slug |
| GET | `/api/v1/blog/sitemap` | Anonymous | Get sitemap entries |
| GET | `/api/v1/pages/{slug}` | Anonymous | Get SEO page |
| POST | `/api/v1/admin/blog` | Required | Create blog post |
| PUT | `/api/v1/admin/blog/{id}` | Required | Update blog post |
| POST | `/api/v1/admin/blog/{id}/publish` | Required | Publish post |
| POST | `/api/v1/admin/blog/{id}/archive` | Required | Archive post |
| GET | `/api/v1/admin/blog` | Required | List all posts (admin) |
| PUT | `/api/v1/admin/pages/{slug}` | Required | Upsert SEO page |

### Partners (`/v1/partners`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/v1/partners` | Required | Register partner organization |
| GET | `/v1/partners/{id}` | Required | Get partner organization |
| POST | `/v1/partners/{id}/verify` | PlatformAdmin | Verify partner |
| POST | `/v1/partners/{id}/members` | Required | Add member |
| GET | `/v1/partners/{id}/members` | Required | List members |
| POST | `/v1/partners/{id}/referral-links` | Required | Generate referral link |
| GET | `/v1/partners/{id}/referral-links` | Required | List referral links |
| POST | `/v1/partners/{id}/reservations` | Required | Create direct reservation |
| POST | `/v1/referral/{code}/redeem` | Required | Redeem referral link |

### Webhooks (Anonymous, signature-verified)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/v1/webhooks/stripe` | Stripe payment events |
| POST | `/v1/webhooks/kyc` | Persona KYC events |
| POST | `/v1/webhooks/insurance/purchase` | Insurance purchase events |

### Health & Diagnostics

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health` | Basic health check |
| GET | `/health/detail` | Detailed health with dependency status |

### Real-Time

| Protocol | Route | Description |
|----------|-------|-------------|
| SignalR | `/hubs/notifications` | Real-time notification push |

---

## Business Flows

### 1. Tenant Registration & Verification Flow

```
Tenant registers (POST /v1/auth/register)
  ↓
Email sent with verification link
  ↓
Tenant verifies email (GET /v1/auth/verify-email?userId=&token=)
  ↓ UserRegisteredEvent
Account activated (IsActive = true)
  ↓
Tenant logs in (POST /v1/auth/login) → receives JWT + refresh token
  ↓
Tenant records consent (POST /v1/privacy/consent) → KYCConsent + DataProcessing
  ↓
Tenant starts KYC (POST /v1/identity/kyc/start) → Persona inquiry created
  ↓
Persona performs liveness check + document authentication
  ↓
Webhook received (POST /v1/webhooks/kyc) → updates verification status
  ↓ IdentityVerifiedEvent
Verification class computed (VerificationAndRisk module)
  ↓ VerificationClassComputedEvent
Deposit band calculated based on risk profile
  ↓
Tenant is now ready to browse and apply for listings
```

### 2. Landlord Onboarding & Listing Flow

```
Landlord registers + verifies email (same as tenant flow)
  ↓
Landlord saves payment details (PUT /v1/hosts/payment-details)
  → Encrypted and stored securely
  ↓
Landlord creates listing (POST /v1/listings)
  → Draft status, includes property details, rent, rules, amenities
  ↓
Landlord adds photos (POST /v1/listings/{id}/photos)
  ↓
Landlord sets approximate location (POST /v1/listings/{id}/approx-location)
  → Geocoded for search; precise address hidden until deal activation
  ↓
Landlord publishes listing (POST /v1/listings/{id}/publish)
  ↓ ListingPublishedEvent
Listing appears in search results
```

### 3. Booking Flow (Application → Deal Activation)

```
Tenant searches listings (GET /v1/listings?keyword=&latitude=&longitude=...)
  ↓
Tenant views listing details (GET /v1/listings/{id})
  ↓
Tenant views landlord risk profile (GET /v1/risk/{tenantUserId})
  → Shows verification class, deposit band recommendation
  ↓
Tenant submits application (POST /v1/applications)
  → ListingId, requested check-in/check-out dates
  ↓ ApplicationSubmittedEvent → notification to landlord
Landlord reviews application
  ↓
Landlord approves (POST /v1/applications/{id}/approve)
  → Sets deposit amount, DealId created
  ↓ ApplicationApprovedEvent → notification to tenant
  ↓
[Structured Inquiry phase — see flow #4]
  ↓
[Truth Surface creation and confirmation — see flow #5]
  ↓ TruthSurfaceConfirmedEvent
Payment confirmation created automatically
  → DealPaymentConfirmation with financial breakdown (rent, deposit, insurance, protocol fee)
  → Grace period starts (default 3 days)
  ↓
[Payment confirmation — see flow #6]
  ↓
Deal activated (POST /v1/deals/{dealId}/activate)
  ↓ DealActivatedEvent
  ↓
Precise address locked (POST /v1/listings/{id}/lock-address)
  ↓ PreciseAddressLockedEvent
  ↓
Billing account created with Stripe subscription
  → Monthly protocol fee invoicing begins
  ↓
Insurance verification started
  → Background polling begins
  ↓
Deal is now ACTIVE — compliance monitoring begins
```

### 4. Structured Inquiry Flow

```
After application approval, before Truth Surface:

Tenant requests detail unlock (POST /v1/inquiries/{dealId}/unlock-request)
  → Session created in "Locked" state
  ↓
Landlord approves unlock (POST /v1/inquiries/{dealId}/approve-unlock)
  → Session moves to "Open" state
  ↓
Tenant browses predefined questions (GET /v1/inquiries/predefined-questions?category=...)
  → Categories: UtilitySpecifics, AccessibilityLayout, RuleClarification, Proximity
  ↓
Tenant submits questions (POST /v1/inquiries/{dealId}/questions)
  → Can use predefined or custom questions
  ↓
Landlord answers (POST /v1/inquiries/{dealId}/answers)
  → Response types: YesNo, MultipleChoice, Numeric
  ↓
[Repeat Q&A as needed]
  ↓
When both parties are satisfied, Truth Surface is created
  → Inquiry session closes automatically on Truth Surface confirmation
  ↓ InquiryClosedEvent
```

### 5. Truth Surface Flow

```
After inquiry (or directly after approval):

Snapshot created (POST /v1/truth-surface)
  → Contains: DealId, protocol version, jurisdiction pack version, canonical content
  → Canonical content = deal terms, rent, deposit, rules, inquiry Q&A
  → Status: Draft → PendingBothConfirmations
  ↓ TruthSurfaceInitiatedEvent → notifications to both parties

Landlord confirms (POST /v1/truth-surface/{snapshotId}/confirm)
  → Status: PendingTenantConfirmation
  ↓
Tenant confirms (POST /v1/truth-surface/{snapshotId}/confirm)
  → Both parties confirmed
  ↓
Snapshot SEALED:
  → SHA-256 hash of canonical content computed
  → Hash digitally signed with platform key
  → CryptographicProof record created
  → Status: Confirmed
  ↓ TruthSurfaceConfirmedEvent
  ↓
Automatically triggers:
  → DealPaymentConfirmation creation (financial breakdown computed)
  → Notifications to both parties

If jurisdiction pack updates after sealing:
  → Reconfirm (POST /v1/truth-surface/{snapshotId}/reconfirm)
  → Original superseded, new snapshot created
  → Both parties must re-confirm
  ↓ TruthSurfaceSupersededEvent
```

### 6. Payment Confirmation Flow

```
After Truth Surface confirmed, DealPaymentConfirmation created automatically:
  → Financial breakdown: FirstMonthRent + Deposit + InsuranceFee + ProtocolFee
  → TotalTenantPayment = FirstMonthRent + Deposit + InsuranceFee
  → TotalHostPlatformPayment = MonthlyProtocolFee
  → Grace period: 3 days (configurable)

Tenant views payment details (GET /v1/deals/{dealId}/payment/details)
  → Shows host bank info (decrypted)
  ↓
Tenant makes payment offline (bank transfer, etc.)
  ↓
Host confirms receipt (POST /v1/deals/{dealId}/payment/confirm)
  ↓ PaymentConfirmedEvent
  ↓
Host pays platform fee (POST /v1/deals/{dealId}/payment/confirm-platform-payment)
  → Recorded separately
  ↓
— OR if tenant disagrees —
  ↓
Tenant disputes (POST /v1/deals/{dealId}/payment/dispute)
  → Rate-limited: 3 disputes per 30 days
  → Includes reason and optional evidence manifest
  ↓ PaymentDisputedEvent
  ↓
Admin resolves (POST /v1/admin/deals/{dealId}/resolve-payment-dispute)
  → PaymentValid = true/false
  ↓ PaymentDisputeResolvedEvent

If grace period expires without confirmation:
  → PaymentConfirmationTimeoutJob sends reminders
  → Eventually auto-cancels if not resolved
```

### 7. Billing & Subscription Flow

```
After deal activation:

Stripe customer + subscription created
  → Monthly protocol fee billing begins
  ↓
BillingReconciliationJob (daily at 4 AM):
  → Reconciles invoices with Stripe
  ↓
Stripe webhook (POST /v1/webhooks/stripe):
  → PaymentIntentSucceeded → invoice marked paid
  → PaymentIntentPaymentFailed → invoice marked failed, notification sent
  → CustomerSubscriptionDeleted → billing stopped
  ↓
Proration quotes available (GET /v1/deals/{dealId}/proration-quote)
  → For mid-month changes
  ↓
Stop billing (POST /v1/deals/{dealId}/stop-billing)
  → When deal ends normally
  ↓ BillingStoppedEvent
```

### 8. Insurance Flow

```
After deal activation or during application:

Insurance verification started (POST /v1/deals/{dealId}/insurance/verify)
  ↓
System polls insurance API (InsurancePollerJob, hourly):
  → Status: Active, NotActive, InstitutionBacked, Unknown
  ↓
If Unknown for too long:
  → InsuranceUnknownSlaJob escalates (every 30 min)
  ↓ InsuranceUnknownSlaBreachedEvent
  ↓
Tenant can upload manual proof (POST /v1/deals/{dealId}/insurance/manual-proof)
  → Staff reviews manually
  ↓
Insurance purchase via partner (POST /v1/webhooks/insurance/purchase):
  → Status updated to Active
  ↓ InsuranceStatusChangedEvent
  ↓
If insurance lapses during active deal:
  → ComplianceSignal recorded
  → Violation created (InsuranceLapse category)
  → Notification sent to tenant
```

### 9. Compliance & Trust Ledger Flow

```
During active deal:

ComplianceScannerJob runs every 6 hours:
  → Scans all active deals for compliance issues
  → Records MonitoredComplianceSignals
  ↓
ComplianceSignalProcessorJob runs every 15 minutes:
  → Processes unprocessed signals
  → Converts to violations or trust ledger entries
  ↓
Landlord can record violation manually (POST /v1/compliance/violations):
  → Categories: NonPayment, UnauthorizedOccupants, PropertyDamage, etc.
  ↓ ViolationCreatedEvent → notification to tenant
  ↓
Violation lifecycle:
  → Open → UnderReview → Resolved / Dismissed / Escalated
  → Resolved: ViolationResolvedEvent → notification
  → Escalated: ViolationEscalatedEvent → notification
  ↓
Trust ledger entries appended (append-only):
  → DealCompleted, ViolationRecorded, ViolationDismissed, ArbitrationRuling
  → InsuranceClaim, PaymentDefault, PositiveReview, IdentityVerified
  ↓
Public trust ledger viewable (GET /v1/compliance/ledger/user/{userId}):
  → Shows only IsPublic entries
  → Used by landlords to evaluate potential tenants
```

### 10. Arbitration Flow

```
Dispute arises during or after a deal:

Party files case (POST /v1/arbitration/cases)
  → Tier: ProtocolAdjudication or BindingArbitration
  → Category: CategoryA–G, Other
  → Rate-limited: 3 cases per 30 days
  ↓ CaseFiledEvent → notifications to all parties
  ↓
Evidence collection phase:
  → Both parties attach evidence (POST /v1/arbitration/cases/{id}/evidence)
  → Evidence linked to sealed EvidenceManifests (see flow #11)
  → Each evidence slot has a type and submitter
  ↓
Party marks evidence complete (POST /v1/arbitration/cases/{id}/evidence-complete)
  ↓ EvidenceCompleteEvent
  → Decision deadline set (based on tier)
  ↓
Arbitrator assigned (POST /v1/arbitration/cases/{id}/assign)
  → Concurrent case load tracked
  ↓
Arbitrator reviews evidence and issues decision:
  → Protocol decision: recommendation, no monetary award
  → Binding award: monetary amount, enforceable
  (POST /v1/arbitration/cases/{id}/decision)
  ↓ DecisionIssuedEvent → notifications
  ↓
Case closed (PUT /v1/arbitration/cases/{id}/close)
  ↓ CaseClosedEvent
  → Trust ledger entry: ArbitrationRuling
  ↓
— OR appeal —
  ↓
Party appeals (POST /v1/arbitration/cases/{id}/appeal)
  ↓ CaseAppealedEvent
  → Case re-opened for higher-tier review

ArbitrationBacklogSlaJob (hourly):
  → Monitors overdue cases
  → Escalates if decision deadline passed
  ↓ ArbitrationBacklogEscalationEvent
```

### 11. Evidence Management Flow

```
Evidence needed for arbitration, damage claims, or compliance:

Create manifest (POST /v1/evidence/manifests)
  → Manifest types: MoveIn, MoveOut, Arbitration, Insurance, Damage
  → Status: Open
  ↓ EvidenceManifestCreatedEvent
  ↓
Request upload URL (POST /v1/evidence/uploads/request-url)
  → Returns presigned MinIO URL
  ↓
Upload file to presigned URL (direct to MinIO)
  ↓
Complete upload (POST /v1/evidence/uploads/{id}/complete)
  → Records file hash (SHA-256), MIME type, original filename
  ↓ EvidenceUploadedEvent
  ↓
MalwareScanPollingJob (every 5 minutes):
  → Sends file to ClamAV for scanning
  → Records result: Clean / Infected
  ↓ EvidenceScannedEvent
  ↓
Check scan status (GET /v1/evidence/uploads/{id}/scan)
  ↓
Seal manifest (POST /v1/evidence/manifests/{id}/seal)
  → Computes hash of all files
  → Manifest becomes immutable
  → Status: Sealed
  ↓ EvidenceManifestSealedEvent
  ↓
Sealed manifest can be referenced by arbitration cases, damage claims, etc.

EvidenceRetentionJob (nightly at 2 AM):
  → Enforces retention policies
  → Removes expired evidence per jurisdiction rules
```

### 12. Damage Claim Flow

```
During or after a deal:

Landlord files damage claim (POST /v1/deals/{dealId}/payment/damage-claim)
  → Description, claimed amount, optional evidence manifest
  ↓ DamageClaimFiledEvent → notifications

Deposit deduction calculated automatically:
  → Min(claimedAmount, depositAmount) deducted from deposit
  → Remainder flagged for insurance claim if applicable
  ↓
Platform admin reviews:
  → Approve (PUT /v1/deals/{dealId}/damage-claims/{claimId}/approve)
  → Reject (PUT /v1/deals/{dealId}/damage-claims/{claimId}/reject)
  → Partial approve (PUT /v1/deals/{dealId}/damage-claims/{claimId}/partial-approve)
  ↓ DamageClaimApprovedEvent / DamageClaimRejectedEvent
  ↓
Deposit released minus approved deduction
Insurance claim filed if approved amount exceeds deposit
```

### 13. Booking Cancellation Flow

```
Before or during active deal:

Party initiates cancellation (POST /v1/deals/{dealId}/payment/cancel)
  → Reason required
  ↓
System calculates refund based on:
  → Listing's cancellation policy (Flexible/Moderate/Strict/NonRefundable/Custom)
  → Free cancellation days
  → Partial refund percentage
  → Insurance refund calculation
  ↓ BookingCancelledEvent
  ↓
Refund processed:
  → Deposit refund (full or partial)
  → Insurance fee refund (if applicable)
  → First month rent refund (based on policy)
  ↓
Billing stopped if deal was active
  ↓ BillingStoppedEvent
  ↓
Listing availability dates unblocked
```

### 14. Privacy & Consent Flow

```
User records required consents (POST /v1/privacy/consent):
  → KYCConsent (required for KYC)
  → DataProcessing (required for most write operations)
  → FCRAConsent (required for background checks)
  → MarketingEmail (optional)
  ↓ ConsentRecordedEvent
  ↓
ConsentMiddleware enforces:
  → All authenticated write requests (POST/PUT/DELETE) require KYCConsent + DataProcessing
  → Exempt: auth, health, webhook, public endpoints
  → Returns 451 (Unavailable for Legal Reasons) if missing
  ↓
User can withdraw consent (DELETE /v1/privacy/consent/{type}):
  → WithdrawnAt timestamp recorded
  → Future requests blocked if required consent withdrawn
  ↓
User requests data export (POST /v1/privacy/export):
  → Async processing: Queued → Processing → Completed/Failed
  → Package generated and stored in MinIO
  → DataExportPurgeJob cleans up expired exports
  ↓
User requests account deletion (POST /v1/privacy/deletion):
  → Checks for active legal holds
  → If legal hold exists: Status = Blocked
  → Otherwise: Status = Completed, data anonymized
  ↓ DataAnonymizedEvent
  ↓
RetentionEnforcementJob (nightly at 1 AM):
  → Enforces data retention policies across all modules
```

### 15. Notification Flow

```
Domain event occurs (e.g., ApplicationApprovedEvent):
  ↓
Event handler sends NotifyUserCommand via MediatR:
  → Recipient, template ID, title, body, metadata, channels
  ↓
NotificationProcessingJob (every 30 seconds):
  → Picks up queued notifications
  → Checks user preferences (opt-in/out per event type)
  → For Email channel: sends via MailKit/Brevo SMTP
  → For InApp channel: creates InAppNotification record + pushes via SignalR
  ↓ NotificationDeliveredEvent
  ↓
If delivery fails:
  → Attempt count incremented, error recorded
  ↓ NotificationFailedEvent
  → NotificationRetryJob (every 10 minutes) retries failed notifications
  ↓
User reads notifications:
  → Real-time via SignalR (/hubs/notifications)
  → Polling via REST (GET /v1/notifications/unread)
  → Mark read (POST /v1/notifications/{id}/read)
  → Mark all read (POST /v1/notifications/read-all)
```

### 16. Jurisdiction Pack Lifecycle

```
Platform admin creates pack draft (POST /v1/jurisdiction-packs):
  → Jurisdiction code: CC-SS or CC-SS-CCC (e.g., US-CA, US-CA-LA, GB-ENG)
  ↓
Admin configures pack version:
  → Deposit cap rules (max multiplier, exceptions, legal references)
  → Field gating rules (hard/soft gates on specific fields)
  → Evidence schedules (minimum requirements per category)
  → Effective date rules
  ↓
Admin requests dual-control approval:
  (POST /v1/jurisdiction-packs/{id}/versions/{versionId}/request-approval)
  → Status: Draft → PendingApproval
  ↓
Second admin approves:
  (POST /v1/jurisdiction-packs/{id}/versions/{versionId}/approve)
  → Two-approver requirement enforced
  ↓
Admin publishes:
  (POST /v1/jurisdiction-packs/{id}/versions/{versionId}/publish)
  → Status: Active
  ↓ JurisdictionPackPublishedEvent
  ↓
PackEffectiveDateActivationJob (daily at midnight):
  → Activates pack versions when their effective date arrives
  ↓ PackEffectiveDateChangedEvent
  → Existing Truth Surfaces using old version may need reconfirmation
  ↓
Deprecation:
  (POST /v1/jurisdiction-packs/{id}/versions/{versionId}/deprecate)
  → Old version marked deprecated when new one published
```

### 17. Anti-Abuse & Integrity Flow

```
PatternDetectionSchedulerJob (every 4 hours):
  → Scans for collusion patterns
  → Detects inquiry abuse
  → Identifies trust ledger gaming
  ↓
If pattern detected:
  ↓ CollusionDetectedEvent / InquiryAbuseDetectedEvent / TrustLedgerGamingDetectedEvent
  ↓
Abuse case created:
  → AbuseType: Collusion, InquiryAbuse, TrustLedgerGaming, etc.
  ↓
Manual detection (POST /v1/integrity/detect/collusion):
  → Admin-triggered analysis
  ↓
Account restriction applied (POST /v1/integrity/restrict):
  → Tiered restriction levels
  ↓ AccountRestrictionAppliedEvent
  ↓
Restrictions viewable (GET /v1/integrity/restrictions/{userId})
Flags viewable (GET /v1/integrity/flags/{userId})
```

### 18. Partner Network Flow

```
Institution registers as partner (POST /v1/partners):
  → Name, organization type, contact email, tax ID
  → Status: PendingVerification
  ↓
Platform admin verifies (POST /v1/partners/{id}/verify):
  ↓ PartnerOrganizationVerifiedEvent
  → Status: Verified
  ↓
Partner adds members (POST /v1/partners/{id}/members):
  → Members get partner-referred status on applications
  ↓
Partner generates referral links (POST /v1/partners/{id}/referral-links):
  → Unique code, expiration, max uses
  ↓
Tenant redeems referral (POST /v1/referral/{code}/redeem):
  ↓ ReferralRedeemedEvent
  → Linked to partner organization
  ↓
Partner creates direct reservation (POST /v1/partners/{id}/reservations):
  → Bypasses standard application flow for institutional tenants
  ↓ DirectReservationCreatedEvent
```

### 19. Content Management Flow

```
Admin creates blog post (POST /api/v1/admin/blog):
  → Status: Draft
  → Includes: title, slug, excerpt, content, tags, SEO metadata
  ↓
Admin publishes (POST /api/v1/admin/blog/{id}/publish):
  ↓ BlogPostPublishedEvent
  → Visible at GET /api/v1/blog/{slug}
  ↓
Public reads blog (GET /api/v1/blog):
  → Paginated, filterable by tag
  → No auth required
  ↓
Sitemap available (GET /api/v1/blog/sitemap)
  ↓
Admin can archive (POST /api/v1/admin/blog/{id}/archive):
  ↓ BlogPostArchivedEvent
  → Removed from public listing

SEO pages managed similarly:
  → Upsert via PUT /api/v1/admin/pages/{slug}
  → Public read via GET /api/v1/pages/{slug}
```

---

## Background Jobs

All jobs run in `Lagedra.Worker` via Quartz.NET with a PostgreSQL persistent job store.

| Job | Module | Schedule | Description |
|-----|--------|----------|-------------|
| OutboxDispatchOrchestrator | Infrastructure | Every 10 sec | Dispatches domain events from outbox tables across all modules |
| RefreshTokenCleanupJob | Auth | Daily 3:00 AM | Removes expired/revoked refresh tokens |
| BillingReconciliationJob | ActivationAndBilling | Daily 4:00 AM | Reconciles invoices with Stripe |
| PaymentConfirmationTimeoutJob | ActivationAndBilling | Hourly | Sends reminders and auto-cancels expired payment confirmations |
| HostPlatformPaymentEnforcementJob | ActivationAndBilling | Daily 8:00 AM | Enforces host platform fee payment deadlines |
| InsurancePollerJob | InsuranceIntegration | Hourly | Polls insurance API for status updates |
| InsuranceUnknownSlaJob | InsuranceIntegration | Every 30 min | Escalates unknown insurance statuses past SLA |
| FraudFlagSlaMonitorJob | IdentityAndVerification | Every 15 min | Monitors fraud flag resolution SLAs |
| ComplianceScannerJob | ComplianceMonitoring | Every 6 hours | Scans active deals for compliance issues |
| ComplianceSignalProcessorJob | Compliance | Every 15 min | Processes compliance signals into violations/ledger entries |
| ArbitrationBacklogSlaJob | Arbitration | Hourly | Monitors and escalates overdue arbitration cases |
| PackEffectiveDateActivationJob | JurisdictionPacks | Daily midnight | Activates jurisdiction pack versions on their effective date |
| MalwareScanPollingJob | Evidence | Every 5 min | Sends uploaded files to ClamAV for malware scanning |
| EvidenceRetentionJob | Evidence | Daily 2:00 AM | Enforces evidence retention policies |
| NotificationRetryJob | Notifications | Every 10 min | Retries failed notification deliveries |
| NotificationProcessingJob | Notifications | Every 30 sec | Processes queued notifications |
| RetentionEnforcementJob | Privacy | Daily 1:00 AM | Enforces data retention policies |
| DataExportPurgeJob | Privacy | Daily 5:00 AM | Purges completed data export packages |
| PatternDetectionSchedulerJob | AntiAbuseAndIntegrity | Every 4 hours | Detects fraud and abuse patterns |
| JurisdictionResolutionJob | ListingAndLocation | Daily 2:00 AM | Resolves jurisdiction codes for listings |
| InquiryIntegrityScanJob | StructuredInquiry | Daily 6:00 AM | Scans inquiry sessions for abuse patterns |
| SnapshotVerificationJob | TruthSurface | Weekly (Sun 3 AM) | Verifies integrity of sealed snapshots |

### Worker Health Monitoring

The `HealthOrchestrator` monitors job execution. If a **critical job** (`MalwareScanPollingJob` or `PaymentConfirmationTimeoutJob`) fails **3 or more consecutive times**, an alert email is sent to the ops team.

---

## Middleware Pipeline

The API Gateway middleware executes in this order for every request:

```
1. CorrelationIdMiddleware
   → Reads or generates X-Correlation-Id header
   → Adds to HttpContext.Items and response headers
   → Pushes to Serilog LogContext

2. GlobalExceptionHandlerMiddleware
   → Catches FluentValidation.ValidationException → 400 with grouped errors
   → Catches all other exceptions → 500 with RFC 7807 Problem Details

3. Serilog Request Logging
   → Structured request/response logging

4. Swagger (Development only)
   → /swagger/v1/swagger.json + Swagger UI

5. CORS
   → "Frontend" policy: FrontendUrl, AdminUrl, MarketingUrl origins

6. Authentication
   → JWT Bearer token validation via ASP.NET Core Authentication

7. Authorization
   → Role-based policies: RequireLandlord, RequireTenant, RequireArbitrator,
     RequirePlatformAdmin, RequireInsurancePartner, RequireInstitutionPartner

8. AuthMiddleware (AuthEnforcement)
   → Extracts UserId + Role from JWT claims into HttpContext.Items
   → Checks IsActive via IUserStatusProvider (5-min cache)
   → Returns 403 if user account is deactivated

9. ConsentMiddleware (ConsentEnforcement)
   → For authenticated POST/PUT/DELETE requests:
     checks KYCConsent + DataProcessing consents via IConsentChecker (10-min cache)
   → Exempt paths: /health, /swagger, /hubs, /v1/auth, /v1/webhook,
     /v1/blog, /v1/seo, /v1/listings/search, /v1/listings/definitions
   → Returns 451 (Unavailable for Legal Reasons) if consents missing

10. Rate Limiting
    → DisputeCap policy: 3 disputes per 30-day window per user
    → Applied to arbitration case filing + payment dispute endpoints

11. IdempotencyMiddleware
    → For POST/PUT/PATCH with Idempotency-Key header
    → Caches response for 24 hours
    → Returns cached response for duplicate keys
```

---

## Cross-Module Communication

Modules communicate through interfaces defined in `Lagedra.SharedKernel`. No module directly references another module's internals.

| Interface | Defined In | Implemented In | Purpose |
|-----------|-----------|---------------|---------|
| `IUserStatusProvider` | SharedKernel | Auth | Check if user is active |
| `IConsentChecker` | SharedKernel | Privacy | Check required consents |
| `IUserEmailResolver` | SharedKernel | Auth | Resolve user ID to email |
| `IHostProfileProvider` | SharedKernel | Auth | Get host profile data |
| `IHostPaymentDetailsProvider` | SharedKernel | IdentityAndVerification | Get decrypted host payment info |
| `IKycProvider` | SharedKernel | Infrastructure (Persona) | KYC inquiry management |
| `IUserVerificationFlagUpdater` | SharedKernel | Auth | Update verification flags |
| `IHostVerificationProvider` | SharedKernel | IdentityAndVerification | Get host verification status |
| `IVerificationSignalProvider` | SharedKernel | IdentityAndVerification | Get verification signals |
| `IUserInsuranceStatusProvider` | SharedKernel | InsuranceIntegration | Get insurance status |
| `IUserViolationCountProvider` | SharedKernel | Compliance | Get active violation count |
| `IDealApplicationStatusProvider` | SharedKernel | ActivationAndBilling | Check deal approval status |
| `IEvidenceManifestProvider` | SharedKernel | Evidence | Access evidence manifests |
| `IPartnerMembershipProvider` | SharedKernel | PartnerNetwork | Get partner organization ID |

### Domain Event Flow

Domain events are published via the **transactional outbox pattern**:

1. Command handler raises domain event on aggregate root
2. `OutboxInterceptor` (EF Core `SaveChangesInterceptor`) persists events to `outbox_messages` table within the same transaction
3. `OutboxDispatchOrchestrator` (runs every 10 seconds) polls outbox tables across all module `DbContext`s
4. Events dispatched via `InMemoryEventBus` to registered `IDomainEventHandler<T>` implementations
5. Processed messages marked as dispatched

---

## Domain Event Catalog

### Auth Events
| Event | Trigger | Handlers |
|-------|---------|----------|
| `UserRegisteredEvent` | Email verified | `OnUserRegisteredNotify` → welcome email + in-app |
| `UserRoleChangedEvent` | Admin updates role | `OnUserRoleChangedNotify` → notification |

### TruthSurface Events
| Event | Trigger | Handlers |
|-------|---------|----------|
| `TruthSurfaceInitiatedEvent` | Snapshot submitted | `OnTruthSurfaceInitiatedNotify` |
| `TruthSurfaceConfirmedEvent` | Both parties confirm | `OnTruthSurfaceConfirmedNotify`, `CreatePaymentConfirmationHandler` |
| `TruthSurfaceSupersededEvent` | Reconfirmation | `OnTruthSurfaceSupersededNotify` |

### Compliance Events
| Event | Trigger | Handlers |
|-------|---------|----------|
| `ViolationCreatedEvent` | Violation recorded | `OnViolationCreatedNotify` |
| `ViolationResolvedEvent` | Violation resolved | `OnViolationResolvedNotify` |
| `ViolationEscalatedEvent` | Violation escalated | `OnViolationEscalatedNotify` |

### ActivationAndBilling Events
| Event | Trigger |
|-------|---------|
| `ApplicationSubmittedEvent` | Application submitted |
| `ApplicationApprovedEvent` | Application approved by landlord |
| `ApplicationRejectedEvent` | Application rejected |
| `DealActivatedEvent` | Deal activated |
| `BookingCancelledEvent` | Booking cancelled |
| `PaymentConfirmedEvent` | Host confirms payment |
| `PaymentDisputedEvent` | Tenant disputes payment |
| `PaymentDisputeResolvedEvent` | Admin resolves dispute |
| `DamageClaimFiledEvent` | Damage claim filed |
| `DamageClaimApprovedEvent` | Claim approved |
| `DamageClaimRejectedEvent` | Claim rejected |
| `BillingStoppedEvent` | Billing stopped |
| `PaymentFailedEvent` | Stripe payment failed |

### ListingAndLocation Events
| Event | Trigger |
|-------|---------|
| `ListingPublishedEvent` | Listing published |
| `PreciseAddressLockedEvent` | Address locked on activation |

### IdentityAndVerification Events
| Event | Trigger |
|-------|---------|
| `IdentityVerifiedEvent` | KYC verification complete |
| `IdentityVerificationFailedEvent` | KYC failed |
| `FraudFlagRaisedEvent` | Fraud flag created |
| `BackgroundCheckReceivedEvent` | Background check result |
| `AffiliationVerifiedEvent` | Institution affiliation verified |

### InsuranceIntegration Events
| Event | Trigger |
|-------|---------|
| `InsuranceStatusChangedEvent` | Insurance status updated |
| `InsuranceUnknownSlaBreachedEvent` | Unknown status past SLA |

### Arbitration Events
| Event | Trigger |
|-------|---------|
| `CaseFiledEvent` | Case filed |
| `EvidenceCompleteEvent` | Evidence collection done |
| `DecisionIssuedEvent` | Decision rendered |
| `CaseClosedEvent` | Case closed |
| `CaseAppealedEvent` | Case appealed |

### Evidence Events
| Event | Trigger |
|-------|---------|
| `EvidenceManifestCreatedEvent` | Manifest created |
| `EvidenceUploadedEvent` | File uploaded |
| `EvidenceManifestSealedEvent` | Manifest sealed |
| `EvidenceScannedEvent` | Malware scan complete |

### Other Module Events
| Event | Module | Trigger |
|-------|--------|---------|
| `JurisdictionPackPublishedEvent` | JurisdictionPacks | Pack version published |
| `ConsentRecordedEvent` | Privacy | Consent recorded |
| `DataAnonymizedEvent` | Privacy | Account data anonymized |
| `LegalHoldAppliedEvent` | Privacy | Legal hold applied |
| `NotificationQueuedEvent` | Notifications | Notification queued |
| `NotificationDeliveredEvent` | Notifications | Notification delivered |
| `NotificationFailedEvent` | Notifications | Notification delivery failed |
| `BlogPostPublishedEvent` | ContentManagement | Blog post published |
| `PartnerOrganizationVerifiedEvent` | PartnerNetwork | Partner verified |
| `ReferralRedeemedEvent` | PartnerNetwork | Referral code redeemed |
| `AccountRestrictionAppliedEvent` | AntiAbuseAndIntegrity | Restriction applied |
| `CollusionDetectedEvent` | AntiAbuseAndIntegrity | Collusion pattern found |
| `InquiryClosedEvent` | StructuredInquiry | Inquiry session closed |
| `VerificationClassComputedEvent` | VerificationAndRisk | Risk class computed |
| `DepositBandUpdatedEvent` | VerificationAndRisk | Deposit band recalculated |

---

*Last updated: 2026-03-05. This document is auto-generated from the codebase and should be regenerated when significant changes are made.*
