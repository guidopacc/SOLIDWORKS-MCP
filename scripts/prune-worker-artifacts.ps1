#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [int]$KeepRecentCycles = 5,
    [int]$KeepRecentEvidenceBundles = 5,
    [int]$KeepRecentHandoffPacks = 5,
    [int]$KeepLatestFailedCycles = 2,
    [int]$KeepLatestFailedEvidenceBundles = 2,
    [switch]$Apply
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
$planPath = Join-Path $indexRoot ("worker-artifact-prune-plan-{0}.json" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
$summaryPath = Join-Path $indexRoot ("worker-artifact-prune-plan-{0}.txt" -f (Get-Date -Format "yyyyMMdd-HHmmss"))

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

    $manifest = Read-JsonFile -Path (Join-Path $Directory.FullName "windows-worker-cycle.json")
    if (-not $manifest) {
        return $null
    }

    return [ordered]@{
        name = $Directory.Name
        path = $Directory.FullName
        bundleMode = (Resolve-BundleMode -Manifest $manifest)
        regressionPassed = if ($manifest.outcome) { [bool]$manifest.outcome.regressionPassed } else { $false }
    }
}

function New-BundleEntry {
    param([System.IO.DirectoryInfo]$Directory)

    $manifest = Read-JsonFile -Path (Join-Path $Directory.FullName "evidence-manifest.json")
    if (-not $manifest) {
        return $null
    }

    return [ordered]@{
        name = $Directory.Name
        path = $Directory.FullName
        bundleMode = (Resolve-BundleMode -Manifest $manifest)
        scenarioFailCount = if ($manifest.summary) { [int]$manifest.summary.scenarioFailCount } else { 0 }
    }
}

function New-HandoffEntry {
    param([System.IO.DirectoryInfo]$Directory)

    $manifest = Read-JsonFile -Path (Join-Path $Directory.FullName "handoff-manifest.json")
    if (-not $manifest) {
        return $null
    }

    return [ordered]@{
        name = $Directory.Name
        path = $Directory.FullName
    }
}

