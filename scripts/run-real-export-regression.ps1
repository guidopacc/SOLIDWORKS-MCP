#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [string]$HarnessProject = "C:\SolidWorksMcpValidation\worker-e2e-harness\worker-e2e-harness.csproj",
    [string]$WorkerExe = "",
    [switch]$SkipPreflight
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$regressionScript = Join-Path $ProjectRoot "scripts\run-real-worker-regression.ps1"

if ($WorkerExe) {
    if ($SkipPreflight) {
        & $regressionScript -ProjectRoot $ProjectRoot -HarnessProject $HarnessProject -Scenarios @("slice4") -WorkerExe $WorkerExe -SkipPreflight
    }
    else {
        & $regressionScript -ProjectRoot $ProjectRoot -HarnessProject $HarnessProject -Scenarios @("slice4") -WorkerExe $WorkerExe
    }
}
else {
    if ($SkipPreflight) {
        & $regressionScript -ProjectRoot $ProjectRoot -HarnessProject $HarnessProject -Scenarios @("slice4") -SkipPreflight
    }
    else {
        & $regressionScript -ProjectRoot $ProjectRoot -HarnessProject $HarnessProject -Scenarios @("slice4")
    }
}

exit $LASTEXITCODE
