#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run the database seeder (reference data: jurisdiction packs, roles, etc.).
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."

Write-Host "Running seed runner..." -ForegroundColor Cyan

dotnet run `
    --project (Join-Path $root "src/Lagedra.ApiGateway") `
    --launch-profile "SeedRunner" `
    -- seed

Write-Host "Seed complete." -ForegroundColor Green
