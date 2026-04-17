#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [string]$EvidenceBundlePath = "",
    [string]$CycleManifestPath = "",
    [string]$PackLabel = ""
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$evidenceRoot = Join-Path $ProjectRoot "artifacts\real-evidence"
$cyclesRoot = Join-Path $ProjectRoot "artifacts\worker\cycles"
$handoffRoot = Join-Path $ProjectRoot "artifacts\handoff-packs"

if (-not (Test-Path $handoffRoot)) {
    New-Item -ItemType Directory -Path $handoffRoot -Force | Out-Null
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

function Get-LatestBundleDirectory {
    param([string]$Root)

    if (-not (Test-Path $Root)) {
        return $null
    }

    $entries = @()
    foreach ($directory in Get-ChildItem -Path $Root -Directory | Sort-Object Name -Descending) {
        $manifestPath = Join-Path $directory.FullName "evidence-manifest.json"
        $manifest = Read-JsonFile -Path $manifestPath
        if (-not $manifest) {
            continue
        }

        $entries += [ordered]@{
            path = $directory.FullName
            manifestPath = $manifestPath
            bundleMode = (Resolve-BundleMode -Manifest $manifest)
            includeCadFiles = [bool]$manifest.includeCadFiles
            scenarioFailCount = if ($manifest.summary) { [int]$manifest.summary.scenarioFailCount } else { 0 }
        }
    }

    $preferred = $entries | Where-Object { $_["bundleMode"] -eq "full_with_cad_files" -and $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1
    if ($preferred) {
        return $preferred
    }

    $fallback = $entries | Where-Object { $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1
    if ($fallback) {
        return $fallback
    }

    return $entries | Select-Object -First 1
}

function Get-RelatedCycleManifest {
    param(
        [string]$CyclesRootPath,
        [string]$BundlePath,
        [string]$BundleManifestPath
    )

    if (-not (Test-Path $CyclesRootPath)) {
        return $null
    }

    foreach ($directory in Get-ChildItem -Path $CyclesRootPath -Directory | Sort-Object Name -Descending) {
        $manifestPathLocal = Join-Path $directory.FullName "windows-worker-cycle.json"
        $manifest = Read-JsonFile -Path $manifestPathLocal
        if (-not $manifest) {
            continue
        }

        $matchesBundlePath = $false
        $matchesManifestPath = $false
        if ($manifest.evidenceBundle) {
            if ($manifest.evidenceBundle.bundlePath -eq $BundlePath) {
                $matchesBundlePath = $true
            }
            if ($manifest.evidenceBundle.manifestPath -eq $BundleManifestPath) {
                $matchesManifestPath = $true
            }
        }

        if ($matchesBundlePath -or $matchesManifestPath) {
            return $manifestPathLocal
        }
    }

    return $null
}

function Copy-IfExists {
    param(
        [string]$SourcePath,
        [string]$DestinationDirectory
    )

    if (-not $SourcePath -or -not (Test-Path $SourcePath)) {
        return $null
    }

    if (-not (Test-Path $DestinationDirectory)) {
        New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    }

    $destinationPath = Join-Path $DestinationDirectory (Split-Path -Leaf $SourcePath)
    Copy-Item -Path $SourcePath -Destination $destinationPath -Force
    return $destinationPath
}

if (-not $EvidenceBundlePath) {
    $latestBundle = Get-LatestBundleDirectory -Root $evidenceRoot
    if (-not $latestBundle) {
        throw "No evidence bundle found under $evidenceRoot"
    }

    $EvidenceBundlePath = $latestBundle.path
}

$EvidenceBundlePath = (Resolve-Path $EvidenceBundlePath).Path
$bundleManifestPath = Join-Path $EvidenceBundlePath "evidence-manifest.json"
$bundleSummaryPath = Join-Path $EvidenceBundlePath "evidence-summary.txt"
$bundleManifest = Read-JsonFile -Path $bundleManifestPath
if (-not $bundleManifest) {
    throw "Evidence bundle manifest not found or unreadable: $bundleManifestPath"
}

if (-not $CycleManifestPath) {
    $CycleManifestPath = Get-RelatedCycleManifest -CyclesRootPath $cyclesRoot -BundlePath $EvidenceBundlePath -BundleManifestPath $bundleManifestPath
}

$cycleManifest = Read-JsonFile -Path $CycleManifestPath
$cycleSummaryPath = if ($CycleManifestPath) { Join-Path (Split-Path -Parent $CycleManifestPath) "windows-worker-cycle-summary.txt" } else { $null }
$regressionManifestPath = $null
if ($cycleManifest -and $cycleManifest.regression) {
    $regressionManifestPath = $cycleManifest.regression.runManifestPath
}

$packName = Get-Date -Format "yyyyMMdd-HHmmss"
if ($PackLabel) {
    $sanitizedLabel = ($PackLabel -replace '[^A-Za-z0-9._-]', '-')
    if ($sanitizedLabel) {
        $packName = "{0}-{1}" -f $packName, $sanitizedLabel
    }
}

$packRoot = Join-Path $handoffRoot $packName
$packManifestPath = Join-Path $packRoot "handoff-manifest.json"
$packSummaryPath = Join-Path $packRoot "handoff-summary.txt"
$packCycleRoot = Join-Path $packRoot "cycle"
$packEvidenceRoot = Join-Path $packRoot "evidence"
$packScenariosRoot = Join-Path $packRoot "scenarios"
$packExtrasRoot = Join-Path $packRoot "extras"

New-Item -ItemType Directory -Path $packRoot -Force | Out-Null

$copiedCycleFiles = @()
$copiedEvidenceFiles = @()
$copiedScenarioEntries = @()
$copiedExtraFiles = @()

$copiedEvidenceFiles += @(
    Copy-IfExists -SourcePath $bundleManifestPath -DestinationDirectory $packEvidenceRoot
    Copy-IfExists -SourcePath $bundleSummaryPath -DestinationDirectory $packEvidenceRoot
) | Where-Object { $_ }

if ($CycleManifestPath) {
    $copiedCycleFiles += @(
        Copy-IfExists -SourcePath $CycleManifestPath -DestinationDirectory $packCycleRoot
        Copy-IfExists -SourcePath $cycleSummaryPath -DestinationDirectory $packCycleRoot
        Copy-IfExists -SourcePath $regressionManifestPath -DestinationDirectory $packCycleRoot
    ) | Where-Object { $_ }
}

foreach ($scenario in $bundleManifest.scenarios) {
    if (-not $scenario.destination -or -not (Test-Path $scenario.destination)) {
        continue
    }

    $scenarioDestinationRoot = Join-Path $packScenariosRoot $scenario.scenario
    New-Item -ItemType Directory -Path $scenarioDestinationRoot -Force | Out-Null

    $copiedScenarioFiles = @()
    foreach ($file in Get-ChildItem -Path $scenario.destination -File | Sort-Object Name) {
        $copiedScenarioFiles += Copy-IfExists -SourcePath $file.FullName -DestinationDirectory $scenarioDestinationRoot
    }

    $copiedScenarioEntries += [ordered]@{
        scenario = $scenario.scenario
        destination = $scenarioDestinationRoot
        copiedFileCount = $copiedScenarioFiles.Count
        copiedFiles = $copiedScenarioFiles
    }
}

$sourceExtrasRoot = Join-Path $EvidenceBundlePath "extras"
if (Test-Path $sourceExtrasRoot) {
    foreach ($extraFile in Get-ChildItem -Path $sourceExtrasRoot -File | Sort-Object Name) {
        $copiedExtraFiles += Copy-IfExists -SourcePath $extraFile.FullName -DestinationDirectory $packExtrasRoot
    }
}

$packManifest = [ordered]@{
    schemaVersion = "handoff-evidence-pack-2026-04-10-v1"
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    handoffPackPath = $packRoot
    sourceEvidenceBundlePath = $EvidenceBundlePath
    sourceEvidenceManifestPath = $bundleManifestPath
    sourceBundleMode = (Resolve-BundleMode -Manifest $bundleManifest)
    includeCadFiles = [bool]$bundleManifest.includeCadFiles
    sourceCycleManifestPath = $CycleManifestPath
    sourceRegressionManifestPath = $regressionManifestPath
    scenarioCount = @($bundleManifest.scenarios).Count
    copiedScenarioCount = $copiedScenarioEntries.Count
    copiedCycleFileCount = $copiedCycleFiles.Count
    copiedEvidenceFileCount = $copiedEvidenceFiles.Count
    copiedExtraFileCount = $copiedExtraFiles.Count
    contents = [ordered]@{
        cycleFiles = $copiedCycleFiles
        evidenceFiles = $copiedEvidenceFiles
        scenarioEntries = $copiedScenarioEntries
        extraFiles = $copiedExtraFiles
    }
    recommendedReviewOrder = @(
        "handoff-summary.txt",
        "cycle\\windows-worker-cycle-summary.txt",
        "cycle\\worker-regression-*.json",
        "evidence\\evidence-summary.txt",
        "evidence\\evidence-manifest.json",
        "scenarios\\<scenario>\\<scenario>-summary.txt",
        "scenarios\\<scenario>\\<scenario>-inspection.json"
    )
}

$packManifest | ConvertTo-Json -Depth 10 | Out-File -FilePath $packManifestPath -Encoding utf8 -Force

$summaryLines = @(
    ("sourceEvidenceBundlePath: {0}" -f $EvidenceBundlePath),
    ("sourceBundleMode: {0}" -f (Resolve-BundleMode -Manifest $bundleManifest)),
    ("includeCadFiles: {0}" -f ([bool]$bundleManifest.includeCadFiles)),
    ("sourceCycleManifestPath: {0}" -f $CycleManifestPath),
    ("sourceRegressionManifestPath: {0}" -f $regressionManifestPath),
    ("scenarioCount: {0}" -f $packManifest.scenarioCount),
    ("copiedScenarioCount: {0}" -f $packManifest.copiedScenarioCount),
    ("copiedCycleFileCount: {0}" -f $packManifest.copiedCycleFileCount),
    ("copiedEvidenceFileCount: {0}" -f $packManifest.copiedEvidenceFileCount),
    ("copiedExtraFileCount: {0}" -f $packManifest.copiedExtraFileCount),
    ("handoffPackPath: {0}" -f $packRoot),
    ("handoffManifestPath: {0}" -f $packManifestPath)
) -join [Environment]::NewLine

$summaryLines | Out-File -FilePath $packSummaryPath -Encoding utf8 -Force

Write-Output ("HANDOFF_PACK={0}" -f $packRoot)
Write-Output ("HANDOFF_MANIFEST={0}" -f $packManifestPath)
Write-Output ("HANDOFF_SUMMARY={0}" -f $packSummaryPath)
exit 0
