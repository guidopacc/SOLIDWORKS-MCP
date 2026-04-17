#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$ProjectRoot = "",
    [switch]$SkipTests,
    [switch]$SkipPreflight
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$workerProject = Join-Path $ProjectRoot "adapters\solidworks\dotnet\SolidWorksWorker\SolidWorksWorker.csproj"
$workerTestsProject = Join-Path $ProjectRoot "adapters\solidworks\dotnet\SolidWorksWorker.Tests\SolidWorksWorker.Tests.csproj"
$workerExe = Join-Path $ProjectRoot ("adapters\solidworks\dotnet\SolidWorksWorker\bin\{0}\net8.0-windows\SolidWorksWorker.exe" -f $Configuration)
$artifactDirectory = Join-Path $ProjectRoot ("artifacts\worker\build\{0}" -f $Configuration)
$artifactPath = Join-Path $artifactDirectory "worker-build.json"
$preflightScript = Join-Path $ProjectRoot "scripts\validate-windows-environment.ps1"

if (-not $SkipPreflight) {
    & $preflightScript -ProjectRoot $ProjectRoot -SkipNodeChecks -SkipBuildOutputChecks
}

Write-Host ("Building worker: {0}" -f $workerProject)
& dotnet build $workerProject -c $Configuration

$testsPassed = $false
if (-not $SkipTests) {
    Write-Host ("Running worker tests: {0}" -f $workerTestsProject)
    & dotnet test $workerTestsProject -c $Configuration
    $testsPassed = $true
}

if (-not (Test-Path $artifactDirectory)) {
    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
}

$result = [ordered]@{
    generatedAt = (Get-Date).ToString("s")
    configuration = $Configuration
    workerProject = $workerProject
    workerTestsProject = $workerTestsProject
    workerExe = $workerExe
    workerExeExists = (Test-Path $workerExe)
    testsExecuted = (-not $SkipTests)
    testsPassed = $testsPassed
}

$result | ConvertTo-Json -Depth 4 | Out-File -FilePath $artifactPath -Encoding utf8 -Force

Write-Output ("WORKER_EXE={0}" -f $workerExe)
Write-Output ("BUILD_ARTIFACT={0}" -f $artifactPath)
exit 0
