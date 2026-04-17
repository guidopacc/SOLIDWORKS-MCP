# SLICE 2 SKETCH CHECKLIST

## Purpose

Capture the minimum real validation needed for the first visible 2D geometry slice on SolidWorks 2022.

## Scope

This slice is intentionally narrow:

- handshake
- `new_part`
- `select_plane`
- `start_sketch`
- `draw_circle`
- `close_sketch`
- optional `save_part` only to preserve external evidence

This slice does **not** claim:

- centered rectangle support
- advanced sketch entities
- dimensions or constraints
- full multi-plane validation

## 2026-04-09 company Windows evidence

Environment:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0`

Primary evidence files from the real run:

- `C:\SolidWorksMcpValidation\slice2\slice2-sequence.ndjson`
- `C:\SolidWorksMcpValidation\slice2\slice2-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\slice2\slice2-summary.txt`
- `C:\SolidWorksMcpValidation\slice2\slice2-part.SLDPRT`

## Validated flow

The validated worker flow was:

1. `handshake_request`
2. `new_part`
3. `select_plane` with `Front Plane`
4. `start_sketch`
5. `draw_circle` with center `(0, 0)` and radius `10 mm`
6. `close_sketch`
7. `save_part` to a deterministic local `.SLDPRT` path
8. `shutdown`

## What passed

- handshake payload was complete
- worker execution metadata stayed complete
- normalized document/sketch state stayed coherent through the full flow
- the saved part reopened successfully
- the reopened part exposed `Sketch1:ProfileFeature`

## What is implemented vs proven

- implemented:
  - `select_plane` for the three standard planes
  - `start_sketch`
  - `draw_circle`
  - `close_sketch`
- compiled locally:
  - yes
- tested locally:
  - yes
- tested on real SolidWorks 2022:
  - yes for the flow above on `Front Plane`

## Validation notes

- `draw_circle` was chosen as the first visible geometry instead of `draw_centered_rectangle`.
  Reason: it was the least ambiguous and most reliable geometry path proven during direct SolidWorks 2022 probing.

- external inspection of the worker-launched SolidWorks session through a second-process ROT attachment was not reliable enough for this slice.
  Because of that, the external evidence path for slice 2 uses the saved `.SLDPRT` reopened in a separate probe.

- the saved part exposes `Sketch1:ProfileFeature`, but automatic sketch-segment counting from the reopened file is still not reliable in the current harness.
  This means slice 2 has strong saved-file evidence, but the harness can still be improved.

## Exit criteria

Slice 2 should be considered passed only when all of the following are true:

- the worker build is clean
- the worker returns complete NDJSON payloads
- the sketch lifecycle flow succeeds on real SolidWorks 2022
- the resulting part can be saved and reopened
- the reopened part shows the expected sketch feature
