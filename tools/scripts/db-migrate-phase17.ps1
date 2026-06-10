#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Apply the Phase 17 (Booking Flow & Inquiry Model Cleanup) EF Core migration.

    Migration applied (idempotent — already-applied ones are skipped by EF):
      - InquiryDbContext : 20260514222405_AddListingScopedInquiry

    What this migration does:
      * Makes inquiry.sessions.DealId nullable (a session can now be
        listing-scoped before the tenant ever applies).
      * Adds inquiry.sessions.ListingId + TenantUserId (both required) and
        backfills them for existing rows by joining through
        billing.deal_applications on DealId.
      * Adds index on ListingId, plus a composite index on
        (ListingId, TenantUserId) so the "find my open thread for this
        listing" lookup stays O(1).
      * Adds inquiry.questions.OpenQuestionText (varchar(1000), nullable)
        for free-form questions inside a chosen category.

    Run AFTER `dotnet build` so the EF tooling reads the latest model
    snapshot, and AFTER any Phase 16 migrations have been applied (the
    backfill SELECTs from billing.deal_applications which Phase 16 owns).

.PARAMETER WhatIf
    Print the dotnet ef commands without executing them.

.EXAMPLE
    pwsh tools/scripts/db-migrate-phase17.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-phase17.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$migrations = @(
    @{
        Project   = "src/Lagedra.Modules/StructuredInquiry"
        Context   = "InquiryDbContext"
        Migration = "20260514222405_AddListingScopedInquiry"
    }
)

Write-Host "Phase 17 — Booking Flow & Inquiry Model Cleanup migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

foreach ($m in $migrations) {
    $proj = Join-Path $root $m.Project
    if (-not (Test-Path $proj)) {
        Write-Host "Skipping $($m.Context) — project not found at $proj" -ForegroundColor Yellow
        continue
    }

    Write-Host "→ $($m.Context) :: $($m.Migration)" -ForegroundColor Cyan

    if ($PSCmdlet.ShouldProcess($m.Context, "dotnet ef database update $($m.Migration)")) {
        dotnet ef database update $m.Migration `
            --project        $proj `
            --startup-project $startup `
            --context        $m.Context

        if ($LASTEXITCODE -ne 0) {
            throw "Migration failed for $($m.Context) ($($m.Migration)). Aborting."
        }
    }

    Write-Host ""
}

Write-Host "Phase 17 migrations complete." -ForegroundColor Green
