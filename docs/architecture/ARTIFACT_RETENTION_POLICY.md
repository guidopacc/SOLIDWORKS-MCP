# ARTIFACT RETENTION POLICY

## Purpose

Keep repository-side artifacts readable and useful over time without introducing destructive cleanup by default.

## Scope

This policy applies primarily to repository-side artifact trees:

- `C:\Tools\SOLIDWORKS-MCP\artifacts\worker\cycles\`
- `C:\Tools\SOLIDWORKS-MCP\artifacts\real-evidence\`
- `C:\Tools\SOLIDWORKS-MCP\artifacts\handoff-packs\`

It does **not** automatically prune:

- `C:\SolidWorksMcpValidation\<scenario>\`
- `C:\SolidWorksMcpValidation\regression-runs\`
- repository build/publish manifests

Those remain the stronger source-of-truth layer for raw run evidence.

## Retention categories

### Always keep

- latest cycle directory
- latest passing cycle directory
- latest `standard_minimal` cycle directory
- latest `full_with_cad_files` cycle directory
- latest passing `standard_minimal` evidence bundle
- latest passing `full_with_cad_files` evidence bundle
- latest handoff pack
- current artifact index files under:
  - `artifacts\indexes\`

### Keep recent

Default safe retention targets:

- latest 5 cycle directories
- latest 5 evidence bundles
- latest 5 handoff packs

### Keep recent failures

Even when older artifacts become prune candidates, keep at least:

- latest 2 failed cycle directories
- latest 2 failed evidence bundles

That preserves useful debugging history without keeping everything forever.

### Prunable

The first prune target is only repository-side copied material:

- older cycle directories not in the protected set
- older evidence bundles not in the protected set
- older handoff packs not in the protected set

Within those directories, the following are considered prunable **only because the whole directory is prunable**:

- manifests and summaries from that old copied run
- transcript NDJSON copies
- copied inspection artifacts
- copied `.SLDPRT` / `.STEP` files
- copied extras

The policy does not currently prune files selectively inside a kept directory.

## Safety rule

Default pruning mode is dry-run.

Use:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\prune-worker-artifacts.ps1
```

That generates only a prune plan and summary.

Deletion requires explicit opt-in:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\prune-worker-artifacts.ps1 -Apply
```

## Why this policy is conservative

- the repository-side artifact store is a convenience layer, not the only evidence source
- older failing runs can still be diagnostically useful
- CAD files can be large, but blindly deleting them is riskier than keeping a modest protected window
- the project is still in an evidence-building phase, not in a high-volume production pipeline phase

## Current script support

Implemented:

- `scripts\prune-worker-artifacts.ps1`

Current behavior:

- dry-run by default
- repository-side scope only
- manifest-backed protection of recent and important directories
- structured prune plan output under:
  - `artifacts\indexes\worker-artifact-prune-plan-*.json`
  - `artifacts\indexes\worker-artifact-prune-plan-*.txt`
