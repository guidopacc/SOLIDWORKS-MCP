# EVIDENCE BUNDLE MODES

## Purpose

Clarify when to use the default repository-side evidence bundle and when to use the fuller variant that also copies CAD files.

## Bundle modes

### `standard_minimal`

Default path:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\collect-real-worker-evidence.ps1
```

Or through the full cycle:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-windows-worker-cycle.ps1
```

What it copies:

- scenario summaries
- scenario inspection artifacts
- scenario input/output transcripts
- extra metadata files passed by the cycle, such as:
  - preflight json
  - build manifest
  - publish manifest
  - smoke transcripts
  - regression stdout
  - regression run manifest

What it does not copy:

- saved `.SLDPRT` files
- exported `.step` / `.stp` files

Use it when:

- you want routine baseline reruns
- you want lighter repository-side evidence
- you only need structured proof and logs, not the CAD payload itself

### `full_with_cad_files`

Explicit path:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\collect-real-worker-evidence.ps1 -IncludeCadFiles
```

Or through the full cycle:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-windows-worker-cycle.ps1 -IncludeCadFiles
```

What it copies in addition to the standard bundle:

- saved `.SLDPRT` files per scenario when present
- exported `.step` / `.stp` files when present

Use it when:

- you need a fuller handoff package
- you need artifact retention for review or audit
- you want one bundle that preserves both logs and generated CAD outputs

## Artifact outputs

Both modes now emit:

- `evidence-manifest.json`
- `evidence-summary.txt`

Both files expose:

- `bundleMode`
- whether CAD files were included
- copied scenario count
- copied evidence-file count
- copied CAD-file count
- copied extra-file count
- total copied size
- aggregated assertion and evidence counts

For cross-run cataloging, refresh the artifact index after new bundles are created:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\update-worker-artifact-index.ps1
```

## Honesty rule

`full_with_cad_files` is a bigger bundle, not stronger logic.

It does not change:

- which worker capabilities are proven
- which scenarios passed
- the distinction between strong, supporting, weak, and cautionary evidence

It only preserves more artifacts from the same run.

## Current verification status

- `standard_minimal`:
  - implemented
  - tested locally
  - exercised in the preferred real Windows cycle
- `full_with_cad_files`:
  - implemented
  - tested locally
  - exercised in the full Windows cycle on the company SolidWorks 2022 machine on `2026-04-10`
