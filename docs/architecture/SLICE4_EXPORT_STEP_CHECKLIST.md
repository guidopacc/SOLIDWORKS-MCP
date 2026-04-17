# SLICE 4 EXPORT STEP CHECKLIST

## Purpose

Capture the minimum real validation needed for the first real STEP export slice on SolidWorks 2022.

## Scope

This slice intentionally stays narrow:

- reuse the proven slice 3 part workflow
- `export_step`
- on-disk verification of the exported `.step` file

This slice does **not** include:

- IGES
- STL
- PDF
- assembly export
- drawing export

## 2026-04-09 company Windows evidence

Environment:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0`

Primary evidence files from the real run:

- `C:\SolidWorksMcpValidation\slice4\slice4-sequence.ndjson`
- `C:\SolidWorksMcpValidation\slice4\slice4-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\slice4\slice4-summary.txt`
- `C:\SolidWorksMcpValidation\slice4\slice4-part.SLDPRT`
- `C:\SolidWorksMcpValidation\slice4\slice4-export.step`

## Validated flow

The validated worker flow was:

1. `handshake_request`
2. `new_part`
3. `select_plane` with `Front Plane`
4. `start_sketch`
5. `draw_circle` with center `(0, 0)` and radius `10 mm`
6. `close_sketch`
7. `extrude_boss` from `sketch-1` with depth `20 mm`
8. `save_part`
9. `export_step`
10. `shutdown`

## What passed

- handshake and execution payloads remained complete
- the worker returned coherent normalized export state
- `nextState.currentDocument.exports` recorded the STEP artifact path
- `execution.operationDetails` reported the export mode and resolved path
- the `.SLDPRT` file remained present on disk
- the `.step` file was present on disk with non-zero size
- the reopened saved part still exposed:
  - `Sketch1:ProfileFeature`
  - `Boss-Extrude1:Extrusion`

## What is implemented vs proven

- implemented:
  - `export_step`
- compiled locally:
  - yes
- tested locally:
  - yes
- tested on real SolidWorks 2022:
  - yes for the flow above

## Validation notes

- the first stable export path uses `ModelDoc2.SaveAs4(...)` against a `.step` target.
- the active `.SLDPRT` path remained stable after export in the real validation run.
- the current export slice is intentionally limited to part documents.
- the current export slice supports only `.step` and `.stp` target extensions.

## Exit criteria

Slice 4 should be considered passed only when all of the following are true:

- the worker build is clean
- the worker tests are clean
- the full part flow through extrude still succeeds
- `export_step` returns a complete NDJSON response
- the requested STEP file exists on disk after command completion
- the active part document state remains coherent after export
