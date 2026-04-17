#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$ProjectRoot = "",
    [string]$OutputRoot = "",
    [string]$HandoffLabel = "",
    [string]$PublishedWorkerPath = "",
    [string]$EvidencePackPath = "",
    [switch]$IncludeEvidencePack
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = (Resolve-Path $ProjectRoot).Path

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $ProjectRoot "artifacts\alpha-handoffs"
}

if (-not (Test-Path $OutputRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Copy-RelativeFile {
    param(
        [string]$ProjectRootPath,
        [string]$RelativePath,
        [string]$DestinationRoot
    )

    $sourcePath = Join-Path $ProjectRootPath $RelativePath
    if (-not (Test-Path $sourcePath)) {
        return $null
    }

    $destinationPath = Join-Path $DestinationRoot $RelativePath
    $destinationDirectory = Split-Path -Parent $destinationPath
    Ensure-Directory -Path $destinationDirectory
    Copy-Item -Path $sourcePath -Destination $destinationPath -Force

    return [ordered]@{
        relativePath = $RelativePath
        sourcePath = (Resolve-Path $sourcePath).Path
        destinationPath = $destinationPath
    }
}

function Copy-FileFlat {
    param(
        [string]$SourcePath,
        [string]$DestinationDirectory
    )

    if (-not $SourcePath -or -not (Test-Path $SourcePath)) {
        return $null
    }

    Ensure-Directory -Path $DestinationDirectory

    $resolvedSourcePath = (Resolve-Path $SourcePath).Path
    $destinationPath = Join-Path $DestinationDirectory (Split-Path -Leaf $resolvedSourcePath)
    Copy-Item -Path $resolvedSourcePath -Destination $destinationPath -Force

    return [ordered]@{
        sourcePath = $resolvedSourcePath
        destinationPath = $destinationPath
    }
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

function Resolve-LatestEvidencePack {
    param([string]$RootPath)

    if (-not (Test-Path $RootPath)) {
        return $null
    }

    foreach ($directory in Get-ChildItem -Path $RootPath -Directory | Sort-Object Name -Descending) {
        $manifestPath = Join-Path $directory.FullName "handoff-manifest.json"
        if (Test-Path $manifestPath) {
            return $directory.FullName
        }
    }

    return $null
}

$coreRelativePaths = @(
    "README.md",
    "PROJECT_HANDOFF.md",
    "LICENSE",
    "docs\getting-started\QUICKSTART_ALPHA.md",
    "docs\architecture\ALPHA_DISTRIBUTION_PLAN.md",
    "docs\architecture\ALPHA_DELIVERY_MODEL.md",
    "docs\architecture\ALPHA_PACKAGE_CONTENTS.md",
    "docs\architecture\FIRST_ALPHA_HANDOFF_GUIDE.md",
    "docs\architecture\FIRST_HANDOFF_GUIDE.md",
    "docs\architecture\FIRST_PROMPT_MCP.md",
    "docs\architecture\FIRST_RESOURCE_MCP.md",
    "docs\architecture\SECOND_RESOURCE_MCP.md",
    "docs\architecture\MCP_CLIENT_SETUP.md",
    "docs\architecture\PUBLIC_ALPHA_BOUNDARY.md",
    "docs\architecture\SECOND_MACHINE_VALIDATION.md",
    "docs\architecture\SECOND_MACHINE_VALIDATION_REPORT.md",
    "docs\architecture\WINDOWS_EXECUTION_RUNBOOK.md"
)

$packName = Get-Date -Format "yyyyMMdd-HHmmss"
if ($HandoffLabel) {
    $sanitizedLabel = ($HandoffLabel -replace '[^A-Za-z0-9._-]', '-')
    if ($sanitizedLabel) {
        $packName = "{0}-{1}" -f $packName, $sanitizedLabel
    }
}

$packRoot = Join-Path $OutputRoot $packName
$docsRoot = $packRoot
$workerRoot = Join-Path $packRoot "worker"
$evidenceRoot = Join-Path $packRoot "evidence-pack"
$manifestPath = Join-Path $packRoot "alpha-handoff-manifest.json"
$summaryPath = Join-Path $packRoot "alpha-handoff-summary.txt"

Ensure-Directory -Path $packRoot

$copiedCoreFiles = @()
$missingCoreFiles = @()

foreach ($relativePath in $coreRelativePaths) {
    $copied = Copy-RelativeFile -ProjectRootPath $ProjectRoot -RelativePath $relativePath -DestinationRoot $docsRoot
    if ($copied) {
        $copiedCoreFiles += $copied
    }
    else {
        $missingCoreFiles += $relativePath
    }
}

$copiedConfigFiles = @()
$configRoot = Join-Path $ProjectRoot "configs\examples"
if (Test-Path $configRoot) {
    foreach ($file in Get-ChildItem -Path $configRoot -File -Recurse | Sort-Object FullName) {
        $relativeConfigPath = $file.FullName.Substring($ProjectRoot.Length + 1)
        $copied = Copy-RelativeFile -ProjectRootPath $ProjectRoot -RelativePath $relativeConfigPath -DestinationRoot $docsRoot
        if ($copied) {
            $copiedConfigFiles += $copied
        }
    }
}

$resolvedWorkerPath = $null
if ($PublishedWorkerPath) {
    if (-not (Test-Path $PublishedWorkerPath)) {
        throw "Published worker path not found: $PublishedWorkerPath"
    }

    $resolvedWorkerPath = (Resolve-Path $PublishedWorkerPath).Path
}
else {
    $defaultWorkerPath = Join-Path $ProjectRoot "artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe"
    if (Test-Path $defaultWorkerPath) {
        $resolvedWorkerPath = (Resolve-Path $defaultWorkerPath).Path
    }
}

$copiedWorkerFiles = @()
if ($resolvedWorkerPath) {
    $copiedWorkerExe = Copy-FileFlat -SourcePath $resolvedWorkerPath -DestinationDirectory $workerRoot
    if ($copiedWorkerExe) {
        $copiedWorkerFiles += $copiedWorkerExe
    }

    $publishManifestPath = Join-Path (Split-Path -Parent $resolvedWorkerPath) "worker-publish.json"
    $copiedPublishManifest = Copy-FileFlat -SourcePath $publishManifestPath -DestinationDirectory $workerRoot
    if ($copiedPublishManifest) {
        $copiedWorkerFiles += $copiedPublishManifest
    }
}

$resolvedEvidencePackPath = $null
if ($EvidencePackPath) {
    if (-not (Test-Path $EvidencePackPath)) {
        throw "Evidence pack path not found: $EvidencePackPath"
    }

    $resolvedEvidencePackPath = (Resolve-Path $EvidencePackPath).Path
}
elseif ($IncludeEvidencePack) {
    $defaultEvidenceRoot = Join-Path $ProjectRoot "artifacts\handoff-packs"
    $resolvedEvidencePackPath = Resolve-LatestEvidencePack -RootPath $defaultEvidenceRoot
}

$evidenceManifest = $null
$copiedEvidencePackPath = $null
if ($resolvedEvidencePackPath) {
    Copy-Item -Path $resolvedEvidencePackPath -Destination $evidenceRoot -Recurse -Force
    $copiedEvidencePackPath = Join-Path $evidenceRoot (Split-Path -Leaf $resolvedEvidencePackPath)
    $evidenceManifest = Read-JsonFile -Path (Join-Path $resolvedEvidencePackPath "handoff-manifest.json")
}

$classification = [ordered]@{
    publicAlphaSupported = $true
    repoFirstDistributable = $true
    secondMachineValidated = $true
    handoffReady = $true
    receiverValidated = $true
    installerGrade = $false
}

$manifest = [ordered]@{
    schemaVersion = "alpha-handoff-2026-04-15-v1"
    generatedAt = (Get-Date).ToString("s")
    projectRoot = $ProjectRoot
    alphaHandoffPath = $packRoot
    intent = "repo-first technical alpha handoff companion"
    classification = $classification
    repoRequired = $true
    publicAlphaToolCount = 13
    publicAlphaPromptCount = 1
    publicAlphaResourceCount = 2
    copiedCoreFileCount = $copiedCoreFiles.Count
    copiedConfigFileCount = $copiedConfigFiles.Count
    missingCoreFileCount = $missingCoreFiles.Count
    publishedWorkerIncluded = [bool]$resolvedWorkerPath
    publishedWorkerSourcePath = $resolvedWorkerPath
    publishedWorkerFileCount = $copiedWorkerFiles.Count
    evidencePackIncluded = [bool]$resolvedEvidencePackPath
    evidencePackSourcePath = $resolvedEvidencePackPath
    evidencePackCopiedPath = $copiedEvidencePackPath
    evidencePackSchemaVersion = if ($evidenceManifest) { $evidenceManifest.schemaVersion } else { $null }
    receiverWorkingModel = [ordered]@{
        repoRequired = $true
        repoRole = "working root for scripts, builds, and server entrypoint"
        companionPackageRole = "orientation, curated docs, config discovery, and optional included worker handoff"
        includedWorkerShortcut = if ($resolvedWorkerPath) { "optional: point SOLIDWORKS_WORKER_COMMAND to the package-local worker copy" } else { "not available in this package" }
    }
    contents = [ordered]@{
        coreFiles = $copiedCoreFiles
        configFiles = $copiedConfigFiles
        workerFiles = $copiedWorkerFiles
        missingCoreFiles = $missingCoreFiles
    }
    notIncludedByDesign = @(
        "repository source code replacement",
        "node_modules",
        "full artifact history",
        "installer wrappers",
        "desktop extension packaging"
    )
    recommendedReadOrder = @(
        "README.md",
        "PROJECT_HANDOFF.md",
        "docs\\architecture\\ALPHA_DELIVERY_MODEL.md",
        "docs\\architecture\\FIRST_ALPHA_HANDOFF_GUIDE.md",
        "docs\\getting-started\\QUICKSTART_ALPHA.md",
        "docs\\architecture\\MCP_CLIENT_SETUP.md",
        "docs\\architecture\\PUBLIC_ALPHA_BOUNDARY.md",
        "docs\\architecture\\FIRST_PROMPT_MCP.md",
        "docs\\architecture\\FIRST_RESOURCE_MCP.md",
        "docs\\architecture\\SECOND_RESOURCE_MCP.md"
    )
}

$manifest | ConvertTo-Json -Depth 10 | Out-File -FilePath $manifestPath -Encoding utf8 -Force

$summaryLines = @(
    ("intent: {0}" -f $manifest.intent),
    ("repoRequired: {0}" -f $manifest.repoRequired),
    ("repoRole: working root for scripts, builds, and server entrypoint"),
    ("companionPackageRole: orientation, curated docs, config discovery, and optional included worker handoff"),
    ("publicAlphaSupported: {0}" -f $classification.publicAlphaSupported),
    ("repoFirstDistributable: {0}" -f $classification.repoFirstDistributable),
    ("secondMachineValidated: {0}" -f $classification.secondMachineValidated),
    ("handoffReady: {0}" -f $classification.handoffReady),
    ("receiverValidated: {0}" -f $classification.receiverValidated),
    ("installerGrade: {0}" -f $classification.installerGrade),
    ("publicAlphaToolCount: {0}" -f $manifest.publicAlphaToolCount),
    ("publicAlphaPromptCount: {0}" -f $manifest.publicAlphaPromptCount),
    ("publicAlphaResourceCount: {0}" -f $manifest.publicAlphaResourceCount),
    ("copiedCoreFileCount: {0}" -f $manifest.copiedCoreFileCount),
    ("copiedConfigFileCount: {0}" -f $manifest.copiedConfigFileCount),
    ("publishedWorkerIncluded: {0}" -f $manifest.publishedWorkerIncluded),
    ("publishedWorkerSourcePath: {0}" -f $manifest.publishedWorkerSourcePath),
    ("includedWorkerShortcut: {0}" -f $(if ($manifest.publishedWorkerIncluded) { "optional: point SOLIDWORKS_WORKER_COMMAND to <package-root>\\worker\\SolidWorksWorker.exe while keeping the repo checkout as working root" } else { "not available in this package" })),
    ("evidencePackIncluded: {0}" -f $manifest.evidencePackIncluded),
    ("evidencePackSourcePath: {0}" -f $manifest.evidencePackSourcePath),
    ("alphaHandoffPath: {0}" -f $packRoot),
    ("alphaHandoffManifest: {0}" -f $manifestPath),
    "",
    "recommendedReadOrder:",
    "1. README.md",
    "2. PROJECT_HANDOFF.md",
    "3. docs\\architecture\\ALPHA_DELIVERY_MODEL.md",
    "4. docs\\architecture\\FIRST_ALPHA_HANDOFF_GUIDE.md",
    "5. docs\\getting-started\\QUICKSTART_ALPHA.md",
    "6. docs\\architecture\\MCP_CLIENT_SETUP.md",
    "7. docs\\architecture\\PUBLIC_ALPHA_BOUNDARY.md",
    "8. docs\\architecture\\FIRST_PROMPT_MCP.md",
    "9. docs\\architecture\\FIRST_RESOURCE_MCP.md",
    "10. docs\\architecture\\SECOND_RESOURCE_MCP.md",
    "",
    "receiverNotes:",
    "- use the companion package for orientation and handoff context",
    "- use the accompanying repository checkout as the working root for scripts and builds",
    "- if a referenced doc is not duplicated in the package, open it from the repo checkout",
    "",
    "notIncludedByDesign:",
    "- repository source replacement",
    "- node_modules",
    "- full artifact history",
    "- installer wrappers",
    "- desktop extension packaging"
) -join [Environment]::NewLine

$summaryLines | Out-File -FilePath $summaryPath -Encoding utf8 -Force

Write-Output ("ALPHA_HANDOFF={0}" -f $packRoot)
Write-Output ("ALPHA_HANDOFF_MANIFEST={0}" -f $manifestPath)
Write-Output ("ALPHA_HANDOFF_SUMMARY={0}" -f $summaryPath)
exit 0
