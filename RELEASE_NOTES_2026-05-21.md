# Lagedra Release Notes — Thursday 21 May 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Cumulative working-tree changes on branch `dev` for arbitration portal, structured verdicts, jurisdiction administration, and related access-control hardening; deployment covers API gateway, modules, and web client.

**Program references:** Phase 9 (admin / arbitrator operations — implemented in `apps/web` under `/app/admin/*` and `/app/arbitration/*`), arbitration engine, jurisdiction packs module.

---

## Executive summary

This release delivers an **operational arbitration and jurisdiction portal** inside the main web app, **structured verdicts** with configurable penalty types, and **role-based access control** enforced on both API and UI. Platform administrators gain **backlog triage**, **arbitrator caseload visibility**, and **auto-assignment** with cap-aware selection. Arbitrators receive a focused **assigned-cases** workflow with verdict issuance. Members retain **My cases** for disputes on their deals. Jurisdiction pack **create / approve / publish / deprecate** flows are admin-gated end-to-end.

---

## Highlights

### Arbitration operations (admin)

- **Arbitration backlog** — `GET /v1/admin/arbitration/backlog` with SLA-oriented queue; admin UI at `/app/admin/arbitration-backlog` with triage sorting and links into case detail.
- **Arbitrator caseload** — `GET /v1/admin/arbitration/caseload` lists panel members with active case counts and hard-cap indicators.
- **Auto-assign** — `POST /v1/admin/arbitration/cases/{caseId}/assign-auto` selects an arbitrator from the panel (soft cap 15, hard cap 20, conflict avoidance on shared deals).
- **Manual assign** — Case detail supports dropdown assign and auto-assign from the admin action card; validates panel membership and hard cap server-side.

### Arbitrator workflow

- **Scoped case list** — `GET /v1/arbitration/cases?status=` returns only cases **assigned to the caller** when role is Arbitrator; platform admins see all cases in that status.
- **Case detail** — Assigned arbitrators can issue **structured or narrative** verdicts and close cases when in the correct status; non-assigned arbitrators cannot act on the case (API enforced).
- **Navigation** — Dedicated sidebar group **Arbitration → My cases**; mobile bottom tab **Cases** for arbitrator role.

### Member / party workflow

- **My cases** — Members see cases they filed or where they are landlord/tenant on the underlying deal.
- **Evidence** — Deal parties attach sealed evidence manifests during open evidence phases; submitter is taken from the JWT (not client-supplied).
- **Appeals** — Any deal party may appeal a **Decided** case (aligned with API access rules).

### Structured verdicts

- **Outcome & severity** — `DecisionOutcome` (landlord favored, tenant favored, shared fault, dismissed) and `DecisionSeverity` (low, medium, high) stored on `arbitration_cases`.
- **Penalties** — Child table `arbitration.decision_penalties` with party, type, optional amount (cents), and description.
- **Penalty catalog** — Sixteen penalty types including monetary, deposit withhold, rent credit, account restriction, platform ban, corrective action, lease termination, and custom; validation rules define which types require a dollar amount.
- **Unified issue-decision API** — `POST /v1/arbitration/cases/{caseId}/decision` accepts `isStructured`, outcome, severity, and penalty array; narrative-only mode remains supported.
- **UI** — `VerdictForm` replaces the legacy free-text-only decision section; labels and severity guidance in `verdictLabels.ts` / `penaltyTypes.ts`.

### Jurisdiction packs (admin)

- **Pack catalog** — `GET /v1/admin/jurisdiction-packs` and pending-approvals queue for dual-control.
- **Version lifecycle UI** — `/app/admin/jurisdiction-packs`: create pack, list versions, set draft effective date, request approval, approve, publish, deprecate.
- **Dual-control inbox** — `/app/admin/dual-control` global pending-approval queue with links into pack management.
- **API hardening** — All pack **mutations** and version **list/detail** admin reads require `RequirePlatformAdmin`; active pack by jurisdiction code (`GET /v1/jurisdiction-packs/{code}`) remains available to authenticated users for product flows.

