#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string[]]$Scenarios = @("plane-top", "plane-right", "line2d", "rect2d", "rect3d", "slice4"),
    [string]$ProjectRoot = "",
    [string]$ValidationRoot = "C:\SolidWorksMcpValidation",
    [string[]]$ExtraFiles = @(),
    [switch]$IncludeCadFiles
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$bundleRoot = Join-Path $ProjectRoot ("artifacts\real-evidence\{0}" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
$bundleMode = if ($IncludeCadFiles) { "full_with_cad_files" } else { "standard_minimal" }

if (-not (Test-Path $bundleRoot)) {
    New-Item -ItemType Directory -Path $bundleRoot -Force | Out-Null
}

function Get-ArtifactKind {
    param([string]$Extension)

    $normalized = if ($Extension) { $Extension.ToLowerInvariant() } else { "" }
    if ($normalized -in @(".sldprt", ".step", ".stp")) {
        return "cad"
    }

    return "evidence"
}

function New-FileFact {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    $item = Get-Item $DestinationPath
    return [ordered]@{
        name = $item.Name
        sourcePath = $SourcePath
        destinationPath = $DestinationPath
        sizeBytes = $item.Length
        extension = $item.Extension
        kind = (Get-ArtifactKind -Extension $item.Extension)
    }
}

function Read-InspectionSummary {
    param([string]$InspectionPath)

    if (-not (Test-Path $InspectionPath)) {
        return $null
    }

    try {
        $inspection = Get-Content $InspectionPath -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }

    return [ordered]@{
        scenarioPassed = if ($null -ne $inspection.scenarioPassed) { [bool]$inspection.scenarioPassed } else { $null }
        durationMs = if ($null -ne $inspection.durationMs) { [int64]$inspection.durationMs } else { $null }
        assertionPassCount = if ($inspection.assertionSummary -and $null -ne $inspection.assertionSummary.passCount) { [int]$inspection.assertionSummary.passCount } else { 0 }
        assertionFailCount = if ($inspection.assertionSummary -and $null -ne $inspection.assertionSummary.failCount) { [int]$inspection.assertionSummary.failCount } else { 0 }
        strongEvidenceCount = if ($inspection.evidence) { @($inspection.evidence.strong).Count } else { 0 }
        supportingEvidenceCount = if ($inspection.evidence) { @($inspection.evidence.supporting).Count } else { 0 }
        weakEvidenceCount = if ($inspection.evidence) { @($inspection.evidence.weak).Count } else { 0 }
        cautionCount = if ($inspection.evidence) { @($inspection.evidence.cautions).Count } else { 0 }
        savePath = if ($inspection.artifacts -and $inspection.artifacts.saveDocument) { [string]$inspection.artifacts.saveDocument.path } else { $inspection.savePath }
        exportPath = if ($inspection.artifacts -and $inspection.artifacts.exportFile) { [string]$inspection.artifacts.exportFile.path } else { $inspection.exportPath }
    }
}

$scenarioEntries = @()
$copiedScenarioCount = 0
$copiedEvidenceFileCount = 0
$copiedCadFileCount = 0
$totalCopiedSizeBytes = [int64]0
$scenarioPassCount = 0
$scenarioFailCount = 0
$assertionPassCount = 0
$assertionFailCount = 0
$strongEvidenceCount = 0
$supportingEvidenceCount = 0
$weakEvidenceCount = 0
$cautionCount = 0

foreach ($scenario in $Scenarios) {
    $scenarioRoot = Join-Path $ValidationRoot $scenario
    if (-not (Test-Path $scenarioRoot)) {
        $scenarioEntries += [ordered]@{
            scenario = $scenario
            copied = $false
            reason = "scenario_directory_missing"
        }
        continue
    }

    $destination = Join-Path $bundleRoot $scenario
    New-Item -ItemType Directory -Path $destination -Force | Out-Null

    $patterns = @(
        "$scenario-summary.txt",
        "$scenario-inspection.json",
        "$scenario-sequence.ndjson",
        "$scenario-sequence.out.ndjson"
    )

    if ($IncludeCadFiles) {
        $patterns += @("*.SLDPRT", "*.step", "*.stp")
    }

    $copiedFiles = @()
    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $scenarioRoot -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
            $targetPath = Join-Path $destination $_.Name
            Copy-Item -Path $_.FullName -Destination $targetPath -Force
            $copiedFiles += (New-FileFact -SourcePath $_.FullName -DestinationPath $targetPath)
        }
    }

    $inspectionSummary = Read-InspectionSummary -InspectionPath (Join-Path $scenarioRoot "$scenario-inspection.json")
    $copiedScenarioCount += 1

    $scenarioEvidenceFileCount = @($copiedFiles | Where-Object { $_["kind"] -eq "evidence" }).Count
    $scenarioCadFileCount = @($copiedFiles | Where-Object { $_["kind"] -eq "cad" }).Count
    $scenarioCopiedSizeBytes = [int64]0
    foreach ($copiedFile in $copiedFiles) {
        if ($null -ne $copiedFile["sizeBytes"]) {
            $scenarioCopiedSizeBytes += [int64]$copiedFile["sizeBytes"]
        }
    }

    $copiedEvidenceFileCount += $scenarioEvidenceFileCount
    $copiedCadFileCount += $scenarioCadFileCount
    $totalCopiedSizeBytes += $scenarioCopiedSizeBytes

    if ($inspectionSummary) {
        if ($inspectionSummary["scenarioPassed"]) {
            $scenarioPassCount += 1
        }
        else {
            $scenarioFailCount += 1
        }

        $assertionPassCount += [int]$inspectionSummary["assertionPassCount"]
        $assertionFailCount += [int]$inspectionSummary["assertionFailCount"]
        $strongEvidenceCount += [int]$inspectionSummary["strongEvidenceCount"]
        $supportingEvidenceCount += [int]$inspectionSummary["supportingEvidenceCount"]
        $weakEvidenceCount += [int]$inspectionSummary["weakEvidenceCount"]
        $cautionCount += [int]$inspectionSummary["cautionCount"]
    }

    $scenarioEntries += [ordered]@{
        scenario = $scenario
        copied = $true
        bundleMode = $bundleMode
        destination = $destination
        copiedFileCount = $copiedFiles.Count
        copiedEvidenceFileCount = $scenarioEvidenceFileCount
        copiedCadFileCount = $scenarioCadFileCount
        copiedSizeBytes = $scenarioCopiedSizeBytes
        copiedFiles = $copiedFiles
        inspectionSummary = $inspectionSummary
    }
}

