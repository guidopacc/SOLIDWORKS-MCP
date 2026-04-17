# SLICE 7 ADD DIMENSION CHECKLIST

## Purpose

Record the first bounded investigation of `add_dimension` on SolidWorks 2022 without overstating support.

## Intended narrow scope

The investigated slice was intentionally limited to:

- one active sketch
- one horizontal line
- one horizontal dimension on that line
- no general smart-dimension system
- no circle diameter/radius support
- no constraint solving claims beyond the explicit dimension object

## 2026-04-09 company Windows evidence

Environment:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0`

Primary evidence files from the first real failure:

- `C:\SolidWorksMcpValidation\line-dim2d\line-dim2d-sequence.ndjson`
- `C:\SolidWorksMcpValidation\line-dim2d\line-dim2d-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\line-dim2d\line-dim2d-summary.txt`
- `C:\SolidWorksMcpValidation\dimension-probe\probe-summary.txt`
- `C:\SolidWorksMcpValidation\dimension-probe\select-coordinate\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\select-created-line\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\select-live-segment\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\select-line-endpoints\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-created-line\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-horizontal-endpoints\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-vertical-created-line\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-typed-horizontal-endpoints\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-typed-vertical-created-line\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-typed-generic-endpoints\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-sketchmanager-alongx-endpoints\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-sketchmanager-alongy-endpoints\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-generic-created-line\probe.log`
- `C:\SolidWorksMcpValidation\dimension-probe\dimension-generic-endpoints\probe.log`

## First real attempt

Target flow:

1. `handshake_request`
2. `new_part`
3. `select_plane`
4. `start_sketch`
5. `draw_line`
6. `add_dimension`
7. `close_sketch`
8. `save_part`
9. `shutdown`

Observed result:

- `draw_line` succeeded
- `add_dimension` returned a typed failure
- failure classification: `unable_to_add_dimension`
- failure stage: `add_dimension`
- primary reason captured in NDJSON:
  - `SelectByID2 could not resolve the target sketch segment.`
- dedicated probe follow-up showed the lower-level cause more precisely:
  - `IModelDocExtension.SelectByID2(...)` on the sketch segment path raised `DISP_E_TYPEMISMATCH`
  - direct `Select2(false, 0)` on the live sketch segment succeeded
  - direct `Select2(...)` on the created line returned by `CreateLine(...)` also succeeded
  - direct `Select2(...)` on both line endpoints also succeeded

## Second real attempt

Follow-up probe after adding a direct active-sketch segment-selection fallback:

- the worker no longer failed fast with the same typed payload
- the worker and SolidWorks could instead remain blocked with the document still open in sketch edit mode
- the run did not complete cleanly or produce a fresh transcript flush before timeout

Observed blocker state:

- live SolidWorks window remained in `Sketch1` edit mode
- test worker process remained alive until manually terminated
- dedicated probe log showed the last successful step before the hang:
  - direct segment selection had already succeeded
  - the hang began at `AddHorizontalDimension2(...)`
- further dedicated probe follow-up showed that:
  - `AddDimension2(...)` is not a quick safe fallback either
  - after a valid direct selection, `AddDimension2(...)` returned `0x800706BE` (`RPC_S_CALL_FAILED`)
- additional probe follow-up then showed that:
  - `AddHorizontalDimension2(...)` can also hang after both line endpoints are already validly selected
  - `AddVerticalDimension2(...)` can hang on a selected vertical line as well
- a final bounded probe round then showed that:
  - typed `IAddHorizontalDimension2(...)` can still hang after endpoint selection
  - typed `IAddVerticalDimension2(...)` can still hang on a selected vertical line
  - typed `IAddDimension2(...)` can still hang after endpoint selection
  - `ISketchManager.AddAlongXDimension(...)` can still hang after endpoint selection
  - `ISketchManager.AddAlongYDimension(...)` can still hang on a selected vertical line

## What is implemented vs proven

- designed:
  - yes
- partially implemented locally:
  - yes, behind local code and tests
- compiled locally:
  - yes
- tested locally:
  - yes for parsing/state-mapping scaffolding
- tested on real SolidWorks 2022:
  - attempted, but not passed
- part of the advertised real worker surface:
  - no

## Decision from this checklist

`add_dimension` must stay out of the advertised real worker slice until all of the following are true:

- segment selection is stable on real SolidWorks 2022
- dimension creation returns deterministically instead of hanging
- at least one minimal dimension flow passes end-to-end with saved-file evidence

## Current blocker summary

- coordinate-based segment resolution through `SelectByID2(...)` is not currently a safe base for this slice on the baseline machine
- direct segment selection is possible, but the first dimension-creation call `AddHorizontalDimension2(...)` can still hang the live SolidWorks 2022 session
- the same hanging pattern now reproduces on endpoint-based horizontal selection and on a vertical-line `AddVerticalDimension2(...)` attempt
- the same unsafe behavior now also reproduces across typed `IModelDoc2` dimension calls and `ISketchManager` dimension calls
- `AddDimension2(...)` is also not a safe first alternative on this machine, even after valid direct selection
- because of that risk, `add_dimension` is not promoted to the current real worker surface
