#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [string]$ValidationRoot = "C:\SolidWorksMcpValidation",
    [string]$HarnessProject = "C:\SolidWorksMcpValidation\worker-e2e-harness\worker-e2e-harness.csproj",
    [string[]]$Scenarios = @("plane-top", "plane-right", "line2d", "rect2d", "rect3d", "slice4"),
    [switch]$IncludeCadFiles,
    [switch]$SkipBuildDebug,
    [switch]$SkipEvidenceBundle
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$preflightScript = Join-Path $ProjectRoot "scripts\validate-windows-environment.ps1"
$buildScript = Join-Path $ProjectRoot "scripts\build-solidworks-worker.ps1"
$publishScript = Join-Path $ProjectRoot "scripts\publish-solidworks-worker.ps1"
$runScript = Join-Path $ProjectRoot "scripts\run-solidworks-worker.ps1"
$regressionScript = Join-Path $ProjectRoot "scripts\run-real-worker-regression.ps1"
$evidenceScript = Join-Path $ProjectRoot "scripts\collect-real-worker-evidence.ps1"
$preflightJsonPath = Join-Path $ValidationRoot "preflight\worker-preflight.json"
$cycleRoot = Join-Path $ProjectRoot ("artifacts\worker\cycles\{0}" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
$cycleManifestPath = Join-Path $cycleRoot "windows-worker-cycle.json"
$cycleSummaryPath = Join-Path $cycleRoot "windows-worker-cycle-summary.txt"
$smokeInputPath = Join-Path $cycleRoot "worker-smoke.ndjson"
$smokeOutputPath = Join-Path $cycleRoot "worker-smoke.out.ndjson"
$regressionStdoutPath = Join-Path $cycleRoot "worker-regression.stdout.txt"
$evidenceStdoutPath = Join-Path $cycleRoot "worker-evidence.stdout.txt"
$bundleMode = if ($IncludeCadFiles) { "full_with_cad_files" } else { "standard_minimal" }

if (-not (Test-Path $cycleRoot)) {
    New-Item -ItemType Directory -Path $cycleRoot -Force | Out-Null
}

function Get-NamedOutputValue {
    param(
        [string[]]$Lines,
        [string]$Name
    )

    $match = $Lines | Where-Object { $_ -like "$Name=*" } | Select-Object -Last 1
    if (-not $match) {
        return $null
    }

    return $match.Substring($Name.Length + 1)
}

function Read-JsonDocument {
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

Write-Host "=== Preflight ==="
& $preflightScript -ProjectRoot $ProjectRoot -ValidationRoot $ValidationRoot -EmitJsonPath $preflightJsonPath

$buildArtifactPath = $null
$debugWorkerExe = $null
if (-not $SkipBuildDebug) {
    Write-Host ""
    Write-Host "=== Build debug worker ==="
    $buildOutput = & $buildScript -ProjectRoot $ProjectRoot -Configuration Debug -SkipPreflight
    if ($LASTEXITCODE -ne 0) {
        throw "Debug worker build failed."
    }

    $buildArtifactPath = Get-NamedOutputValue -Lines $buildOutput -Name "BUILD_ARTIFACT"
    $debugWorkerExe = Get-NamedOutputValue -Lines $buildOutput -Name "WORKER_EXE"
}

Write-Host ""
Write-Host "=== Publish release worker ==="
$publishOutput = & $publishScript -ProjectRoot $ProjectRoot -Configuration Release -SkipPreflight
if ($LASTEXITCODE -ne 0) {
    throw "Release worker publish failed."
}

$publishArtifactPath = Get-NamedOutputValue -Lines $publishOutput -Name "PUBLISH_ARTIFACT"
$publishedWorkerExe = Get-NamedOutputValue -Lines $publishOutput -Name "WORKER_PUBLISH_EXE"
if (-not $publishedWorkerExe) {
    throw "Publish step did not report WORKER_PUBLISH_EXE."
}

Write-Host ""
Write-Host "=== Smoke check ==="
$smokeMessages = @(
    '{"messageType":"handshake_request","requestId":"handshake-1","protocolVersion":"0.1.0","client":{"name":"windows-worker-cycle","version":"0.1.0"}}',
    '{"messageType":"shutdown_request","requestId":"shutdown-1","protocolVersion":"0.1.0"}'
)
$smokeMessages | Out-File -FilePath $smokeInputPath -Encoding utf8 -Force
$smokeOutput = & $runScript -ProjectRoot $ProjectRoot -Source publish -Configuration Release -InputLine $smokeMessages
$smokeOutput | Out-File -FilePath $smokeOutputPath -Encoding utf8 -Force
if ($LASTEXITCODE -ne 0) {
    throw "Published worker smoke check failed."
}

$handshakeSeen = @($smokeOutput | Where-Object { $_ -like '*"messageType":"handshake_response"*' }).Count -gt 0
$shutdownSeen = @($smokeOutput | Where-Object { $_ -like '*"messageType":"shutdown_response"*' }).Count -gt 0
if (-not ($handshakeSeen -and $shutdownSeen)) {
    throw "Published worker smoke output did not contain both handshake_response and shutdown_response."
}

Write-Host ""
Write-Host "=== Real regression ==="
$regressionOutput = & $regressionScript `
    -ProjectRoot $ProjectRoot `
    -ValidationRoot $ValidationRoot `
    -HarnessProject $HarnessProject `
    -WorkerExe $publishedWorkerExe `
    -Scenarios $Scenarios `
    -SkipPreflight
$regressionOutput | Out-File -FilePath $regressionStdoutPath -Encoding utf8 -Force
if ($LASTEXITCODE -ne 0) {
    throw "Serialized real regression failed."
}

$runManifestPath = Get-NamedOutputValue -Lines $regressionOutput -Name "RUN_MANIFEST"
$regressionManifest = Read-JsonDocument -Path $runManifestPath

$evidenceBundlePath = $null
$evidenceManifestPath = $null
$evidenceSummaryPath = $null
if (-not $SkipEvidenceBundle) {
    Write-Host ""
    Write-Host "=== Evidence bundle ==="
    $extraFiles = @(
        $preflightJsonPath,
        $buildArtifactPath,
        $publishArtifactPath,
        $smokeInputPath,
        $smokeOutputPath,
        $regressionStdoutPath,
        $runManifestPath
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    $evidenceOutput = & $evidenceScript `
        -ProjectRoot $ProjectRoot `
        -ValidationRoot $ValidationRoot `
        -Scenarios $Scenarios `
        -ExtraFiles $extraFiles `
        -IncludeCadFiles:$IncludeCadFiles
    $evidenceOutput | Out-File -FilePath $evidenceStdoutPath -Encoding utf8 -Force
    if ($LASTEXITCODE -ne 0) {
        throw "Evidence bundle collection failed."
    }

    $evidenceBundlePath = Get-NamedOutputValue -Lines $evidenceOutput -Name "EVIDENCE_BUNDLE"
    $evidenceManifestPath = Get-NamedOutputValue -Lines $evidenceOutput -Name "EVIDENCE_MANIFEST"
    $evidenceSummaryPath = Get-NamedOutputValue -Lines $evidenceOutput -Name "EVIDENCE_SUMMARY"
}

$evidenceManifest = Read-JsonDocument -Path $evidenceManifestPath
$regressionPassed = $null
$passedScenarioCount = $null
$failedScenarioCount = $null
if ($regressionManifest) {
    if ($null -ne $regressionManifest.hasFailures) {
        $regressionPassed = -not [bool]$regressionManifest.hasFailures
    }

    if ($null -ne $regressionManifest.passedScenarioCount) {
        $passedScenarioCount = [int]$regressionManifest.passedScenarioCount
    }

    if ($null -ne $regressionManifest.failedScenarioCount) {
        $failedScenarioCount = [int]$regressionManifest.failedScenarioCount
    }
}

$evidenceSummary = $null
if ($evidenceManifest -and $evidenceManifest.summary) {
    $evidenceSummary = $evidenceManifest.summary
}

$cycleManifest = [ordered]@{
    schemaVersion = "windows-worker-cycle-2026-04-10-v2"
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    validationRoot = $ValidationRoot
    harnessProject = $HarnessProject
    cycleRoot = $cycleRoot
    scenarios = $Scenarios
    bundleMode = $bundleMode
    includeCadFiles = [bool]$IncludeCadFiles
    outcome = [ordered]@{
        smokePassed = ($handshakeSeen -and $shutdownSeen)
        regressionPassed = $regressionPassed
        evidenceBundleCollected = [bool](-not $SkipEvidenceBundle)
        passedScenarioCount = $passedScenarioCount
        failedScenarioCount = $failedScenarioCount
    }
    debugBuild = [ordered]@{
        skipped = [bool]$SkipBuildDebug
        workerExe = $debugWorkerExe
        artifactPath = $buildArtifactPath
    }
    publish = [ordered]@{
        workerExe = $publishedWorkerExe
        artifactPath = $publishArtifactPath
    }
    smoke = [ordered]@{
        inputPath = $smokeInputPath
        outputPath = $smokeOutputPath
        handshakeSeen = $handshakeSeen
        shutdownSeen = $shutdownSeen
    }
    regression = [ordered]@{
        stdoutPath = $regressionStdoutPath
        runManifestPath = $runManifestPath
        passedScenarioCount = $passedScenarioCount
        failedScenarioCount = $failedScenarioCount
        durationMs = if ($regressionManifest) { $regressionManifest.durationMs } else { $null }
    }
    evidenceBundle = [ordered]@{
        skipped = [bool]$SkipEvidenceBundle
        mode = $bundleMode
        stdoutPath = if ($SkipEvidenceBundle) { $null } else { $evidenceStdoutPath }
        bundlePath = $evidenceBundlePath
        manifestPath = $evidenceManifestPath
        summaryPath = $evidenceSummaryPath
        copiedScenarioCount = if ($evidenceSummary) { $evidenceSummary.copiedScenarioCount } else { $null }
        copiedEvidenceFileCount = if ($evidenceSummary) { $evidenceSummary.copiedEvidenceFileCount } else { $null }
        copiedCadFileCount = if ($evidenceSummary) { $evidenceSummary.copiedCadFileCount } else { $null }
        copiedExtraFileCount = if ($evidenceSummary) { $evidenceSummary.copiedExtraFileCount } else { $null }
    }
    preflightJsonPath = $preflightJsonPath
}

$cycleManifest | ConvertTo-Json -Depth 10 | Out-File -FilePath $cycleManifestPath -Encoding utf8 -Force

$cycleSummaryLines = @(
    ("bundleMode: {0}" -f $bundleMode),
    ("includeCadFiles: {0}" -f ([bool]$IncludeCadFiles)),
    ("publishedWorkerExe: {0}" -f $publishedWorkerExe),
    ("smokePassed: {0}" -f ($handshakeSeen -and $shutdownSeen)),
    ("regressionPassed: {0}" -f $regressionPassed),
    ("passedScenarioCount: {0}" -f $passedScenarioCount),
    ("failedScenarioCount: {0}" -f $failedScenarioCount),
    ("regressionRunManifest: {0}" -f $runManifestPath),
    ("evidenceBundlePath: {0}" -f $evidenceBundlePath),
    ("evidenceManifestPath: {0}" -f $evidenceManifestPath),
    ("evidenceSummaryPath: {0}" -f $evidenceSummaryPath),
    ("cycleManifestPath: {0}" -f $cycleManifestPath)
)

if ($evidenceSummary) {
    $cycleSummaryLines += @(
        ("copiedScenarioCount: {0}" -f $evidenceSummary.copiedScenarioCount),
        ("copiedEvidenceFileCount: {0}" -f $evidenceSummary.copiedEvidenceFileCount),
        ("copiedCadFileCount: {0}" -f $evidenceSummary.copiedCadFileCount),
        ("copiedExtraFileCount: {0}" -f $evidenceSummary.copiedExtraFileCount),
        ("assertionPassCount: {0}" -f $evidenceSummary.assertionPassCount),
        ("assertionFailCount: {0}" -f $evidenceSummary.assertionFailCount),
        ("strongEvidenceCount: {0}" -f $evidenceSummary.strongEvidenceCount),
        ("supportingEvidenceCount: {0}" -f $evidenceSummary.supportingEvidenceCount),
        ("weakEvidenceCount: {0}" -f $evidenceSummary.weakEvidenceCount),
        ("cautionCount: {0}" -f $evidenceSummary.cautionCount)
    )
}

($cycleSummaryLines -join [Environment]::NewLine) | Out-File -FilePath $cycleSummaryPath -Encoding utf8 -Force

Write-Host ""
Write-Output ("CYCLE_MANIFEST={0}" -f $cycleManifestPath)
Write-Output ("CYCLE_SUMMARY={0}" -f $cycleSummaryPath)
exit 0
