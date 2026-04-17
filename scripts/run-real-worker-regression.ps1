#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string[]]$Scenarios = @("plane-top", "plane-right", "line2d", "rect2d", "rect3d", "slice4"),
    [string]$ProjectRoot = "",
    [string]$HarnessProject = "C:\SolidWorksMcpValidation\worker-e2e-harness\worker-e2e-harness.csproj",
    [string]$ValidationRoot = "C:\SolidWorksMcpValidation",
    [string]$WorkerExe = "",
    [switch]$SkipPreflight,
    [switch]$KeepGoingOnFailure
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$preflightScript = Join-Path $ProjectRoot "scripts\validate-windows-environment.ps1"
$runManifestDirectory = Join-Path $ValidationRoot "regression-runs"
$runManifestPath = Join-Path $runManifestDirectory ("worker-regression-{0}.json" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
$runStartedAt = Get-Date

function Test-ProcessRunning {
    param([string[]]$Names)

    foreach ($name in $Names) {
        if (Get-Process -Name $name -ErrorAction SilentlyContinue) {
            return $true
        }
    }

    return $false
}

function Read-ScenarioStatus {
    param(
        [string]$Scenario,
        [string]$ValidationRootPath
    )

    $scenarioRoot = Join-Path $ValidationRootPath $Scenario
    $summaryPath = Join-Path $scenarioRoot "$Scenario-summary.txt"
    $inspectionPath = Join-Path $scenarioRoot "$Scenario-inspection.json"
    $summaryLines = @()
    $summaryFailures = @()
    $inspection = $null

    if (Test-Path $summaryPath) {
        $summaryLines = @(Get-Content $summaryPath)
        $summaryFailures = @($summaryLines | Where-Object { $_ -like "FAIL *" })
    }

    if (Test-Path $inspectionPath) {
        try {
            $inspection = Get-Content $inspectionPath -Raw | ConvertFrom-Json
        }
        catch {
            $inspection = $null
        }
    }

    if (-not (Test-Path $summaryPath) -and -not $inspection) {
        return [ordered]@{
            scenario = $Scenario
            passed = $false
            reason = "summary_and_inspection_missing"
            summaryPath = $summaryPath
            inspectionPath = $inspectionPath
            durationMs = $null
            assertionPassCount = 0
            assertionFailCount = 0
            savePath = $null
            exportPath = $null
            strongEvidenceCount = 0
            supportingEvidenceCount = 0
            weakEvidenceCount = 0
            cautionCount = 0
        }
    }

    $inspectionPassed = $null
    if ($inspection -and $null -ne $inspection.scenarioPassed) {
        $inspectionPassed = [bool]$inspection.scenarioPassed
    }

    $passed = if ($null -ne $inspectionPassed) {
        $inspectionPassed
    }
    elseif (Test-Path $summaryPath) {
        $summaryFailures.Count -eq 0
    }
    else {
        $false
    }

    $failedAssertions = @()
    if ($inspection -and $inspection.assertionSummary -and $inspection.assertionSummary.failed) {
        $failedAssertions = @($inspection.assertionSummary.failed | ForEach-Object { [string]$_ })
    }
    elseif ($summaryFailures.Count -gt 0) {
        $failedAssertions = $summaryFailures
    }

    $reason = if ($passed) {
        "ok"
    }
    elseif ($failedAssertions.Count -gt 0) {
        $failedAssertions -join " | "
    }
    elseif (-not (Test-Path $summaryPath)) {
        "summary_missing"
    }
    else {
        "inspection_failed_without_assertion_lines"
    }

    $assertionPassCount = 0
    $assertionFailCount = 0
    if ($inspection -and $inspection.assertionSummary) {
        if ($null -ne $inspection.assertionSummary.passCount) {
            $assertionPassCount = [int]$inspection.assertionSummary.passCount
        }
        if ($null -ne $inspection.assertionSummary.failCount) {
            $assertionFailCount = [int]$inspection.assertionSummary.failCount
        }
    }
    elseif (Test-Path $summaryPath) {
        $assertionPassCount = @($summaryLines | Where-Object { $_ -like "PASS *" }).Count
        $assertionFailCount = $summaryFailures.Count
    }

    $savePath = $null
    $exportPath = $null
    $durationMs = $null
    $strongEvidenceCount = 0
    $supportingEvidenceCount = 0
    $weakEvidenceCount = 0
    $cautionCount = 0

    if ($inspection) {
        if ($inspection.artifacts -and $inspection.artifacts.saveDocument) {
            $savePath = [string]$inspection.artifacts.saveDocument.path
        }
        elseif ($null -ne $inspection.savePath) {
            $savePath = [string]$inspection.savePath
        }

        if ($inspection.artifacts -and $inspection.artifacts.exportFile) {
            $exportPath = [string]$inspection.artifacts.exportFile.path
        }
        elseif ($null -ne $inspection.exportPath) {
            $exportPath = [string]$inspection.exportPath
        }

        if ($null -ne $inspection.durationMs) {
            $durationMs = [int64]$inspection.durationMs
        }

        if ($inspection.evidence) {
            $strongEvidenceCount = @($inspection.evidence.strong).Count
            $supportingEvidenceCount = @($inspection.evidence.supporting).Count
            $weakEvidenceCount = @($inspection.evidence.weak).Count
            $cautionCount = @($inspection.evidence.cautions).Count
        }
    }

    return [ordered]@{
        scenario = $Scenario
        passed = $passed
        reason = $reason
        summaryPath = $summaryPath
        inspectionPath = $inspectionPath
        durationMs = $durationMs
        assertionPassCount = $assertionPassCount
        assertionFailCount = $assertionFailCount
        savePath = $savePath
        exportPath = $exportPath
        strongEvidenceCount = $strongEvidenceCount
        supportingEvidenceCount = $supportingEvidenceCount
        weakEvidenceCount = $weakEvidenceCount
        cautionCount = $cautionCount
    }
}

if (-not (Test-Path $HarnessProject)) {
    throw "Harness project not found: $HarnessProject"
}

if (-not $SkipPreflight) {
    & $preflightScript -ProjectRoot $ProjectRoot -ValidationRoot $ValidationRoot -SkipNodeChecks
}

if ($WorkerExe -and -not (Test-Path $WorkerExe)) {
    throw "Worker executable not found: $WorkerExe"
}

if (Test-ProcessRunning -Names @("SLDWORKS", "SolidWorksWorker")) {
    throw "Refusing to start real regressions while SolidWorks or SolidWorksWorker is already running. Close the active session first."
}

$results = @()
$hasFailures = $false
$originalWorkerOverride = $env:SOLIDWORKS_MCP_VALIDATION_WORKER_EXE

if ($WorkerExe) {
    $env:SOLIDWORKS_MCP_VALIDATION_WORKER_EXE = $WorkerExe
}

try {
    foreach ($scenario in $Scenarios) {
        Write-Host "=== Running scenario: $scenario ==="
        & dotnet run --project $HarnessProject -- $scenario

        $status = Read-ScenarioStatus -Scenario $scenario -ValidationRootPath $ValidationRoot
        $results += $status

        if ($status["passed"]) {
            Write-Host ("PASS {0} | pass={1} fail={2} | inspection={3}" -f $status["scenario"], $status["assertionPassCount"], $status["assertionFailCount"], $status["inspectionPath"])
        }
        else {
            $hasFailures = $true
            Write-Host ("FAIL {0}" -f $status["scenario"])
            Write-Host $status["reason"]

            if (-not $KeepGoingOnFailure) {
                break
            }
        }
    }
}
finally {
    if ($originalWorkerOverride) {
        $env:SOLIDWORKS_MCP_VALIDATION_WORKER_EXE = $originalWorkerOverride
    }
    else {
        Remove-Item Env:SOLIDWORKS_MCP_VALIDATION_WORKER_EXE -ErrorAction SilentlyContinue
    }
}

$completedAt = Get-Date
$durationMs = [int64](($completedAt - $runStartedAt).TotalMilliseconds)
$passedScenarioCount = @($results | Where-Object { $_["passed"] }).Count
$failedScenarioCount = @($results | Where-Object { -not $_["passed"] }).Count

Write-Host ""
Write-Host "=== Real worker regression summary ==="
foreach ($result in $results) {
    Write-Host ("{0} | passed={1} | pass={2} fail={3} | strong={4} supporting={5} weak={6} cautions={7} | summary={8} | inspection={9}" -f `
        $result["scenario"],
        $result["passed"],
        $result["assertionPassCount"],
        $result["assertionFailCount"],
        $result["strongEvidenceCount"],
        $result["supportingEvidenceCount"],
        $result["weakEvidenceCount"],
        $result["cautionCount"],
        $result["summaryPath"],
        $result["inspectionPath"])
}

if (-not (Test-Path $runManifestDirectory)) {
    New-Item -ItemType Directory -Path $runManifestDirectory -Force | Out-Null
}

$runManifest = [ordered]@{
    schemaVersion = "worker-regression-2026-04-10-v2"
    generatedAt = $completedAt.ToString("s")
    runStartedAt = $runStartedAt.ToString("s")
    durationMs = $durationMs
    projectRoot = $ProjectRoot
    harnessProject = $HarnessProject
    validationRoot = $ValidationRoot
    workerExe = if ($WorkerExe) { $WorkerExe } else { "<default-debug-worker>" }
    keepGoingOnFailure = [bool]$KeepGoingOnFailure
    hasFailures = $hasFailures
    scenarioCount = $results.Count
    passedScenarioCount = $passedScenarioCount
    failedScenarioCount = $failedScenarioCount
    scenariosRequested = $Scenarios
    scenarios = $results
}

$runManifest | ConvertTo-Json -Depth 8 | Out-File -FilePath $runManifestPath -Encoding utf8 -Force
Write-Output ("RUN_MANIFEST={0}" -f $runManifestPath)

if ($results.Count -eq 0 -or $hasFailures) {
    exit 1
}

exit 0