### Access control & security

- **Case access evaluator** — Central `ArbitrationCaseAccessEvaluator` enforces view, evidence, decide/close, and appeal permissions per case.
- **403 / 404 contracts** — `Arbitration.Forbidden` → HTTP 403; missing or inaccessible case → 404 on get.
- **Arbitrator cases endpoint** — `GET /v1/arbitrators/{userId}/cases` limited to self or platform admin.
- **Frontend route guard** — `RequireArbitrationAccess` on `/app/arbitration/*` (Member, Arbitrator, PlatformAdmin only).

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/admin/arbitration/backlog` | Platform admin | Operational backlog with arbitrator email resolution. |
| `GET` | `/v1/admin/arbitration/caseload` | Platform admin | Panel caseload for assignment UI. |
| `POST` | `/v1/admin/arbitration/cases/{caseId}/assign-auto` | Platform admin | Cap-aware auto-assignment. |
| `GET` | `/v1/admin/jurisdiction-packs` | Platform admin | Pack catalog for admin UI. |
| `GET` | `/v1/admin/jurisdiction-packs/pending-approvals` | Platform admin | Dual-control pending queue. |
| `POST` | `/v1/arbitration/cases/{caseId}/decision` | Arbitrator or admin | Structured or narrative verdict (+ penalties). |
| `GET` | `/v1/arbitration/cases/{caseId}` | Authenticated | Case detail (access-scoped). |
| `GET` | `/v1/arbitration/cases` | Authenticated | List by status (role-scoped). |

*Existing arbitration routes (`evidence`, `assign`, `evidence-complete`, `close`, `appeal`, `file`) retain prior paths; behavior and authorization are tightened as described below.*

### Authorization matrix (arbitration)

| Action | Platform admin | Assigned arbitrator | Deal party (host/guest/filer) | Other authenticated |
|--------|----------------|---------------------|-------------------------------|---------------------|
| View case | Yes | Yes | Yes | No |
| List cases | All in status | Assigned only | Own / deal-related | Empty |
| Attach evidence | Yes | No | Yes | No |
| Issue decision / close | Yes | Yes (assigned) | No | No |
| Appeal | Yes | No | Yes | No |
| Mark evidence complete / assign | Yes | No | No | No |

### Domain & application layer (selected)

- **Arbitration:** `ArbitrationCaseAccess`, `ArbitrationCaseAccessEvaluator`, `ArbitratorAssignmentSelector`, `IArbitratorPanelProvider`, extended `IssueDecisionCommand`, `ListCasesByStatusQuery` scoping, `GetCaseQuery` access check, `PenaltyTypeRules`, `StructuredVerdictPolicy`.
- **Jurisdiction:** `ListJurisdictionPacksQuery`, `ListPendingPackApprovalsQuery`; approve endpoint uses JWT user when body omits `ApproverId`.
- **Auth:** `UserStripeProfileService` / arbitrator panel provider (from prior tranche; required for assignment).

---

## Frontend changes

### Navigation (`permissions.ts` / `AppShell`)

| Role | Arbitration | Admin portal |
|------|-------------|--------------|
| **Member** | Bookings → **My cases** | — |
| **Arbitrator** | **Arbitration** → **My cases**; bottom tab **Cases** | — |
| **Platform admin** | **My cases** + Operations → **Arbitration Backlog**; Configuration → **Jurisdiction Packs**, **Dual Control** | `/app/admin/*` |

### New or substantially updated pages & components

- **`ArbitrationBacklogPage`** — Caseload table, triage, auto-assign, drill-through to case detail.
- **`JurisdictionPackVersionsPage`** — Full pack/version lifecycle management.
- **`DualControlApprovalsPage`** — Cross-pack pending approvals.
- **`CaseListPage`** / **`CaseDetailPage`** — Role-aware copy; admin vs arbitrator action cards; `VerdictForm` for structured verdicts.
- **`VerdictForm`**, **`penaltyTypes.ts`**, **`verdictLabels.ts`** — Structured verdict builder and display.
- **`RequireArbitrationAccess`** — Route guard for arbitration paths.

### API client

- Extended `adminApi` for backlog, caseload, auto-assign, jurisdiction lists.
- `IssueDecisionRequest` and penalty DTOs in `types.ts`; `arbitrationApi.issueDecision` sends full verdict payload.
- Evidence attach no longer sends `submittedBy` (server derives from session).

---

## Database & schema

| Context | Migration | Summary |
|---------|-----------|---------|
| `ArbitrationDbContext` | `20260521120000_AddStructuredVerdict` | `DecisionOutcome`, `DecisionSeverity`, `IsStructuredVerdict` on `arbitration.arbitration_cases`; new `arbitration.decision_penalties` with audit columns. |

**Apply:**

```powershell
dotnet ef database update `
  --project src/Lagedra.Modules/Arbitration `
  --startup-project src/Lagedra.ApiGateway `
  --context ArbitrationDbContext
```

Or run `tools/scripts/db-migrate.ps1` (includes all contexts).

**Note:** Migration includes a `.Designer.cs` so EF discovers it in the chain. If the database was patched manually earlier, ensure `__EFMigrationsHistory` contains `20260521120000_AddStructuredVerdict` before skipping.

---

## Security & privacy posture

- **Case IDs are not public capabilities** — Knowledge of a `caseId` alone does not grant access; evaluator checks role, assignment, and deal participation.
- **Jurisdiction pack writes** — Restricted to platform administrators at the HTTP layer; reduces risk of member-tier token abuse on publish/approve paths.
- **One-tap and booking flows** from the 16 May release are unchanged; this tranche does not alter token-gated host approval.

---

## Configuration & dependencies

- **Arbitrator panel** — Users with role `Arbitrator` in auth; assignment selector reads panel via `IArbitratorPanelProvider`.
- **Platform settings** — Filing fees still driven by `arbitration_fee.*` keys (unchanged).
- **No new third-party keys** for this tranche beyond existing Stripe/auth/email stack.

---

## Deployment & verification checklist

1. **Build** API gateway and `apps/web`; run test pipeline if configured.
2. **Migrate** `ArbitrationDbContext` (`20260521120000_AddStructuredVerdict`).
3. **Deploy** API then web static assets / SSR host.
4. **Smoke tests**
   - **Admin:** Open `/app/admin/arbitration-backlog`; auto-assign a case; open case from backlog.
   - **Arbitrator:** Sign in as `arbitrator@lagedra.dev` (or panel user); confirm list shows **only assigned** cases; issue structured verdict on assigned case; confirm **403** on another arbitrator’s case ID.
   - **Member:** File case on active deal; attach evidence; confirm another user cannot `GET` the case.
   - **Jurisdiction:** As platform admin, create draft → request approval → approve (two distinct admins if dual-control enforced) → publish; confirm member cannot `POST` publish.
   - **Regression:** List cases as admin vs arbitrator vs member; appeal as non-filer party on deal.

---

## Known limitations & follow-up (engineering)

- **Penalty enforcement** — Penalties are stored and displayed; automatic Stripe charges, deposit withholds, trust-ledger writes, and account restrictions are **not** wired from verdict issuance yet.
- **Separate `apps/admin` app** — Phase 9 admin UX lives in `apps/web`; a dedicated admin SPA remains a future split if desired.
- **Rich jurisdiction rule editor** — Pack metadata and lifecycle are manageable; deep JSON rule editing in UI is still minimal (API supports draft fields).
- **Legacy protocol/binding command handlers** — Internal `IssueProtocolDecisionCommand` / `IssueBindingAwardCommand` paths do not use the new access evaluator; public API uses unified `IssueDecision`.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-05-21 | Arbitration portal, structured verdicts, jurisdiction admin, RBAC hardening. |

---

*End of release notes.*
