# Lagedra — Mid-Term Rental Trust Protocol

A neutral, risk-aware enforcement layer for mid-term rentals (30–180 days).  
Not a property manager. Not a payments processor. Not a reviews platform.

---

## What It Does

Lagedra provides:

- **Verified deal execution** — cryptographically signed, immutable deal snapshots (Truth Surface)
- **Objective risk scoring** — deterministic Verification Class (Low / Medium / High) from verified signals only
- **Data-driven deposit guidance** — jurisdiction-capped recommendations based on risk class
- **Structured arbitration** — evidence-bound, two-tier resolution (Protocol Adjudication → Binding Arbitration)
- **Objective Trust Ledger** — append-only, mathematically derived reputation from factual outcomes
- **Jurisdiction Packs** — counsel-vetted addenda for state/local compliance (California v1)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core |
| Architecture | Modular Monolith (schema-per-module) |
| Database | PostgreSQL 16 (via Npgsql + EF Core 9) |
| CQRS / Bus | MediatR 12 + domain events + Outbox pattern |
| Auth | ASP.NET Identity + JWT + Refresh Tokens |
| Email | MailKit → Brevo (SMTP relay) |
| Payments | Stripe (protocol fee only) |
| Maps / Geocoding | Google Maps Platform |
| KYC / Background | Persona |
| Object Storage | MinIO (self-hosted, S3-compatible) |
| Antivirus | ClamAV (self-hosted) |
| Jobs | Quartz.NET (PostgreSQL job store) |
| Observability | Serilog + OpenTelemetry |
| Frontend (app) | React 19 + Vite + TypeScript + Tailwind + shadcn/ui |
| Frontend (marketing) | Next.js 15 (App Router) + TypeScript + Tailwind |
| State | Zustand |
| Data Fetching | TanStack Query |
| Deployment | Docker Compose + Nginx on VPS |
| CI/CD | GitHub Actions |

---

## Local Quickstart

### Prerequisites

- Docker Desktop
- .NET 10 SDK
- (Optional) Node 20 + pnpm — only needed to run frontend outside Docker

### Start Everything

```powershell
# Windows
.\tools\scripts\dev-up.ps1
```

```bash
# Linux / macOS
./tools/scripts/dev-up.sh
```

This will:
1. Start all Docker services (PostgreSQL, MinIO, ClamAV, API, Worker, Web, Admin, Marketing)
2. Run all EF Core migrations
3. Seed reference data

### Access Points (local dev)

| Service | URL |
|---|---|
| API (Swagger) | http://localhost:8080/swagger |
| Web app | http://localhost:3000 |
| Admin app | http://localhost:3001 |
| Marketing site | http://localhost:3002 |
| MinIO Console | http://localhost:9001 |
| PostgreSQL | localhost:5432 |

### Environment Variables

Copy `.env.example` to `.env` and fill in your credentials:

```powershell
Copy-Item .env.example .env
```

**Never commit the `.env` file.** See `deploy/env/` for staging/prod templates.

---

## Repository Structure

```
Lagedra/
├── src/
│   ├── Lagedra.ApiGateway/       # ASP.NET Core entry point + route composition
│   ├── Lagedra.Worker/           # Quartz.NET background job host
│   ├── Lagedra.Auth/             # ASP.NET Identity, JWT, refresh tokens
│   ├── Lagedra.SharedKernel/     # Base classes, Result<T>, domain events
│   ├── Lagedra.Infrastructure/   # EF Core, email, storage, external clients
│   ├── Lagedra.TruthSurface/     # Immutable signed deal snapshots
│   ├── Lagedra.Compliance/       # Trust Ledger, violation categories
│   └── Lagedra.Modules/          # All 14 business modules
│       ├── ActivationAndBilling/
│       ├── IdentityAndVerification/
│       ├── ListingAndLocation/
│       ├── StructuredInquiry/
│       ├── VerificationAndRisk/
│       ├── InsuranceIntegration/
│       ├── ComplianceMonitoring/
│       ├── Arbitration/
│       ├── JurisdictionPacks/
│       ├── Evidence/
│       ├── Privacy/
│       ├── Notifications/
│       ├── AntiAbuseAndIntegrity/
│       └── ContentManagement/
├── tests/
│   ├── Lagedra.Tests.Architecture/
│   ├── Lagedra.Tests.Unit/
│   └── Lagedra.Tests.Integration/
├── apps/
│   ├── web/                      # Tenant/landlord React app (Vite)
│   ├── admin/                    # Ops dashboard React app (Vite)
│   └── marketing/                # Public marketing site (Next.js 15)
├── packages/
│   ├── ui/                       # Shared shadcn/ui component library
│   ├── contracts/                # Shared TypeScript DTOs and enums
│   └── test-utils/               # Shared test helpers (RTL, MSW)
├── deploy/
│   ├── env/                      # Environment variable templates
│   └── nginx/                    # Nginx configuration
├── docs/
│   ├── architecture/             # Architecture docs
│   ├── decisions/                # Architecture Decision Records (ADRs)
│   └── runbooks/                 # Ops runbooks
└── tools/
    ├── scripts/                  # Dev helper scripts
    ├── postman/                  # Postman collections
    └── openapi/                  # Generated OpenAPI specs
```

---

## Architecture

See [`docs/architecture/01-modular-monolith.md`](docs/architecture/01-modular-monolith.md) for the full architecture overview.

Key constraints:
- No module may reference another module directly (only SharedKernel, Infrastructure, TruthSurface, Compliance, JurisdictionPacks)
- All cross-module communication via domain events + Outbox pattern
- No internal HTTP calls
- Domain layer has zero dependency on Infrastructure or EF Core

---

## Contributing

See [`docs/architecture/`](docs/architecture/) for conventions, module boundaries, and ADRs before making changes.
