# SLICE 6 DRAW LINE CHECKLIST

## Purpose

Capture the minimum real validation needed for the first real `draw_line` slice on SolidWorks 2022.

## Scope

This slice intentionally stays narrow:

- `draw_line`
- real 2D saved-sketch validation
- no dimensions
- no profile solving
- no feature creation from the line

## 2026-04-09 company Windows evidence

Environment:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0`

Primary evidence files from the real run:

- `C:\SolidWorksMcpValidation\line2d\line2d-sequence.ndjson`
- `C:\SolidWorksMcpValidation\line2d\line2d-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\line2d\line2d-summary.txt`
- `C:\SolidWorksMcpValidation\line2d\line2d-part.SLDPRT`

## Validated flow

1. `handshake_request`
2. `new_part`
3. `select_plane` with `Front Plane`
4. `start_sketch`
5. `draw_line` from `(0, 0)` to `(30, 0)` in mm
6. `close_sketch`
7. `save_part`
8. `shutdown`

## What passed

- handshake and execution payloads remained complete
- the worker returned coherent normalized state with one `line` entity in `sketch-1`
- `execution.operationDetails.lengthMm` reported the expected `30 mm`
- the saved 2D part reopened successfully and exposed `Sketch1:ProfileFeature`
- the active saved path in the reopen probe matched the requested `.SLDPRT` path

## What is implemented vs proven

- implemented:
  - `draw_line`
- compiled locally:
  - yes
- tested locally:
  - yes
- tested on real SolidWorks 2022:
  - yes for the 2D flow above

## Validation notes

- the first real `draw_line` slice intentionally uses a single horizontal line with explicit endpoints.
- the normalized state keeps one `line` entity, not raw SolidWorks selection handles.
- the current external readback still trusts the reopened `Sketch1:ProfileFeature` plus the saved-file path more than raw sketch-segment counting.

## Exit criteria

Slice 6 should be considered passed only when all of the following are true:

- the worker build is clean
- the worker tests are clean
- `draw_line` succeeds on real SolidWorks 2022
- the saved 2D part exists on disk
- the reopened part exposes `Sketch1:ProfileFeature`
- the worker response remains complete and typed
