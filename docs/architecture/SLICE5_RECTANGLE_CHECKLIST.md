# SLICE 5 RECTANGLE CHECKLIST

## Purpose

Capture the minimum real validation needed for the first centered-rectangle sketch primitive on SolidWorks 2022.

## Scope

This slice intentionally stays narrow:

- `draw_centered_rectangle`
- real 2D saved-sketch validation
- real 3D boss extrusion from the same rectangle profile

This slice does **not** include:

- dimensions
- constraints
- arbitrary polygons
- cut extrude
- fillet

## 2026-04-09 company Windows evidence

Environment:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0`

Primary evidence files from the real runs:

- `C:\SolidWorksMcpValidation\rect2d\rect2d-sequence.ndjson`
- `C:\SolidWorksMcpValidation\rect2d\rect2d-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\rect2d\rect2d-summary.txt`
- `C:\SolidWorksMcpValidation\rect2d\rect2d-part.SLDPRT`
- `C:\SolidWorksMcpValidation\rect3d\rect3d-sequence.ndjson`
- `C:\SolidWorksMcpValidation\rect3d\rect3d-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\rect3d\rect3d-summary.txt`
- `C:\SolidWorksMcpValidation\rect3d\rect3d-part.SLDPRT`

## Validated flows

### 2D rectangle flow

1. `handshake_request`
2. `new_part`
3. `select_plane` with `Front Plane`
4. `start_sketch`
5. `draw_centered_rectangle` with center `(0, 0)` and corner `(20, 10)` in mm
6. `close_sketch`
7. `save_part`
8. `shutdown`

### 3D rectangle flow

1. `handshake_request`
2. `new_part`
3. `select_plane` with `Front Plane`
4. `start_sketch`
5. `draw_centered_rectangle`
6. `close_sketch`
7. `extrude_boss` from `sketch-1`
8. `save_part`
9. `shutdown`

## What passed

- handshake and execution payloads remained complete
- the worker returned coherent normalized state with a `centered_rectangle` entity
- the saved 2D rectangle part reopened successfully and exposed `Sketch1:ProfileFeature`
- the rectangle-based boss extrusion reopened successfully and exposed:
  - `Sketch1:ProfileFeature`
  - `Boss-Extrude1:Extrusion`

## What is implemented vs proven

- implemented:
  - `draw_centered_rectangle`
- compiled locally:
  - yes
- tested locally:
  - yes
- tested on real SolidWorks 2022:
  - yes for the 2D and 3D flows above

## Validation notes

- the first real rectangle path uses `SketchManager.CreateCenterRectangle(...)`.
- the raw SolidWorks-created segment count observed in `operationDetails.segmentCount` was `6` on this machine for the proven rectangle case.
- the normalized worker state still represents that geometry as one `centered_rectangle` entity, which is the intended backend contract.
- the successful rectangle-based `extrude_boss` run is downstream evidence that the closed rectangle profile was real and usable.

## Exit criteria

Slice 5 should be considered passed only when all of the following are true:

- the worker build is clean
- the worker tests are clean
- `draw_centered_rectangle` succeeds on real SolidWorks 2022
- the saved 2D part exists on disk
- the rectangle profile can drive a real boss extrusion
- the reopened saved 3D part exposes the expected extrusion feature
