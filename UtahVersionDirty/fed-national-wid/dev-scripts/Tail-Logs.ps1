#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Tails CloudWatch logs for the National WID API and Ingestion Lambdas.

.PARAMETER Environment
    Deployment environment prefix (default: dev).

.PARAMETER Function
    Which Lambda to tail: 'api', 'ingestion', or 'all' (default).
#>
param(
    [string] $Environment = 'dev',
    [ValidateSet('api', 'ingestion', 'all')]
    [string] $Function = 'all'
)

$ErrorActionPreference = 'Stop'

$apiFunctionName       = "$Environment-national-wid-api"
$ingestionFunctionName = "$Environment-national-wid-ingestion"

# Determine AWS profile from environment name
$awsProfile = if ($Environment -eq 'prod') { 'GovProd' } else { 'GovDev' }

function Tail-Lambda([string] $FunctionName) {
    Write-Host "Tailing logs for: $FunctionName" -ForegroundColor Cyan
    aws logs tail "/aws/lambda/$FunctionName" `
        --follow `
        --format short `
        --profile $awsProfile `
        --region us-gov-west-1
}

switch ($Function) {
    'api'       { Tail-Lambda $apiFunctionName }
    'ingestion' { Tail-Lambda $ingestionFunctionName }
    'all' {
        # Run both in parallel jobs and merge output
        $apiJob = Start-Job -ScriptBlock {
            param($fn, $p)
            aws logs tail "/aws/lambda/$fn" --follow --format short --profile $p --region us-gov-west-1
        } -ArgumentList $apiFunctionName, $awsProfile

        $ingJob = Start-Job -ScriptBlock {
            param($fn, $p)
            aws logs tail "/aws/lambda/$fn" --follow --format short --profile $p --region us-gov-west-1
        } -ArgumentList $ingestionFunctionName, $awsProfile

        Write-Host "Tailing both Lambdas (Ctrl+C to stop)..." -ForegroundColor Yellow
        try {
            while ($true) {
                Receive-Job -Job $apiJob, $ingJob | Write-Host
                Start-Sleep -Milliseconds 500
            }
        } finally {
            Stop-Job -Job $apiJob, $ingJob
            Remove-Job -Job $apiJob, $ingJob -Force
        }
    }
}
