#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Watches a CloudFormation stack until it reaches a terminal state and prints useful failure details.

.PARAMETER StackName
    CloudFormation stack name.

.PARAMETER Profile
    AWS CLI profile name.

.PARAMETER Region
    AWS region.

.PARAMETER TimeoutMinutes
    Maximum time to wait before exiting with timeout.

.PARAMETER PollSeconds
    Poll interval in seconds.
#>
param(
    [Parameter(Mandatory)]
    [string] $StackName,

    [Parameter(Mandatory)]
    [string] $Profile,

    [Parameter(Mandatory)]
    [string] $Region,

    [int] $TimeoutMinutes = 30,
    [int] $PollSeconds = 10
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$terminalSuccess = @(
    'CREATE_COMPLETE',
    'UPDATE_COMPLETE',
    'IMPORT_COMPLETE',
    'ROLLBACK_COMPLETE',
    'UPDATE_ROLLBACK_COMPLETE',
    'DELETE_COMPLETE'
)

$terminalFailure = @(
    'CREATE_FAILED',
    'ROLLBACK_FAILED',
    'UPDATE_FAILED',
    'UPDATE_ROLLBACK_FAILED',
    'DELETE_FAILED',
    'IMPORT_ROLLBACK_FAILED'
)

$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
$lastStatus = $null

Write-Host "Watching stack '$StackName' in $Region (profile: $Profile)" -ForegroundColor Cyan

while ($true) {
    $stack = aws cloudformation describe-stacks `
        --stack-name $StackName `
        --profile $Profile `
        --region $Region `
        --query 'Stacks[0].{Status:StackStatus,Reason:StackStatusReason}' `
        --output json | ConvertFrom-Json

    $status = [string]$stack.Status
    $reason = [string]$stack.Reason

    if ($status -ne $lastStatus) {
        $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        if ([string]::IsNullOrWhiteSpace($reason)) {
            Write-Host "[$ts] $status" -ForegroundColor Yellow
        }
        else {
            Write-Host "[$ts] $status :: $reason" -ForegroundColor Yellow
        }
        $lastStatus = $status
    }

    if ($terminalSuccess -contains $status) {
        Write-Host "Terminal success state reached: $status" -ForegroundColor Green

        if ($status -like '*ROLLBACK_COMPLETE') {
            Write-Host "Note: stack is stable but deployment changes were rolled back." -ForegroundColor Magenta
        }

        if ($status -eq 'DELETE_COMPLETE') {
            Write-Host "Stack no longer exists." -ForegroundColor Magenta
        }

        exit 0
    }

    if ($terminalFailure -contains $status) {
        Write-Host "Terminal failure state reached: $status" -ForegroundColor Red
        Write-Host "Most recent failed resources:" -ForegroundColor Red

        aws cloudformation describe-stack-events `
            --stack-name $StackName `
            --profile $Profile `
            --region $Region `
            --max-items 50 `
            --query "StackEvents[?contains(ResourceStatus, 'FAILED')].[Timestamp,ResourceStatus,LogicalResourceId,ResourceType,ResourceStatusReason]" `
            --output table

        exit 2
    }

    if ((Get-Date) -ge $deadline) {
        Write-Host "Timed out waiting for terminal stack state." -ForegroundColor Red
        Write-Host "Current status: $status" -ForegroundColor Red
        Write-Host "Recent events:" -ForegroundColor Red
        aws cloudformation describe-stack-events `
            --stack-name $StackName `
            --profile $Profile `
            --region $Region `
            --max-items 20 `
            --query "StackEvents[].[Timestamp,ResourceStatus,LogicalResourceId,ResourceType,ResourceStatusReason]" `
            --output table
        exit 3
    }

    Start-Sleep -Seconds $PollSeconds
}
