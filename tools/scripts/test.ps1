#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run all tests: .NET (unit + integration + architecture) and frontend (Vitest).
#>

param(
    [switch]$DotnetOnly,
    [switch]$FrontendOnly,
    [switch]$Coverage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
Push-Location $root

$failed = $false

if (-not $FrontendOnly) {
    Write-Host "── .NET tests ───────────────────────────────────────" -ForegroundColor Cyan
    $args = @("test", "Lagedra.sln", "--no-build", "-c", "Release")
    if ($Coverage) {
        $args += "--collect:XPlat Code Coverage"
        $args += "--results-directory", "coverage/dotnet"
    }
    dotnet @args
    if ($LASTEXITCODE -ne 0) { $failed = $true }
}

if (-not $DotnetOnly) {
    Write-Host "── Frontend tests ───────────────────────────────────" -ForegroundColor Cyan
    pnpm --recursive test --run
    if ($LASTEXITCODE -ne 0) { $failed = $true }
}

Pop-Location

if ($failed) {
    Write-Host "Tests failed." -ForegroundColor Red
    exit 1
}

Write-Host "All tests passed." -ForegroundColor Green
