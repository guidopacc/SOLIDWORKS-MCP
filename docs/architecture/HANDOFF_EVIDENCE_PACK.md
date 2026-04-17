# HANDOFF EVIDENCE PACK

## Purpose

Define one repeatable internal handoff package built from already-collected evidence, without pretending to be a release artifact or installer.

This evidence pack is not the same thing as the alpha handoff companion package prepared by:

- `.\scripts\prepare-alpha-handoff.ps1`

Use the alpha handoff companion for onboarding and controlled sharing.
Use this evidence pack only when review depth requires the proof bundle.

## Generator

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\new-handoff-evidence-pack.ps1
```

Optional label:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\new-handoff-evidence-pack.ps1 -PackLabel latest-full
```

## Default source selection

If no bundle is provided explicitly, the script chooses:

1. latest passing `full_with_cad_files` evidence bundle
2. otherwise latest passing evidence bundle
3. otherwise latest available evidence bundle

It then tries to locate the related cycle manifest automatically.

## Output

The handoff pack is created under:

- `C:\Tools\SOLIDWORKS-MCP\artifacts\handoff-packs\<timestamp>\`

Main files:

- `handoff-manifest.json`
- `handoff-summary.txt`

## Contents

The pack currently copies:

- cycle-level files when available:
  - `windows-worker-cycle.json`
  - `windows-worker-cycle-summary.txt`
  - linked regression manifest
- evidence-level files:
  - `evidence-manifest.json`
  - `evidence-summary.txt`
- per-scenario files from the chosen evidence bundle
- `extras\` files from the chosen evidence bundle when present

If the source bundle is `full_with_cad_files`, the pack also includes copied `.SLDPRT` and `.step` files already preserved in that source bundle.

## Recommended review order

1. `handoff-summary.txt`
2. cycle summary
3. linked regression manifest
4. evidence summary
5. evidence manifest
6. scenario summaries
7. scenario inspection artifacts
8. scenario CAD files, when present

## What this is not

- not a release package
- not a zip-by-default workflow
- not a deployment artifact
- not a replacement for the original validation-root evidence

## Current status

- designed: yes
- implemented: yes
- tested locally against real-generated artifacts: yes
- tested by running a new SolidWorks 2022 scenario in this session: no

The pack is built from existing evidence. It improves internal delivery of proof, but it does not itself create new CAD validation evidence.
