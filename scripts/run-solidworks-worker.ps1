#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet("build", "publish")]
    [string]$Source = "build",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$Runtime = "win-x64",
    [string]$ProjectRoot = "",
    [string[]]$InputLine = @()
)

$ErrorActionPreference = "Stop"
if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path

if ($Source -eq "publish") {
    $workerExe = Join-Path $ProjectRoot ("artifacts\worker\publish\framework-dependent\{0}\{1}\SolidWorksWorker.exe" -f $Runtime, $Configuration)
}
else {
    $workerExe = Join-Path $ProjectRoot ("adapters\solidworks\dotnet\SolidWorksWorker\bin\{0}\net8.0-windows\SolidWorksWorker.exe" -f $Configuration)
}

if (-not (Test-Path $workerExe)) {
    throw ("Worker executable not found: {0}" -f $workerExe)
}

Write-Host ("WORKER_EXE={0}" -f $workerExe)
Write-Host ("WORKER_SOURCE={0}" -f $Source)

if ($InputLine.Count -gt 0) {
    $InputLine | & $workerExe
}
else {
    & $workerExe
}

exit $LASTEXITCODE