$extraFileEntries = @()
$copiedExtraFileCount = 0
$copiedExtraFileSizeBytes = [int64]0
if ($ExtraFiles.Count -gt 0) {
    $extrasRoot = Join-Path $bundleRoot "extras"
    New-Item -ItemType Directory -Path $extrasRoot -Force | Out-Null

    foreach ($extraFile in $ExtraFiles | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique) {
        if (-not (Test-Path $extraFile)) {
            $extraFileEntries += [ordered]@{
                sourcePath = $extraFile
                copied = $false
                reason = "extra_file_missing"
            }
            continue
        }

        $targetPath = Join-Path $extrasRoot (Split-Path -Leaf $extraFile)
        Copy-Item -Path $extraFile -Destination $targetPath -Force
        $extraFact = New-FileFact -SourcePath $extraFile -DestinationPath $targetPath
        $copiedExtraFileCount += 1
        $copiedExtraFileSizeBytes += [int64]$extraFact["sizeBytes"]
        $extraFileEntries += [ordered]@{
            copied = $true
            file = $extraFact
        }
    }
}

$summary = [ordered]@{
    bundleMode = $bundleMode
    includeCadFiles = [bool]$IncludeCadFiles
    scenariosRequestedCount = $Scenarios.Count
    copiedScenarioCount = $copiedScenarioCount
    scenarioPassCount = $scenarioPassCount
    scenarioFailCount = $scenarioFailCount
    copiedEvidenceFileCount = $copiedEvidenceFileCount
    copiedCadFileCount = $copiedCadFileCount
    copiedExtraFileCount = $copiedExtraFileCount
    totalCopiedSizeBytes = ($totalCopiedSizeBytes + $copiedExtraFileSizeBytes)
    assertionPassCount = $assertionPassCount
    assertionFailCount = $assertionFailCount
    strongEvidenceCount = $strongEvidenceCount
    supportingEvidenceCount = $supportingEvidenceCount
    weakEvidenceCount = $weakEvidenceCount
    cautionCount = $cautionCount
}

$manifest = [ordered]@{
    schemaVersion = "evidence-bundle-2026-04-10-v3"
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    validationRoot = $ValidationRoot
    bundleRoot = $bundleRoot
    bundleMode = $bundleMode
    includeCadFiles = [bool]$IncludeCadFiles
    summary = $summary
    scenariosRequested = $Scenarios
    scenarios = $scenarioEntries
    extraFiles = $extraFileEntries
}

$manifestPath = Join-Path $bundleRoot "evidence-manifest.json"
$summaryPath = Join-Path $bundleRoot "evidence-summary.txt"
$manifest | ConvertTo-Json -Depth 10 | Out-File -FilePath $manifestPath -Encoding utf8 -Force

$summaryLines = @(
    ("bundleMode: {0}" -f $bundleMode),
    ("includeCadFiles: {0}" -f ([bool]$IncludeCadFiles)),
    ("scenariosRequestedCount: {0}" -f $summary["scenariosRequestedCount"]),
    ("copiedScenarioCount: {0}" -f $summary["copiedScenarioCount"]),
    ("scenarioPassCount: {0}" -f $summary["scenarioPassCount"]),
    ("scenarioFailCount: {0}" -f $summary["scenarioFailCount"]),
    ("copiedEvidenceFileCount: {0}" -f $summary["copiedEvidenceFileCount"]),
    ("copiedCadFileCount: {0}" -f $summary["copiedCadFileCount"]),
    ("copiedExtraFileCount: {0}" -f $summary["copiedExtraFileCount"]),
    ("totalCopiedSizeBytes: {0}" -f $summary["totalCopiedSizeBytes"]),
    ("assertionPassCount: {0}" -f $summary["assertionPassCount"]),
    ("assertionFailCount: {0}" -f $summary["assertionFailCount"]),
    ("strongEvidenceCount: {0}" -f $summary["strongEvidenceCount"]),
    ("supportingEvidenceCount: {0}" -f $summary["supportingEvidenceCount"]),
    ("weakEvidenceCount: {0}" -f $summary["weakEvidenceCount"]),
    ("cautionCount: {0}" -f $summary["cautionCount"]),
    ("manifestPath: {0}" -f $manifestPath),
    ("bundleRoot: {0}" -f $bundleRoot)
) -join [Environment]::NewLine
$summaryLines | Out-File -FilePath $summaryPath -Encoding utf8 -Force

Write-Output ("EVIDENCE_BUNDLE={0}" -f $bundleRoot)
Write-Output ("EVIDENCE_MANIFEST={0}" -f $manifestPath)
Write-Output ("EVIDENCE_SUMMARY={0}" -f $summaryPath)
exit 0
