#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for manual KYC document review.

    One DbContext changes:

      - IdentityDbContext (identity schema):
          adds the kyc_documents table (UserId, DocumentType, StorageKey,
          FileName, MimeType, SizeBytes, UploadedAt) used by the manual
          ID + live-selfie verification flow. Purely additive — no existing
          rows or columns are touched.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).
    Use on environments where the migration is already committed.

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddKycDocuments).

.EXAMPLE
    pwsh tools/scripts/db-migrate-manual-kyc.ps1

.EXAMPLE
    # Re-apply only (e.g. on another environment), migration already committed
    pwsh tools/scripts/db-migrate-manual-kyc.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddKycDocuments"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$proj    = Join-Path $root "src/Lagedra.Modules/IdentityAndVerification"

Write-Host "Manual KYC — EF Core migration (IdentityDbContext)" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

# Build once so the EF tooling reads the latest model snapshot.
Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    Write-Host "→ add $Name :: IdentityDbContext" -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project         $proj `
        --startup-project $startup `
        --context         IdentityDbContext
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed for IdentityDbContext. Aborting." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "→ database update :: IdentityDbContext" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $proj `
        --startup-project $startup `
        --context         IdentityDbContext
    if ($LASTEXITCODE -ne 0) { throw "database update failed for IdentityDbContext. Aborting." }
    Write-Host ""
}

Write-Host "Manual KYC migration complete." -ForegroundColor Green
