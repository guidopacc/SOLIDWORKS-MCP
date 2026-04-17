#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ProjectRoot = "",
    [switch]$SkipPreflight
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$workerProject = Join-Path $ProjectRoot "adapters\solidworks\dotnet\SolidWorksWorker\SolidWorksWorker.csproj"
$publishDirectory = Join-Path $ProjectRoot ("artifacts\worker\publish\framework-dependent\{0}\{1}" -f $Runtime, $Configuration)
$publishExe = Join-Path $publishDirectory "SolidWorksWorker.exe"
$artifactPath = Join-Path $publishDirectory "worker-publish.json"
$preflightScript = Join-Path $ProjectRoot "scripts\validate-windows-environment.ps1"

if (-not $SkipPreflight) {
    & $preflightScript -ProjectRoot $ProjectRoot -SkipNodeChecks -SkipBuildOutputChecks
}

if (-not (Test-Path $publishDirectory)) {
    New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
}

Write-Host ("Publishing worker: {0}" -f $workerProject)
& dotnet publish $workerProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    /p:PublishSingleFile=false `
    -o $publishDirectory

$result = [ordered]@{
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    workerProject = $workerProject
    configuration = $Configuration
    runtime = $Runtime
    publishMode = "framework-dependent"
    publishSingleFile = $false
    publishDirectory = $publishDirectory
    publishExe = $publishExe
    publishExeExists = (Test-Path $publishExe)
}

$result | ConvertTo-Json -Depth 4 | Out-File -FilePath $artifactPath -Encoding utf8 -Force

Write-Output ("WORKER_PUBLISH_EXE={0}" -f $publishExe)
Write-Output ("PUBLISH_ARTIFACT={0}" -f $artifactPath)
exit 0
