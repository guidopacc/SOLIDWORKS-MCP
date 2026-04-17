# REAL REGRESSION RUNBOOK

## Current baseline scenarios

The default serialized baseline is:

- `plane-top`
- `plane-right`
- `line2d`
- `rect2d`
- `rect3d`
- `slice4`

## Why serialized

On the company Windows machine, parallel real runs against the same SolidWorks installation produce false negatives.

Use only:

- `scripts\run-real-worker-regression.ps1`
- `scripts\run-real-export-regression.ps1`

By default the baseline script also runs Windows preflight first.

## Baseline command

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-real-worker-regression.ps1
```

## Published-worker command

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-real-worker-regression.ps1 `
  -WorkerExe C:\Tools\SOLIDWORKS-MCP\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe
```

Equivalent environment override:

```powershell
$env:SOLIDWORKS_MCP_VALIDATION_WORKER_EXE='C:\Tools\SOLIDWORKS-MCP\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe'
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-real-worker-regression.ps1
```

## Export-only command

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-real-export-regression.ps1
```

## Outputs

For each scenario:

- `<scenario>-sequence.ndjson`
- `<scenario>-sequence.out.ndjson`
- `<scenario>-summary.txt`
- `<scenario>-inspection.json`

For each regression session:

- `C:\SolidWorksMcpValidation\regression-runs\worker-regression-*.json`

The run manifest records:

- timestamp
- run duration
- worker executable used
- scenarios attempted
- scenario pass/fail
- assertion pass/fail counts
- strong/supporting/weak/caution evidence counts
- saved-part and export paths when present
- summary and inspection artifact paths

## Reading results quickly

The fastest files to inspect are:

- the run manifest for scenario pass/fail at session level
- the scenario summary for human-readable assertions
- the scenario inspection artifact for structured evidence

The preferred evidence order is now:

1. strong evidence from the inspection artifact
2. supporting feature-tree and transcript facts
3. weak evidence such as sketch-segment counting only when available

## Evidence bundling

To collect a copy of the current artifacts under the repository:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\collect-real-worker-evidence.ps1
```

That default bundle mode is intentionally minimal:

- summaries
- inspection artifacts
- input/output transcripts
- no copied CAD files unless requested explicitly

To collect the fuller handoff bundle with saved `.SLDPRT` and exported `.step` / `.stp` files:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\collect-real-worker-evidence.ps1 -IncludeCadFiles
```

The collector now writes:

- `evidence-manifest.json`
- `evidence-summary.txt`

The manifest records `bundleMode` explicitly:

- `standard_minimal`
- `full_with_cad_files`

Useful follow-up helpers after a baseline run:

- refresh artifact index:
  - `pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\update-worker-artifact-index.ps1`
- create dry-run prune plan:
  - `pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\prune-worker-artifacts.ps1`
- prepare internal handoff pack:
  - `pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\new-handoff-evidence-pack.ps1`

If preflight has already been run in the same session and you only want to rerun the baseline quickly:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-real-worker-regression.ps1 -SkipPreflight
```
