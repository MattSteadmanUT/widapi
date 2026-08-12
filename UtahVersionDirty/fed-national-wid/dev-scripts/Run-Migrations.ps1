#!/usr/bin/env pwsh
param(
    [string] $FunctionName = "dev-national-wid-ingestion",
    [string] $Profile = "GovDev",
    [string] $Region = "us-gov-west-1",
    [switch] $VerifyOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$payload = if ($VerifyOnly) {
    '{"verifyOnly":true}'
}
else {
    '{"runMigrationsOnly":true}'
}

$outFile = Join-Path $PSScriptRoot "..\ingest-migration-output.json"

Write-Host "Invoking $FunctionName (verifyOnly=$VerifyOnly)..." -ForegroundColor Yellow
aws lambda invoke `
    --function-name $FunctionName `
    --profile $Profile `
    --region $Region `
    --cli-binary-format raw-in-base64-out `
    --payload $payload `
    $outFile | Out-Null

$result = Get-Content $outFile -Raw | ConvertFrom-Json

if ($result.PSObject.Properties.Name -contains 'migrationsApplied') {
    Write-Host "MigrationsApplied: $($result.migrationsApplied)"
}
else {
    Write-Host "MigrationsApplied: (not present in Lambda response)"
}

if ($result.PSObject.Properties.Name -contains 'migrationsSkipped') {
    Write-Host "MigrationsSkipped: $($result.migrationsSkipped)"
}
else {
    Write-Host "MigrationsSkipped: (not present in Lambda response)"
}

if ($result.PSObject.Properties.Name -contains 'tableChecks' -and $null -ne $result.tableChecks) {
    $missing = @()
    foreach ($p in $result.tableChecks.PSObject.Properties) {
        $ok = [bool]$p.Value
        Write-Host ("{0}: {1}" -f $p.Name, ($(if ($ok) { 'ok' } else { 'missing' })))
        if (-not $ok) { $missing += $p.Name }
    }

    if ($missing.Count -gt 0) {
        throw "Missing required tables: $($missing -join ', ')"
    }
}
else {
    throw "Lambda response does not include tableChecks. Deploy latest ingestion code before running migration verification."
}

Write-Host "Migration/verification completed successfully." -ForegroundColor Green