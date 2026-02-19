#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run all linters: dotnet format (C#) and pnpm lint (TypeScript/React).
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
Push-Location $root

$failed = $false

Write-Host "── .NET format check ────────────────────────────────" -ForegroundColor Cyan
dotnet format Lagedra.sln --verify-no-changes --severity warn
if ($LASTEXITCODE -ne 0) { $failed = $true }

Write-Host "── pnpm lint ────────────────────────────────────────" -ForegroundColor Cyan
pnpm --recursive lint
if ($LASTEXITCODE -ne 0) { $failed = $true }

Pop-Location

if ($failed) {
    Write-Host "Lint failed." -ForegroundColor Red
    exit 1
}

Write-Host "All lint checks passed." -ForegroundColor Green