function Get-Entries {
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

function Add-ProtectedPath {
    param(
        [hashtable]$Set,
        [string]$Path,
        [string]$Reason
    )

    if (-not $Path) {
        return
    }

    if (-not $Set.ContainsKey($Path)) {
        $Set[$Path] = @()
    }

    $Set[$Path] += $Reason
}

$cycleEntries = Get-Entries -Root $cyclesRoot -Factory ${function:New-CycleEntry}
$bundleEntries = Get-Entries -Root $evidenceRoot -Factory ${function:New-BundleEntry}
$handoffEntries = Get-Entries -Root $handoffRoot -Factory ${function:New-HandoffEntry}

$protectedCycles = @{}
$protectedBundles = @{}
$protectedHandoffPacks = @{}

foreach ($entry in ($cycleEntries | Select-Object -First $KeepRecentCycles)) {
    Add-ProtectedPath -Set $protectedCycles -Path $entry.path -Reason "keep_recent_cycle"
}
foreach ($entry in ($cycleEntries | Where-Object { $_["regressionPassed"] } | Select-Object -First 1)) {
    Add-ProtectedPath -Set $protectedCycles -Path $entry.path -Reason "latest_passing_cycle"
}
foreach ($entry in ($cycleEntries | Where-Object { $_["bundleMode"] -eq "standard_minimal" } | Select-Object -First 1)) {
    Add-ProtectedPath -Set $protectedCycles -Path $entry.path -Reason "latest_standard_cycle"
}
foreach ($entry in ($cycleEntries | Where-Object { $_["bundleMode"] -eq "full_with_cad_files" } | Select-Object -First 1)) {
    Add-ProtectedPath -Set $protectedCycles -Path $entry.path -Reason "latest_full_cycle"
}
foreach ($entry in ($cycleEntries | Where-Object { -not $_["regressionPassed"] } | Select-Object -First $KeepLatestFailedCycles)) {
    Add-ProtectedPath -Set $protectedCycles -Path $entry.path -Reason "keep_latest_failed_cycle"
}

foreach ($entry in ($bundleEntries | Select-Object -First $KeepRecentEvidenceBundles)) {
    Add-ProtectedPath -Set $protectedBundles -Path $entry.path -Reason "keep_recent_bundle"
}
foreach ($entry in ($bundleEntries | Where-Object { $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1)) {
    Add-ProtectedPath -Set $protectedBundles -Path $entry.path -Reason "latest_passing_bundle"
}
foreach ($entry in ($bundleEntries | Where-Object { $_["bundleMode"] -eq "standard_minimal" -and $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1)) {
    Add-ProtectedPath -Set $protectedBundles -Path $entry.path -Reason "latest_standard_bundle"
}
foreach ($entry in ($bundleEntries | Where-Object { $_["bundleMode"] -eq "full_with_cad_files" -and $_["scenarioFailCount"] -eq 0 } | Select-Object -First 1)) {
    Add-ProtectedPath -Set $protectedBundles -Path $entry.path -Reason "latest_full_bundle"
}
foreach ($entry in ($bundleEntries | Where-Object { $_["scenarioFailCount"] -gt 0 } | Select-Object -First $KeepLatestFailedEvidenceBundles)) {
    Add-ProtectedPath -Set $protectedBundles -Path $entry.path -Reason "keep_latest_failed_bundle"
}

foreach ($entry in ($handoffEntries | Select-Object -First $KeepRecentHandoffPacks)) {
    Add-ProtectedPath -Set $protectedHandoffPacks -Path $entry.path -Reason "keep_recent_handoff_pack"
}

$cycleCandidates = @()
foreach ($entry in $cycleEntries) {
    if (-not $protectedCycles.ContainsKey($entry.path)) {
        $cycleCandidates += [ordered]@{
            category = "cycle"
            path = $entry.path
            name = $entry.name
            reasons = @()
            status = "planned"
        }
    }
}

$bundleCandidates = @()
foreach ($entry in $bundleEntries) {
    if (-not $protectedBundles.ContainsKey($entry.path)) {
        $bundleCandidates += [ordered]@{
            category = "evidence_bundle"
            path = $entry.path
            name = $entry.name
            reasons = @()
            status = "planned"
        }
    }
}

$handoffCandidates = @()
foreach ($entry in $handoffEntries) {
    if (-not $protectedHandoffPacks.ContainsKey($entry.path)) {
        $handoffCandidates += [ordered]@{
            category = "handoff_pack"
            path = $entry.path
            name = $entry.name
            reasons = @()
            status = "planned"
        }
    }
}

$allCandidates = @($cycleCandidates + $bundleCandidates + $handoffCandidates)
$deletedCount = 0

if ($Apply) {
    foreach ($candidate in $allCandidates) {
        if (Test-Path $candidate.path) {
            Remove-Item -LiteralPath $candidate.path -Recurse -Force
            $candidate["status"] = "deleted"
            $deletedCount += 1
        }
        else {
            $candidate["status"] = "missing"
        }
    }
}

$plan = [ordered]@{
    schemaVersion = "worker-artifact-prune-plan-2026-04-10-v1"
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    applyRequested = [bool]$Apply
    safeMode = [bool](-not $Apply)
    roots = [ordered]@{
        cyclesRoot = $cyclesRoot
        evidenceRoot = $evidenceRoot
        handoffRoot = $handoffRoot
    }
    policy = [ordered]@{
        keepRecentCycles = $KeepRecentCycles
        keepRecentEvidenceBundles = $KeepRecentEvidenceBundles
        keepRecentHandoffPacks = $KeepRecentHandoffPacks
        keepLatestFailedCycles = $KeepLatestFailedCycles
        keepLatestFailedEvidenceBundles = $KeepLatestFailedEvidenceBundles
        defaultMode = "dry_run"
        explicitNonGoals = @(
            "do_not_prune_validation_root_raw_scenarios",
            "do_not_prune_regression_run_manifests",
            "do_not_prune_build_publish_manifests"
        )
    }
    protected = [ordered]@{
        cycles = @(
            foreach ($key in $protectedCycles.Keys | Sort-Object) {
                [ordered]@{ path = $key; reasons = $protectedCycles[$key] }
            }
        )
        evidenceBundles = @(
            foreach ($key in $protectedBundles.Keys | Sort-Object) {
                [ordered]@{ path = $key; reasons = $protectedBundles[$key] }
            }
        )
        handoffPacks = @(
            foreach ($key in $protectedHandoffPacks.Keys | Sort-Object) {
                [ordered]@{ path = $key; reasons = $protectedHandoffPacks[$key] }
            }
        )
    }
    candidates = $allCandidates
    summary = [ordered]@{
        cycleCandidates = $cycleCandidates.Count
        evidenceBundleCandidates = $bundleCandidates.Count
        handoffPackCandidates = $handoffCandidates.Count
        totalCandidates = $allCandidates.Count
        deletedCount = $deletedCount
    }
}

$plan | ConvertTo-Json -Depth 10 | Out-File -FilePath $planPath -Encoding utf8 -Force

$summaryLines = @(
    ("applyRequested: {0}" -f ([bool]$Apply)),
    ("safeMode: {0}" -f ([bool](-not $Apply))),
    ("keepRecentCycles: {0}" -f $KeepRecentCycles),
    ("keepRecentEvidenceBundles: {0}" -f $KeepRecentEvidenceBundles),
    ("keepRecentHandoffPacks: {0}" -f $KeepRecentHandoffPacks),
    ("keepLatestFailedCycles: {0}" -f $KeepLatestFailedCycles),
    ("keepLatestFailedEvidenceBundles: {0}" -f $KeepLatestFailedEvidenceBundles),
    ("cycleCandidates: {0}" -f $plan.summary.cycleCandidates),
    ("evidenceBundleCandidates: {0}" -f $plan.summary.evidenceBundleCandidates),
    ("handoffPackCandidates: {0}" -f $plan.summary.handoffPackCandidates),
    ("totalCandidates: {0}" -f $plan.summary.totalCandidates),
    ("deletedCount: {0}" -f $plan.summary.deletedCount),
    ("planPath: {0}" -f $planPath)
) -join [Environment]::NewLine

$summaryLines | Out-File -FilePath $summaryPath -Encoding utf8 -Force

Write-Output ("PRUNE_PLAN={0}" -f $planPath)
Write-Output ("PRUNE_SUMMARY={0}" -f $summaryPath)
exit 0
