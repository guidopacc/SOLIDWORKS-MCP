#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [string]$ValidationRoot = "C:\SolidWorksMcpValidation",
    [string]$WorkerConfiguration = "Debug",
    [string]$EmitJsonPath = "",
    [switch]$SkipNodeChecks,
    [switch]$SkipBuildOutputChecks
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$checks = @()
$hasFailures = $false

function Add-CheckResult {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Details
    )

    $script:checks += [ordered]@{
        name = $Name
        status = $Status
        details = $Details
    }

    Write-Host ("[{0}] {1}" -f $Status, $Name)
    if ($Details) {
        Write-Host ("       {0}" -f $Details)
    }

    if ($Status -eq "FAIL") {
        $script:hasFailures = $true
    }
}

function Test-CommandAvailable {
    param([string]$Name)

    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Test-DirectoryWritable {
    param([string]$Path)

    $probePath = Join-Path $Path "preflight-write-test.txt"
    try {
        "ok" | Out-File -FilePath $probePath -Encoding utf8 -Force
        Remove-Item $probePath -Force -ErrorAction SilentlyContinue
        return $true
    }
    catch {
        return $false
    }
}

function Get-SolidWorksVersionFact {
    param(
        [string]$VersionKeyPath,
        [string]$ExecutablePath
    )

    $details = @()
    $is2022 = $false

    if (Test-Path $VersionKeyPath) {
        try {
            $registryVersion = (Get-ItemProperty $VersionKeyPath).CurrentVersion
            if ($registryVersion) {
                $details += ("registry={0}" -f $registryVersion)
                if ($registryVersion -like "*2022*") {
                    $is2022 = $true
                }
            }
        }
        catch {
            $details += "registry=unavailable"
        }
    }
    else {
        $details += "registry=missing"
    }

    if (Test-Path $ExecutablePath) {
        try {
            $versionInfo = (Get-Item $ExecutablePath).VersionInfo
            if ($versionInfo.ProductName) {
                $details += ("productName={0}" -f $versionInfo.ProductName)
                if ($versionInfo.ProductName -like "*2022*") {
                    $is2022 = $true
                }
            }

            if ($versionInfo.ProductVersion) {
                $details += ("productVersion={0}" -f $versionInfo.ProductVersion)
            }
        }
        catch {
            $details += "exeVersion=unavailable"
        }
    }
    else {
        $details += "exeVersion=missing"
    }

    if ($details.Count -eq 0) {
        return [ordered]@{
            status = "WARN"
            details = "SolidWorks version details unavailable"
        }
    }

    return [ordered]@{
        status = $(if ($is2022) { "PASS" } else { "WARN" })
        details = ($details -join "; ")
    }
}

function Get-NodeCleanBaselineFact {
    param([string]$VersionText)

    $rawVersion = $VersionText.Trim()
    $normalizedVersion = $rawVersion.TrimStart("v")
    $parts = $normalizedVersion.Split(".")
    $baselineText = "^20.19.0 || >=22.12.0"

    if ($parts.Count -lt 3) {
        return [ordered]@{
            status = "WARN"
            details = ("{0}; unable to verify clean repo baseline {1}" -f $rawVersion, $baselineText)
        }
    }

    try {
        $major = [int]$parts[0]
        $minor = [int]$parts[1]
        $patch = [int]$parts[2]
    }
    catch {
        return [ordered]@{
            status = "WARN"
            details = ("{0}; unable to parse clean repo baseline {1}" -f $rawVersion, $baselineText)
        }
    }

    $meetsBaseline = $false
    if ($major -eq 20) {
        $meetsBaseline = ($minor -gt 19) -or ($minor -eq 19 -and $patch -ge 0)
    }
    elseif ($major -eq 22) {
        $meetsBaseline = ($minor -gt 12) -or ($minor -eq 12 -and $patch -ge 0)
    }
    elseif ($major -gt 22) {
        $meetsBaseline = $true
    }

    return [ordered]@{
        status = $(if ($meetsBaseline) { "PASS" } else { "WARN" })
        details = $(if ($meetsBaseline) {
            ("{0}; clean repo baseline {1}" -f $rawVersion, $baselineText)
        }
        else {
            ("{0}; below clean repo baseline {1}" -f $rawVersion, $baselineText)
        })
    }
}

Write-Host "SOLIDWORKS-MCP worker preflight"
Write-Host ("projectRoot={0}" -f $ProjectRoot)
Write-Host ("validationRoot={0}" -f $ValidationRoot)
Write-Host ""

$workerProject = Join-Path $ProjectRoot "adapters\solidworks\dotnet\SolidWorksWorker\SolidWorksWorker.csproj"
$workerTestsProject = Join-Path $ProjectRoot "adapters\solidworks\dotnet\SolidWorksWorker.Tests\SolidWorksWorker.Tests.csproj"
$workerDebugExe = Join-Path $ProjectRoot ("adapters\solidworks\dotnet\SolidWorksWorker\bin\{0}\net8.0-windows\SolidWorksWorker.exe" -f $WorkerConfiguration)
$workerPublishExe = Join-Path $ProjectRoot "artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe"
$regressionScript = Join-Path $ProjectRoot "scripts\run-real-worker-regression.ps1"
$exportRegressionScript = Join-Path $ProjectRoot "scripts\run-real-export-regression.ps1"
$buildScript = Join-Path $ProjectRoot "scripts\build-solidworks-worker.ps1"
$publishScript = Join-Path $ProjectRoot "scripts\publish-solidworks-worker.ps1"
$runScript = Join-Path $ProjectRoot "scripts\run-solidworks-worker.ps1"
$cycleScript = Join-Path $ProjectRoot "scripts\run-windows-worker-cycle.ps1"
$collectEvidenceScript = Join-Path $ProjectRoot "scripts\collect-real-worker-evidence.ps1"
$artifactIndexScript = Join-Path $ProjectRoot "scripts\update-worker-artifact-index.ps1"
$artifactPruneScript = Join-Path $ProjectRoot "scripts\prune-worker-artifacts.ps1"
$handoffPackScript = Join-Path $ProjectRoot "scripts\new-handoff-evidence-pack.ps1"
$harnessProject = "C:\SolidWorksMcpValidation\worker-e2e-harness\worker-e2e-harness.csproj"
$solidWorksProgIdKey = "Registry::HKEY_CLASSES_ROOT\SldWorks.Application"
$solidWorksVersionKey = "HKLM:\SOFTWARE\Wow6432Node\SolidWorks"
$solidWorksExe = "C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\sldworks.exe"

Add-CheckResult -Name "Project root" -Status $(if (Test-Path $ProjectRoot) { "PASS" } else { "FAIL" }) -Details $ProjectRoot
Add-CheckResult -Name "Worker project" -Status $(if (Test-Path $workerProject) { "PASS" } else { "FAIL" }) -Details $workerProject
Add-CheckResult -Name "Worker tests project" -Status $(if (Test-Path $workerTestsProject) { "PASS" } else { "FAIL" }) -Details $workerTestsProject
Add-CheckResult -Name "Worker build script" -Status $(if (Test-Path $buildScript) { "PASS" } else { "FAIL" }) -Details $buildScript
Add-CheckResult -Name "Worker publish script" -Status $(if (Test-Path $publishScript) { "PASS" } else { "FAIL" }) -Details $publishScript
Add-CheckResult -Name "Worker run script" -Status $(if (Test-Path $runScript) { "PASS" } else { "FAIL" }) -Details $runScript
Add-CheckResult -Name "Worker cycle script" -Status $(if (Test-Path $cycleScript) { "PASS" } else { "FAIL" }) -Details $cycleScript
Add-CheckResult -Name "Real regression script" -Status $(if (Test-Path $regressionScript) { "PASS" } else { "FAIL" }) -Details $regressionScript
Add-CheckResult -Name "Export regression script" -Status $(if (Test-Path $exportRegressionScript) { "PASS" } else { "FAIL" }) -Details $exportRegressionScript
Add-CheckResult -Name "Evidence collection script" -Status $(if (Test-Path $collectEvidenceScript) { "PASS" } else { "FAIL" }) -Details $collectEvidenceScript
Add-CheckResult -Name "Artifact index script" -Status $(if (Test-Path $artifactIndexScript) { "PASS" } else { "FAIL" }) -Details $artifactIndexScript
Add-CheckResult -Name "Artifact prune script" -Status $(if (Test-Path $artifactPruneScript) { "PASS" } else { "FAIL" }) -Details $artifactPruneScript
Add-CheckResult -Name "Handoff pack script" -Status $(if (Test-Path $handoffPackScript) { "PASS" } else { "FAIL" }) -Details $handoffPackScript
Add-CheckResult -Name "Harness project" -Status $(if (Test-Path $harnessProject) { "PASS" } else { "WARN" }) -Details $harnessProject

if (Test-CommandAvailable -Name "dotnet") {
    $dotnetVersion = (& dotnet --version).Trim()
    $dotnetSdkMajor = [int]($dotnetVersion.Split(".")[0])
    Add-CheckResult -Name ".NET SDK" -Status $(if ($dotnetSdkMajor -ge 8) { "PASS" } else { "FAIL" }) -Details $dotnetVersion
}
else {
    Add-CheckResult -Name ".NET SDK" -Status "FAIL" -Details "dotnet command not found"
}

if (-not $SkipNodeChecks) {
    if (Test-CommandAvailable -Name "node.exe") {
        $nodeVersion = (& node.exe --version).Trim()
        $nodeFact = Get-NodeCleanBaselineFact -VersionText $nodeVersion
        Add-CheckResult -Name "node" -Status $nodeFact.status -Details $nodeFact.details
    }
    else {
        Add-CheckResult -Name "node" -Status "WARN" -Details "node command not found"
    }

    if (Test-CommandAvailable -Name "npm.cmd") {
        Add-CheckResult -Name "npm" -Status "PASS" -Details ((& npm.cmd --version).Trim())
    }
    else {
        Add-CheckResult -Name "npm" -Status "WARN" -Details "npm command not found"
    }
}

Add-CheckResult -Name "SolidWorks ProgID" -Status $(if (Test-Path $solidWorksProgIdKey) { "PASS" } else { "FAIL" }) -Details $solidWorksProgIdKey
Add-CheckResult -Name "SolidWorks executable" -Status $(if (Test-Path $solidWorksExe) { "PASS" } else { "WARN" }) -Details $solidWorksExe

$solidWorksVersionFact = Get-SolidWorksVersionFact -VersionKeyPath $solidWorksVersionKey -ExecutablePath $solidWorksExe
Add-CheckResult -Name "SolidWorks current version" -Status $solidWorksVersionFact.status -Details $solidWorksVersionFact.details

if (-not (Test-Path $ValidationRoot)) {
    New-Item -ItemType Directory -Path $ValidationRoot -Force | Out-Null
}

Add-CheckResult -Name "Validation root" -Status $(if (Test-Path $ValidationRoot) { "PASS" } else { "FAIL" }) -Details $ValidationRoot
Add-CheckResult -Name "Validation root writable" -Status $(if (Test-DirectoryWritable -Path $ValidationRoot) { "PASS" } else { "FAIL" }) -Details $ValidationRoot

if (-not $SkipBuildOutputChecks) {
    Add-CheckResult -Name "Worker debug executable" -Status $(if (Test-Path $workerDebugExe) { "PASS" } else { "WARN" }) -Details $workerDebugExe
    Add-CheckResult -Name "Worker published executable" -Status $(if (Test-Path $workerPublishExe) { "PASS" } else { "WARN" }) -Details $workerPublishExe
}

$result = [ordered]@{
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    validationRoot = $ValidationRoot
    workerConfiguration = $WorkerConfiguration
    hasFailures = $hasFailures
    checks = $checks
}

if ($EmitJsonPath) {
    $emitDirectory = Split-Path -Parent $EmitJsonPath
    if ($emitDirectory -and -not (Test-Path $emitDirectory)) {
        New-Item -ItemType Directory -Path $emitDirectory -Force | Out-Null
    }

    $result | ConvertTo-Json -Depth 5 | Out-File -FilePath $EmitJsonPath -Encoding utf8 -Force
    Write-Host ("PREFLIGHT_JSON={0}" -f $EmitJsonPath)
}

if ($hasFailures) {
    exit 1
}

exit 0
