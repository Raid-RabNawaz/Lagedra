#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that widens listings.Description
    from varchar(5000) to unbounded text.

    One DbContext changes, one column type is widened in place (no data change,
    no rewrite needed by Postgres for varchar -> text):

      - ListingsDbContext (listings schema):
          listings.Description: character varying(5000) -> text

    Why: channel content sync (Hostaway/Guesty/OwnerRez) imports listing
    descriptions longer than 5000 characters. Every sync run (06:00/18:00 UTC)
    failed with Postgres 22001 "value too long for type character varying(5000)"
    for the affected listings, so their content never updated.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: WidenListingDescription).

.EXAMPLE
    pwsh tools/scripts/db-migrate-widen-listing-description.ps1

.EXAMPLE
    # Re-apply only (migration already committed)
    pwsh tools/scripts/db-migrate-widen-listing-description.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "WidenListingDescription"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/ListingAndLocation"; Context = "ListingsDbContext" }
)

Write-Host "Widen listings.Description — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

# Build once so the EF tooling reads the latest model snapshot.
Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    foreach ($c in $contexts) {
        $proj = Join-Path $root $c.Project
        Write-Host "→ add $Name :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef migrations add $Name `
            --project         $proj `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "migrations add failed for $($c.Context). Aborting." }
        Write-Host ""
    }
}

if (-not $SkipUpdate) {
    foreach ($c in $contexts) {
        $proj = Join-Path $root $c.Project
        Write-Host "→ database update :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef database update `
            --project         $proj `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "database update failed for $($c.Context). Aborting." }
        Write-Host ""
    }
}

Write-Host "Widen listings.Description migration complete." -ForegroundColor Green
