# SLICE 3 EXTRUDE CHECKLIST

## Purpose

Capture the minimum real validation needed for the first 3D solid feature slice on SolidWorks 2022.

## Scope

This slice intentionally stays narrow:

- reuse the proven slice 2 circle-sketch flow
- `extrude_boss`
- `save_part`

This slice does **not** include:

- cut extrude
- fillet
- shell
- pattern
- STEP export

## 2026-04-09 company Windows evidence

Environment:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0`

Primary evidence files from the real run:

- `C:\SolidWorksMcpValidation\slice3\slice3-sequence.ndjson`
- `C:\SolidWorksMcpValidation\slice3\slice3-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\slice3\slice3-summary.txt`
- `C:\SolidWorksMcpValidation\slice3\slice3-part.SLDPRT`

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
9. `shutdown`

## What passed

- handshake and execution payloads remained complete
- the worker returned coherent normalized feature state
- `execution.operationDetails` reported `featureName = Boss-Extrude1` and `featureType = Extrusion`
- the saved part reopened successfully
- the reopened part exposed:
  - `Sketch1:ProfileFeature`
  - `Boss-Extrude1:Extrusion`

## What is implemented vs proven

- implemented:
  - `extrude_boss` with a minimal blind-boss path
- compiled locally:
  - yes
- tested locally:
  - yes
- tested on real SolidWorks 2022:
  - yes for the flow above

## Validation notes

- the first reliable boss-extrude path uses sketch-feature selection plus `FeatureExtrusion3(...)`.
- earlier probing showed that `SimpleFeatureBossExtrude(...)` was not reliable enough for this slice.
- the saved part path is externally verified and matches the worker's normalized state.

## Exit criteria

Slice 3 should be considered passed only when all of the following are true:

- the slice 2 sketch flow is already stable
- the boss extrusion succeeds on real SolidWorks 2022
- the response payload is complete
- the resulting file exists on disk
- the reopened part exposes the expected extrusion feature
