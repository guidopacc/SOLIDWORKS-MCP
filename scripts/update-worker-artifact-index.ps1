#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [string]$ValidationRoot = "C:\SolidWorksMcpValidation",
    [int]$MaxCycles = 10,
    [int]$MaxEvidenceBundles = 10,
    [int]$MaxRegressionRuns = 10,
    [int]$MaxHandoffPacks = 10
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$cyclesRoot = Join-Path $ProjectRoot "artifacts\worker\cycles"
$evidenceRoot = Join-Path $ProjectRoot "artifacts\real-evidence"
$handoffRoot = Join-Path $ProjectRoot "artifacts\handoff-packs"
$indexRoot = Join-Path $ProjectRoot "artifacts\indexes"
$regressionRoot = Join-Path $ValidationRoot "regression-runs"
$indexPath = Join-Path $indexRoot "worker-artifact-index.json"
$summaryPath = Join-Path $indexRoot "worker-artifact-index-summary.txt"

if (-not (Test-Path $indexRoot)) {
    New-Item -ItemType Directory -Path $indexRoot -Force | Out-Null
}

function Read-JsonFile {
    param([string]$Path)

    if (-not $Path -or -not (Test-Path $Path)) {
        return $null
    }

    try {
        return Get-Content $Path -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Resolve-BundleMode {
    param($Manifest)

    if ($Manifest -and $Manifest.bundleMode) {
        $mode = [string]$Manifest.bundleMode
        if (-not [string]::IsNullOrWhiteSpace($mode)) {
            return $mode.Trim()
        }
    }

    if ($Manifest -and $null -ne $Manifest.includeCadFiles) {
        if ([bool]$Manifest.includeCadFiles) {
            return "full_with_cad_files"
        }

        return "standard_minimal"
    }

    return ""
}

function New-CycleEntry {
    param([System.IO.DirectoryInfo]$Directory)

    $manifestPath = Join-Path $Directory.FullName "windows-worker-cycle.json"
    $summaryPathLocal = Join-Path $Directory.FullName "windows-worker-cycle-summary.txt"
    $manifest = Read-JsonFile -Path $manifestPath
    if (-not $manifest) {
        return $null
    }

    return [ordered]@{
        name = $Directory.Name
        generatedAt = $manifest.generatedAt
        path = $Directory.FullName
        bundleMode = (Resolve-BundleMode -Manifest $manifest)
        includeCadFiles = [bool]$manifest.includeCadFiles
        smokePassed = if ($manifest.outcome) { [bool]$manifest.outcome.smokePassed } else { $null }
        regressionPassed = if ($manifest.outcome) { [bool]$manifest.outcome.regressionPassed } else { $null }
        passedScenarioCount = if ($manifest.outcome) { $manifest.outcome.passedScenarioCount } else { $null }
        failedScenarioCount = if ($manifest.outcome) { $manifest.outcome.failedScenarioCount } else { $null }
        cycleManifestPath = $manifestPath
        cycleSummaryPath = $summaryPathLocal
        regressionRunManifestPath = if ($manifest.regression) { $manifest.regression.runManifestPath } else { $null }
        evidenceBundlePath = if ($manifest.evidenceBundle) { $manifest.evidenceBundle.bundlePath } else { $null }
        evidenceManifestPath = if ($manifest.evidenceBundle) { $manifest.evidenceBundle.manifestPath } else { $null }
        evidenceSummaryPath = if ($manifest.evidenceBundle) { $manifest.evidenceBundle.summaryPath } else { $null }
        publishedWorkerExe = if ($manifest.publish) { $manifest.publish.workerExe } else { $null }
    }
}

function New-EvidenceBundleEntry {
    param([System.IO.DirectoryInfo]$Directory)

    $manifestPath = Join-Path $Directory.FullName "evidence-manifest.json"
    $summaryPathLocal = Join-Path $Directory.FullName "evidence-summary.txt"
    $manifest = Read-JsonFile -Path $manifestPath
    if (-not $manifest) {
        return $null
    }

    return [ordered]@{
        name = $Directory.Name
        generatedAt = $manifest.generatedAt
        path = $Directory.FullName
        bundleMode = (Resolve-BundleMode -Manifest $manifest)
        includeCadFiles = [bool]$manifest.includeCadFiles
        scenarioPassCount = if ($manifest.summary) { $manifest.summary.scenarioPassCount } else { $null }
        scenarioFailCount = if ($manifest.summary) { $manifest.summary.scenarioFailCount } else { $null }
        copiedScenarioCount = if ($manifest.summary) { $manifest.summary.copiedScenarioCount } else { $null }
        copiedEvidenceFileCount = if ($manifest.summary) { $manifest.summary.copiedEvidenceFileCount } else { $null }
        copiedCadFileCount = if ($manifest.summary) { $manifest.summary.copiedCadFileCount } else { $null }
        copiedExtraFileCount = if ($manifest.summary) { $manifest.summary.copiedExtraFileCount } else { $null }
        totalCopiedSizeBytes = if ($manifest.summary) { $manifest.summary.totalCopiedSizeBytes } else { $null }
        evidenceManifestPath = $manifestPath
        evidenceSummaryPath = $summaryPathLocal
    }
}

function New-RegressionEntry {
    param([System.IO.FileInfo]$File)

    $manifest = Read-JsonFile -Path $File.FullName
    if (-not $manifest) {
        return $null
    }

    return [ordered]@{
        name = $File.Name
        generatedAt = $manifest.generatedAt
        path = $File.FullName
        workerExe = $manifest.workerExe
        hasFailures = [bool]$manifest.hasFailures
        passedScenarioCount = $manifest.passedScenarioCount
        failedScenarioCount = $manifest.failedScenarioCount
        durationMs = $manifest.durationMs
    }
}

function New-HandoffEntry {
    param([System.IO.DirectoryInfo]$Directory)

    $manifestPath = Join-Path $Directory.FullName "handoff-manifest.json"
    $summaryPathLocal = Join-Path $Directory.FullName "handoff-summary.txt"
    $manifest = Read-JsonFile -Path $manifestPath
    if (-not $manifest) {
        return $null
    }

    return [ordered]@{
        name = $Directory.Name
        generatedAt = $manifest.generatedAt
        path = $Directory.FullName
        sourceBundleMode = $manifest.sourceBundleMode
        includeCadFiles = [bool]$manifest.includeCadFiles
        copiedScenarioCount = $manifest.copiedScenarioCount
        scenarioCount = $manifest.scenarioCount
        handoffManifestPath = $manifestPath
        handoffSummaryPath = $summaryPathLocal
        sourceEvidenceBundlePath = $manifest.sourceEvidenceBundlePath
        sourceCycleManifestPath = $manifest.sourceCycleManifestPath
        sourceRegressionManifestPath = $manifest.sourceRegressionManifestPath
    }
}

function Get-DirectoryEntries {
    param(
        [string]$Root,
        [scriptblock]$Factory
    )

    if (-not (Test-Path $Root)) {
        return @()
    }

    $entries = @()
    foreach ($directory in Get-ChildItem -Path $Root -Directory | Sort-Object Name -Descending) {
        $entry = & $Factory $directory
        if ($entry) {
            $entries += $entry
        }
    }

    return $entries
}

function Get-FileEntries {
    param(
        [string]$Root,
        [string]$Filter,
        [scriptblock]$Factory
    )

    if (-not (Test-Path $Root)) {
        return @()
    }

    $entries = @()
    foreach ($file in Get-ChildItem -Path $Root -File -Filter $Filter | Sort-Object Name -Descending) {
        $entry = & $Factory $file
        if ($entry) {
            $entries += $entry
        }
    }

    return $entries
}

$cycleEntries = Get-DirectoryEntries -Root $cyclesRoot -Factory ${function:New-CycleEntry}
$bundleEntries = Get-DirectoryEntries -Root $evidenceRoot -Factory ${function:New-EvidenceBundleEntry}
$regressionEntries = Get-FileEntries -Root $regressionRoot -Filter "worker-regression-*.json" -Factory ${function:New-RegressionEntry}
$handoffEntries = Get-DirectoryEntries -Root $handoffRoot -Factory ${function:New-HandoffEntry}

$latestCycle = $cycleEntries | Select-Object -First 1
$latestPassingCycle = $cycleEntries | Where-Object { $_["regressionPassed"] } | Select-Object -First 1
$latestStandardCycle = $cycleEntries | Where-Object { $_["bundleMode"] -eq "standard_minimal" } | Select-Object -First 1
$latestFullCycle = $cycleEntries | Where-Object { $_["bundleMode"] -eq "full_with_cad_files" } | Select-Object -First 1

$latestBundle = $bundleEntries | Select-Object -First 1
$latestPassingBundle = $bundleEntries | Where-Object { $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1
$latestStandardBundle = $bundleEntries | Where-Object { $_["bundleMode"] -eq "standard_minimal" -and $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1
$latestFullBundle = $bundleEntries | Where-Object { $_["bundleMode"] -eq "full_with_cad_files" -and $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1

$latestRegression = $regressionEntries | Select-Object -First 1
$latestPassingRegression = $regressionEntries | Where-Object { -not $_["hasFailures"] } | Select-Object -First 1
$latestHandoffPack = $handoffEntries | Select-Object -First 1

$latestCyclePath = if ($latestCycle) { $latestCycle.path } else { "" }
$latestPassingCyclePath = if ($latestPassingCycle) { $latestPassingCycle.path } else { "" }
$latestStandardCyclePath = if ($latestStandardCycle) { $latestStandardCycle.path } else { "" }
$latestFullCyclePath = if ($latestFullCycle) { $latestFullCycle.path } else { "" }
$latestBundlePath = if ($latestBundle) { $latestBundle.path } else { "" }
$latestPassingBundlePath = if ($latestPassingBundle) { $latestPassingBundle.path } else { "" }
$latestStandardBundlePath = if ($latestStandardBundle) { $latestStandardBundle.path } else { "" }
$latestFullBundlePath = if ($latestFullBundle) { $latestFullBundle.path } else { "" }
$latestRegressionPath = if ($latestRegression) { $latestRegression.path } else { "" }
$latestHandoffPackPath = if ($latestHandoffPack) { $latestHandoffPack.path } else { "" }

$index = [ordered]@{
    schemaVersion = "worker-artifact-index-2026-04-10-v1"
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    validationRoot = $ValidationRoot
    roots = [ordered]@{
        cyclesRoot = $cyclesRoot
        evidenceRoot = $evidenceRoot
        regressionRoot = $regressionRoot
        handoffRoot = $handoffRoot
        indexRoot = $indexRoot
    }
    latest = [ordered]@{
        cycle = $latestCycle
        passingCycle = $latestPassingCycle
        standardCycle = $latestStandardCycle
        fullEvidenceCycle = $latestFullCycle
        evidenceBundle = $latestBundle
        passingEvidenceBundle = $latestPassingBundle
        standardEvidenceBundle = $latestStandardBundle
        fullEvidenceBundle = $latestFullBundle
        regressionRun = $latestRegression
        passingRegressionRun = $latestPassingRegression
        handoffPack = $latestHandoffPack
    }
    preferred = [ordered]@{
        routineCycle = if ($latestStandardCycle) { $latestStandardCycle } else { $latestPassingCycle }
        fullEvidenceBundle = if ($latestFullBundle) { $latestFullBundle } else { $latestPassingBundle }
        handoffSourceBundle = if ($latestFullBundle) { $latestFullBundle } else { $latestPassingBundle }
    }
    counts = [ordered]@{
        cycles = $cycleEntries.Count
        evidenceBundles = $bundleEntries.Count
        regressionRuns = $regressionEntries.Count
        handoffPacks = $handoffEntries.Count
    }
    recent = [ordered]@{
        cycles = @($cycleEntries | Select-Object -First $MaxCycles)
        evidenceBundles = @($bundleEntries | Select-Object -First $MaxEvidenceBundles)
        regressionRuns = @($regressionEntries | Select-Object -First $MaxRegressionRuns)
        handoffPacks = @($handoffEntries | Select-Object -First $MaxHandoffPacks)
    }
}

$index | ConvertTo-Json -Depth 10 | Out-File -FilePath $indexPath -Encoding utf8 -Force

$summaryLines = @(
    ("generatedAt: {0}" -f $index.generatedAt),
    ("cycles: {0}" -f $index.counts.cycles),
    ("evidenceBundles: {0}" -f $index.counts.evidenceBundles),
    ("regressionRuns: {0}" -f $index.counts.regressionRuns),
    ("handoffPacks: {0}" -f $index.counts.handoffPacks),
    ("latestCycle: {0}" -f $latestCyclePath),
    ("latestPassingCycle: {0}" -f $latestPassingCyclePath),
    ("latestStandardCycle: {0}" -f $latestStandardCyclePath),
    ("latestFullEvidenceCycle: {0}" -f $latestFullCyclePath),
    ("latestEvidenceBundle: {0}" -f $latestBundlePath),
    ("latestPassingEvidenceBundle: {0}" -f $latestPassingBundlePath),
    ("latestStandardEvidenceBundle: {0}" -f $latestStandardBundlePath),
    ("latestFullEvidenceBundle: {0}" -f $latestFullBundlePath),
    ("latestRegressionRun: {0}" -f $latestRegressionPath),
    ("latestHandoffPack: {0}" -f $latestHandoffPackPath),
    ("indexPath: {0}" -f $indexPath)
) -join [Environment]::NewLine

$summaryLines | Out-File -FilePath $summaryPath -Encoding utf8 -Force

Write-Output ("ARTIFACT_INDEX={0}" -f $indexPath)
Write-Output ("ARTIFACT_INDEX_SUMMARY={0}" -f $summaryPath)
exit 0
