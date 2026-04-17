# WINDOWS EXECUTION RUNBOOK

## Purpose

Give one concrete Windows path for building, publishing, running, and validating the real SolidWorks worker.

## Recommended invocation style

Use the scripts from the repository root:

```powershell
.\scripts\validate-windows-environment.ps1
```

If you must run them from another folder, set a local repo root first:

```powershell
$repoRoot = 'C:\path\to\SOLIDWORKS-MCP'
pwsh -File "$repoRoot\scripts\validate-windows-environment.ps1"
```

For the rest of this document, `<repo-root>` means your actual local checkout path.

## Preflight

Run:

```powershell
.\scripts\validate-windows-environment.ps1
```

Useful artifact:

- `C:\SolidWorksMcpValidation\preflight\worker-preflight.json`

Optional flags:

- `-SkipNodeChecks`
- `-SkipBuildOutputChecks`
- `-EmitJsonPath <custom json path>`

## Build debug worker

Run:

```powershell
.\scripts\build-solidworks-worker.ps1 -Configuration Debug
```

Outputs:

- debug worker executable
- local worker test run
- build artifact manifest under:
  - `<repo-root>\artifacts\worker\build\Debug\worker-build.json`

## Publish release worker

Run:

```powershell
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

Current publish mode:

- framework-dependent
- `win-x64`
- not single-file

Outputs:

- published worker executable:
  - `<repo-root>\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe`
- publish artifact manifest:
  - `<repo-root>\artifacts\worker\publish\framework-dependent\win-x64\Release\worker-publish.json`

## Run worker directly

Debug build:

```powershell
.\scripts\run-solidworks-worker.ps1 -Source build -Configuration Debug
```

Published worker:

```powershell
.\scripts\run-solidworks-worker.ps1 -Source publish -Configuration Release
```

Minimal protocol smoke check through the wrapper:

```powershell
$messages = @(
  '{"messageType":"handshake_request","requestId":"handshake-1","protocolVersion":"0.1.0","client":{"name":"manual-wrapper-check","version":"0.1.0"}}',
  '{"messageType":"shutdown_request","requestId":"shutdown-1","protocolVersion":"0.1.0"}'
)

.\scripts\run-solidworks-worker.ps1 `
  -Source publish `
  -Configuration Release `
  -InputLine $messages
```

## Run real baseline regression

Default debug worker:

```powershell
.\scripts\run-real-worker-regression.ps1
```

Specific published worker:

```powershell
.\scripts\run-real-worker-regression.ps1 `
  -WorkerExe .\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe
```

Equivalent environment override:

```powershell
$env:SOLIDWORKS_MCP_VALIDATION_WORKER_EXE='.\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe'
.\scripts\run-real-worker-regression.ps1
```

## Preferred full Windows cycle

For the current disciplined local path on the company machine:

```powershell
.\scripts\run-windows-worker-cycle.ps1
```

For the heavier bundle mode that also copies saved `.SLDPRT` and exported `.step` files into the repository evidence bundle:

```powershell
.\scripts\run-windows-worker-cycle.ps1 -IncludeCadFiles
```

The cycle performs:

- preflight
- debug build plus local worker tests
- framework-dependent publish
- protocol smoke check through the worker wrapper
- serialized real regression on the published worker
- evidence bundle collection

Mode guidance:

- standard default:
  - use for routine reruns and regression discipline
  - keeps the repository-side bundle smaller by copying summaries, transcripts, inspection artifacts, and extra metadata files only
- `-IncludeCadFiles`:
  - use when you need a fuller evidence package for handoff, audit, or artifact retention
  - also copies saved `.SLDPRT` and exported `.step` / `.stp` files per scenario when present

## Run export-only regression

```powershell
.\scripts\run-real-export-regression.ps1 `
  -WorkerExe .\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe
```

## Collect evidence bundle

```powershell
.\scripts\collect-real-worker-evidence.ps1
```

Optional CAD files:

```powershell
.\scripts\collect-real-worker-evidence.ps1 -IncludeCadFiles
```

## Main artifact locations

- per-scenario raw outputs:
  - `C:\SolidWorksMcpValidation\<scenario>\`
- per-scenario inspection artifact:
  - `C:\SolidWorksMcpValidation\<scenario>\<scenario>-inspection.json`
- regression run manifest:
  - `C:\SolidWorksMcpValidation\regression-runs\worker-regression-*.json`
- repository-side worker build manifests:
  - `<repo-root>\artifacts\worker\build\`
- repository-side worker publish manifests:
  - `<repo-root>\artifacts\worker\publish\`
- copied evidence bundles:
  - `<repo-root>\artifacts\real-evidence\`
- handoff packs:
  - `<repo-root>\artifacts\handoff-packs\`
- artifact indexes and prune plans:
  - `<repo-root>\artifacts\indexes\`
- cycle manifests:
  - `<repo-root>\artifacts\worker\cycles\`
- cycle summaries:
  - `<repo-root>\artifacts\worker\cycles\<timestamp>\windows-worker-cycle-summary.txt`
- evidence summaries:
  - `<repo-root>\artifacts\real-evidence\<timestamp>\evidence-summary.txt`

## Setup-error note

If `npm test` fails before any worker interaction with a `rolldown` or native-binding startup error, treat it as a local Node/toolchain setup issue, not as a SolidWorks worker failure.

The clean repository verification baseline is:

- Node.js `^20.19.0 || >=22.12.0`

## Machine-readable output lines

The main scripts now emit machine-readable `NAME=value` lines for downstream automation, including:

- `WORKER_EXE`
- `BUILD_ARTIFACT`
- `WORKER_PUBLISH_EXE`
- `PUBLISH_ARTIFACT`
- `RUN_MANIFEST`
- `EVIDENCE_BUNDLE`
- `EVIDENCE_MANIFEST`
- `EVIDENCE_SUMMARY`
- `CYCLE_MANIFEST`
- `CYCLE_SUMMARY`

## Artifact governance helpers

Refresh the lightweight artifact inventory:

```powershell
.\scripts\update-worker-artifact-index.ps1
```

Create a prune plan in safe dry-run mode:

```powershell
.\scripts\prune-worker-artifacts.ps1
```

Prepare an internal handoff pack from the latest fuller evidence bundle:

```powershell
.\scripts\new-handoff-evidence-pack.ps1
```

Prepare the current alpha handoff companion package:

```powershell
.\scripts\prepare-alpha-handoff.ps1
```

Prepare the alpha handoff companion and include the latest evidence pack copy:

```powershell
.\scripts\prepare-alpha-handoff.ps1 -IncludeEvidencePack
```
