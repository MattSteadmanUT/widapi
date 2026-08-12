#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys the National WID 3.0 API stack to AWS using dotnet lambda deploy-serverless.

.PARAMETER DeploymentProfile
    Path to the .deployment-profile.jsonc file (e.g. cloud-deployment/dev.deployment-profile.jsonc).

.PARAMETER ValidateOnly
    When set, validates the merged CloudFormation template without deploying.
#>
param(
    [Parameter(Mandatory)]
    [string] $DeploymentProfile,

    [switch] $ValidateOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Strip JSONC comments and parse
$raw = Get-Content $DeploymentProfile -Raw
$json = $raw -replace '(?m)^\s*//.*$', '' -replace '/\*[\s\S]*?\*/', ''
$profile = $json | ConvertFrom-Json

$defaults   = $profile.awsToolsDefaults
$awsProfile = $defaults.profile
$region     = $defaults.region
$s3Bucket   = $defaults.'s3-bucket'
$s3Prefix   = $defaults.'s3-prefix'
$stackName  = $defaults.'stack-name'

# Build CloudFormation parameter overrides string
$paramOverrides = ($profile.templateParameters.PSObject.Properties | ForEach-Object {
    "$($_.Name)=$($_.Value)"
}) -join ';'

Write-Host "=== National WID API Deployment ===" -ForegroundColor Cyan
Write-Host "Profile : $awsProfile"
Write-Host "Region  : $region"
Write-Host "Stack   : $stackName"
Write-Host "Bucket  : $s3Bucket"
Write-Host ""

if ($ValidateOnly) {
    Write-Host "Validating SAM template..." -ForegroundColor Yellow
    sam validate `
        --template cloud-deployment/lambda.template `
        --region $region `
        --profile $awsProfile
    Write-Host "Validation complete." -ForegroundColor Green
    return
}

# Build release artifacts
Write-Host "Building solution (Release)..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Deploy via dotnet lambda deploy-serverless
Write-Host "Deploying stack '$stackName'..." -ForegroundColor Yellow
dotnet lambda deploy-serverless `
    --template cloud-deployment/lambda.template `
    --stack-name $stackName `
    --s3-bucket $s3Bucket `
    --s3-prefix $s3Prefix `
    --region $region `
    --profile $awsProfile `
    --template-parameters $paramOverrides

if ($LASTEXITCODE -ne 0) { throw "Deployment failed" }

Write-Host ""
Write-Host "Deployment succeeded." -ForegroundColor Green
