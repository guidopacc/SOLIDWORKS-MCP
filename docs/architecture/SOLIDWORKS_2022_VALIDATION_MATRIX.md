# SOLIDWORKS 2022 VALIDATION MATRIX

## Purpose

Define the minimum real-machine validation required before `SOLIDWORKS-MCP` can claim any level of real SolidWorks 2022 support.

This matrix exists to prevent two common failures:

- confusing “designed” or “implemented” with “supported”
- confusing mock-tested behavior with real SolidWorks behavior

## Definition of “real SolidWorks 2022 support”

For this repository, **real SolidWorks 2022 support** means:

1. the capability was executed on a Windows machine with a real SolidWorks 2022 installation
2. the execution outcome was observed and recorded
3. the expected result was confirmed by both:
   - runtime response
   - externally visible evidence when applicable (document state, output file, etc.)
4. failures and limitations were recorded honestly

Support levels should remain explicit:

- `designed`
- `implemented`
- `tested in mock`
- `tested on real SolidWorks 2022`
- `officially supported`

## Validation levels

### Level 0: Real backend bootstrap proven

This is the bar for completing the **first real worker slice**.

Current repository note:

- the Level 0 worker slice is implemented in code
- it has been compiled on the company Windows machine
- it has been validated on a real SolidWorks 2022 installation
- backend/session/version facts are emitted through handshake metadata and command execution or failure envelopes, not through dedicated diagnostics commands yet

## 2026-04-09 company Windows validation result

Environment used for the real run:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0` (`ProductVersion 30.4.0.0045`)
- worker built from repository source on the same machine

Level 0 results observed on real SolidWorks 2022:

- passed: `CON-01`, `CON-02`, `CON-03`
- passed: `SES-01`, `SES-02`, `SES-03`
- passed: `DOC-01`, `DOC-02`
- passed: `IO-01`, `IO-02`
- passed: `DIA-01`, `DIA-02`, `DIA-03`

Validation notes from that run:

- the worker originally executed real actions but emitted almost-empty NDJSON responses until the .NET response serialization was fixed
- the explicit-path save path originally failed with `DISP_E_TYPEMISMATCH` until the late-bound COM invocation marked `Errors` and `Warnings` as `byref`
- `SES-01` / `DIA-03` required a dedicated no-document validation setup because a naive fresh worker launch was not reliable on the first attempt
- this run proves the launch path cleanly; attach-to-existing user-owned sessions was not independently evidenced as a separate pass condition

Required categories:

- connectivity
- session
- document
- save/export
- diagnostics

Not required yet:

- sketch
- feature

## 2026-04-09 company Windows minimal-modeling validation result

Additional real run performed after the Level 0 bootstrap slice:

- worker build still clean on the same machine
- local worker regression tests still passing
- real worker e2e flow validated for:
  - `new_part`
  - `select_plane` on `Front Plane`
  - `start_sketch`
  - `draw_circle`
  - `close_sketch`
  - `extrude_boss`
  - `save_part`

Level 1 evidence from that run:

- passed: `SKT-02`, `SKT-03`
- passed: `FEA-01`, `FEA-02`
- partial evidence: `SKT-01`
  - the worker flow proved `Front Plane`
  - `Top Plane` and `Right Plane` were not independently exercised in the worker e2e run
- not yet separately evidenced: `DIA-04`
- not yet implemented: `IO-03`
- not yet evidenced: `DOC-03`

Validation notes from the minimal-modeling run:

- the first visible 2D geometry path intentionally used `draw_circle`, not `draw_centered_rectangle`
- direct second-process ROT inspection of the worker-launched SolidWorks session was not reliable enough to be the primary external evidence path
- external evidence therefore used saved-part reopen validation
- the reopened slice-2 file exposed `Sketch1:ProfileFeature`
- automatic sketch-segment counting from the reopened file is still not reliable in the current harness
- the successful slice-3 `Boss-Extrude1:Extrusion` run is downstream evidence that the closed sketch profile was real and usable

Support interpretation at that stage:

- Level 0 bootstrap slice: complete and evidenced on real SolidWorks 2022
- Level 1 initial real modeling support: not complete yet
  - reason: export and some remaining validation gaps are still open

## 2026-04-09 company Windows STEP export and DIA-04 validation result

Additional real runs performed after the slice-3 baseline:

- full worker e2e flow validated for:
  - `new_part`
  - `select_plane` on `Front Plane`
  - `start_sketch`
  - `draw_circle`
  - `close_sketch`
  - `extrude_boss`
  - `save_part`
  - `export_step`
- dedicated negative worker validation executed for:
  - `start_sketch` without a selected plane
  - `draw_circle` without an active sketch
  - `extrude_boss` while the sketch was still open

Level 1 evidence added by those runs:

- passed: `IO-03`
- passed: `DIA-04`
- passed: `DOC-03`
  - validation note: this evidence comes from the dedicated reopen probe used by the real harness, not from a worker-side reopen command
- partial evidence remains: `SKT-01`
  - `Front Plane` is proven in the worker e2e flow
  - `Top Plane` and `Right Plane` are still not independently evidenced in the worker e2e flow

Validation notes from the STEP export run:

- `export_step` produced a real STEP file on disk at `C:\SolidWorksMcpValidation\slice4\slice4-export.step`
- the exported file had non-zero size in the captured summary
- the active `.SLDPRT` document path remained stable after export
- the response NDJSON included:
  - `execution.operationDetails.exportKind`
  - `execution.operationDetails.exportMode`
  - `execution.operationDetails.resolvedPath`
  - `execution.operationDetails.solidWorksErrors`
  - `execution.operationDetails.solidWorksWarnings`

Support interpretation at that stage:

- Level 0 bootstrap slice: complete and evidenced on real SolidWorks 2022
- Level 1 initial real modeling support: still not complete
  - reason: plane-selection evidence is still incomplete because `Top Plane` and `Right Plane` have not yet been independently exercised

## 2026-04-09 company Windows plane, attach-existing, and rectangle validation result

Additional real runs performed after the slice-4 baseline:

- separate worker e2e validation for `select_plane` on:
  - `Top Plane`
  - `Right Plane`
- separate attach validation against a pre-existing visible `SLDWORKS.exe` process
- centered-rectangle 2D worker flow validated for:
  - `new_part`
  - `select_plane`
  - `start_sketch`
  - `draw_centered_rectangle`
  - `close_sketch`
  - `save_part`
- centered-rectangle 3D worker flow validated for:
  - `draw_centered_rectangle`
  - `extrude_boss`
  - `save_part`
- circle-based slice-4 `export_step` rerun after those changes still passed

Level 1 evidence added by those runs:

- passed: `SKT-01`
- strengthened: `CON-03`
  - validation note: attach behavior is now separately evidenced even when a pre-existing `SLDWORKS.exe` process is not directly visible through ROT
- strengthened: `FEA-01`
  - validation note: the first minimal native sketch geometry is now proven through both circle and centered-rectangle flows
- strengthened: `FEA-02`
  - validation note: boss extrusion is now proven from both circle and centered-rectangle profiles

Validation notes from those runs:

- on this machine, a visible `SLDWORKS.exe` process can exist while `GetActiveObject(...)` still returns no active ROT object
- the worker therefore now classifies `attached_existing` from observed process reuse during COM activation, not only from direct ROT success
- the real `draw_centered_rectangle` run reported `operationDetails.segmentCount = 6` on this machine
- the normalized worker state still represents that geometry as one `centered_rectangle` entity, which is the intended backend contract
- the rectangle-based saved part reopened with `Sketch1:ProfileFeature`
- the rectangle-based extrusion run reopened with:
  - `Sketch1:ProfileFeature`
  - `Boss-Extrude1:Extrusion`

Current support interpretation:

- Level 0 bootstrap slice: complete and evidenced on real SolidWorks 2022
- Level 1 initial real modeling support: complete and evidenced on real SolidWorks 2022 for the current narrow worker surface
- this is still **not** an official full-v1 support claim
  - reason: wider v1 items such as `add_dimension`, `cut_extrude`, `add_fillet`, assemblies, and drawings are still outside the proven real slice

## 2026-04-09 company Windows draw-line and dimension-probe result

Additional real runs performed after the rectangle baseline:

- dedicated worker e2e validation for:
  - `draw_line`
  - `save_part`
- post-change rerun of the proven slice-4 flow still passing for:
  - `draw_circle`
  - `extrude_boss`
  - `save_part`
  - `export_step`
- bounded `add_dimension` investigation attempted on a horizontal sketch line

Evidence added by those runs:

- strengthened: `FEA-01`
  - validation note: minimal native sketch geometry is now proven through circle, centered rectangle, and line flows
- regression check: `IO-03` still passing after the draw-line and sketch-start hardening changes

Validation notes from those runs:

- the saved draw-line part reopened successfully and exposed `Sketch1:ProfileFeature`
- the line run returned complete NDJSON payloads including `operationDetails.lengthMm`
- during reruns after the dimension investigation, `start_sketch` exposed an intermittent case where `InsertSketch(...)` returned with `ActiveSketch == null`
- a narrow fallback through `EditSketch()` stabilized the proven reruns without widening the worker surface
- the first bounded real `add_dimension` attempt failed with typed classification `unable_to_add_dimension`
  - captured reason: `SelectByID2 could not resolve the target sketch segment.`
- a dedicated probe then showed:
  - `SelectByID2(...)` raised `DISP_E_TYPEMISMATCH`
  - direct `Select2(false, 0)` on the live sketch segment could succeed
  - direct `Select2(...)` on the created line and on both line endpoints could also succeed
  - `AddHorizontalDimension2(...)` could hang the live SolidWorks session instead of returning a clean success or failure
  - `AddDimension2(...)` could fail with `0x800706BE` after a valid direct selection
- additional bounded probe variants then showed:
  - `AddHorizontalDimension2(...)` could also hang after both endpoints were already validly selected
  - `AddVerticalDimension2(...)` could hang on a selected vertical line as well
- a final bounded probe round on 2026-04-10 then showed:
  - typed `IAddHorizontalDimension2(...)` could still hang after endpoint selection
  - typed `IAddVerticalDimension2(...)` could still hang on a selected vertical line
  - typed `IAddDimension2(...)` could still hang after endpoint selection
  - `ISketchManager.AddAlongXDimension(...)` could still hang after endpoint selection
  - `ISketchManager.AddAlongYDimension(...)` could still hang on a selected vertical line
- real harness scenarios must be serialized on this machine
  - parallel runs against the same SolidWorks installation created false negatives and are not valid evidence
- because of that blocker, `add_dimension` is not promoted into the advertised real worker surface
- the real harness now also emits structured `*-inspection.json` artifacts for saved-file/readback evidence
- on 2026-04-10, the current serialized real baseline also passed against the published framework-dependent worker executable
  - run manifest: `C:\SolidWorksMcpValidation\regression-runs\worker-regression-20260410-095421.json`

Support interpretation after those runs:

- Level 0 bootstrap slice: still complete and evidenced on real SolidWorks 2022
- Level 1 initial real modeling support: still complete and evidenced on real SolidWorks 2022 for the current advertised worker surface
- `draw_line`: now inside the proven real slice
- `add_dimension`: still outside the proven real slice

## 2026-04-10 company Windows execution-discipline and packaging result

Additional non-surface work performed after the final bounded dimension decision:

- Windows preflight script implemented and run successfully
- worker build script implemented and run successfully
- worker publish script implemented and run successfully
- direct worker-run script implemented
- real export-only regression wrapper implemented and run successfully
- evidence-bundle collection script implemented and run successfully
- the published framework-dependent worker executable reran the serialized real baseline successfully
- the external harness inspection artifact was enriched with structured file, feature-tree, worker-path, and assertion facts
- the local Windows worker cycle was automated through one repository-side orchestration script

Operational evidence from those runs:

- `scripts\validate-windows-environment.ps1` passed on the company machine
- `scripts\build-solidworks-worker.ps1` passed on the company machine
- `scripts\publish-solidworks-worker.ps1` passed on the company machine
- `scripts\run-solidworks-worker.ps1` passed a minimal wrapper smoke check for both:
  - debug build worker
  - published framework-dependent worker
- `scripts\run-real-worker-regression.ps1` passed against:
  - the debug worker path
  - the published framework-dependent worker path
- `scripts\run-real-export-regression.ps1` passed against the published worker path
- `scripts\collect-real-worker-evidence.ps1` produced a copied evidence bundle under:
  - `C:\Tools\SOLIDWORKS-MCP\artifacts\real-evidence\`
- `scripts\run-windows-worker-cycle.ps1` passed on the company machine for:
  - preflight
  - debug build plus worker tests
  - framework-dependent publish
  - wrapper smoke check
  - serialized real regression on the published worker
  - evidence bundle collection

Validation notes from those runs:

- this milestone does not widen the CAD command surface
- it strengthens repeatability, packaging discipline, and evidence readability around the already-proven Level 1 slice
- `backendMetadata.sliceName` was aligned to `level1-real-modeling-v1` to reflect the current proven worker milestone more honestly
- after the packaging/readback hardening changes, the published worker reran the full serialized baseline successfully again
  - run manifest: `C:\SolidWorksMcpValidation\regression-runs\worker-regression-20260410-100848.json`
- after the inspection/readback and automation hardening changes, the full Windows cycle also passed again
  - run manifest: `C:\SolidWorksMcpValidation\regression-runs\worker-regression-20260410-104605.json`
  - cycle manifest: `C:\Tools\SOLIDWORKS-MCP\artifacts\worker\cycles\20260410-104556\windows-worker-cycle.json`

## 2026-04-10 company Windows full-evidence cycle result

Additional operational validation performed after the first scripted cycle milestone:

- evidence collector rerun in default `standard_minimal` mode
- evidence collector rerun in `full_with_cad_files` mode
- full Windows cycle rerun with:
  - framework-dependent published worker
  - serialized real regression
  - evidence bundle in `full_with_cad_files` mode

Operational evidence from those runs:

- `scripts\collect-real-worker-evidence.ps1` passed locally in:
  - `standard_minimal`
  - `full_with_cad_files`
- `scripts\run-windows-worker-cycle.ps1 -IncludeCadFiles` passed on the company machine
- the full-evidence cycle produced:
  - regression run manifest: `C:\SolidWorksMcpValidation\regression-runs\worker-regression-20260410-140512.json`
  - cycle manifest: `C:\Tools\SOLIDWORKS-MCP\artifacts\worker\cycles\20260410-140501\windows-worker-cycle.json`
  - cycle summary: `C:\Tools\SOLIDWORKS-MCP\artifacts\worker\cycles\20260410-140501\windows-worker-cycle-summary.txt`
  - evidence manifest: `C:\Tools\SOLIDWORKS-MCP\artifacts\real-evidence\20260410-141018\evidence-manifest.json`
  - evidence summary: `C:\Tools\SOLIDWORKS-MCP\artifacts\real-evidence\20260410-141018\evidence-summary.txt`

Validation notes from those runs:

- this milestone still does not widen the CAD command surface
- it distinguishes clearly between routine lighter evidence collection and fuller CAD-artifact preservation
- the `full_with_cad_files` mode is now not only designed and implemented, but also evidenced on the company SolidWorks 2022 machine
- the default automation recommendation remains the standard cycle without copied CAD files, with `-IncludeCadFiles` reserved for fuller evidence retention

### Level 1: Initial real SolidWorks 2022 workflow support

This is the minimum bar for claiming **initial real modeling support**.

Required categories:

- connectivity
- session
- document
- sketch
- feature
- save/export
- diagnostics

## Minimum tests required for an initial support claim

The following tests must pass on real SolidWorks 2022 before claiming initial real support for the v1 modeling workflow:

- connectivity: attach or launch SolidWorks successfully
- diagnostics: report worker identity, protocol compatibility, and detected SolidWorks version
- session: report session status with no active document and with an active document
- document: create a new part document through the worker
- sketch: select a standard plane and open/close a sketch
- feature: create at least one minimal boss extrusion from native sketch geometry
- save/export: save the part and export STEP successfully, with output-file verification
- diagnostics: return typed failures for missing preconditions

## Test classification

## Connectivity

| ID | Test | Required For | Expected result |
| --- | --- | --- | --- |
| `CON-01` | Worker handshake succeeds | Level 0 | Protocol version matches and worker identity is returned |
| `CON-02` | Worker advertises SolidWorks 2022 support | Level 0 | `supportedSolidWorksMajorVersions` includes `2022` |
| `CON-03` | Worker can attach to or launch SolidWorks | Level 0 | SolidWorks session becomes available without crashing the worker, and execution metadata reports session facts |

## Session

| ID | Test | Required For | Expected result |
| --- | --- | --- | --- |
| `SES-01` | No-document session status | Level 0 | Worker reports connected session with no active document through failure details or execution metadata |
| `SES-02` | Active-document session status | Level 0 | After `new_part`, worker reports an active part document through normalized state and execution metadata |
| `SES-03` | Clean worker shutdown | Level 0 | Worker exits cleanly without corrupting protocol; it may release COM references without auto-closing SolidWorks |

## Document

| ID | Test | Required For | Expected result |
| --- | --- | --- | --- |
| `DOC-01` | Create new part | Level 0 | A new part document is created and reflected in normalized state |
| `DOC-02` | Document metadata readback | Level 0 | Name/type/path metadata are returned consistently |
| `DOC-03` | Reopen saved part | Level 1 | Saved part can be reopened and becomes the active document |

## Sketch

| ID | Test | Required For | Expected result |
| --- | --- | --- | --- |
| `SKT-01` | Select standard plane | Level 1 | `Front Plane`, `Top Plane`, and `Right Plane` selection succeeds |
| `SKT-02` | Start sketch | Level 1 | Sketch opens on selected plane and state updates correctly |
| `SKT-03` | Close sketch | Level 1 | Sketch exits cleanly and active-sketch state clears correctly |

## Feature

| ID | Test | Required For | Expected result |
| --- | --- | --- | --- |
| `FEA-01` | Draw minimal native sketch geometry | Level 1 | At least one minimal profile can be created reliably |
| `FEA-02` | Boss extrusion | Level 1 | A blind boss extrusion succeeds from a closed sketch |
| `FEA-03` | Cut extrusion | Later milestone | Cut extrusion succeeds from a closed sketch |
| `FEA-04` | Fillet | Later milestone | Fillet succeeds on a known test feature |

## Save / export

| ID | Test | Required For | Expected result |
| --- | --- | --- | --- |
| `IO-01` | Save new part to explicit `.sldprt` path | Level 0 | API succeeds, target file exists afterward, and readback path matches the requested `.sldprt` |
| `IO-02` | Save active existing part in place | Level 0 | Save succeeds without changing path semantics and the file still exists afterward |
| `IO-03` | Export STEP | Level 1 | API succeeds and target `.step` file exists afterward |

## Diagnostics

| ID | Test | Required For | Expected result |
| --- | --- | --- | --- |
| `DIA-01` | Worker version info | Level 0 | Worker reports its own identity/version consistently |
| `DIA-02` | SolidWorks version info | Level 0 | Worker reports detected SolidWorks major version and it is `2022` |
| `DIA-03` | Typed no-document failure | Level 0 | Commands that require a document fail with typed, deterministic error payloads |
| `DIA-04` | Typed precondition failure for sketch/feature commands | Level 1 | Commands fail deterministically when plane/sketch/feature state is missing |

## Environment prerequisites for the company Windows machine

- Windows 10 or Windows 11
- Real SolidWorks 2022 installation, licensed and activated
- SolidWorks launched at least once under the test user profile
- Default part template configured in SolidWorks options
- Write access to a deterministic test output directory
- Interactive desktop session available for COM automation
- .NET runtime/toolchain available if the worker is launched from source
- Same Windows user account used both for SolidWorks and for the MCP worker session

Recommended additional prerequisites:

- test directory outside cloud-sync folders
- local antivirus or DLP policy checked for `.sldprt` / `.step` writes
- no modal SolidWorks startup prompts left pending
- PowerShell or an equivalent shell available to run the slice-1 worker checklist

## Tests to run immediately

These belong to the **first real worker slice** and should be executed first:

- `CON-01` Worker handshake succeeds
- `CON-02` Worker advertises SolidWorks 2022 support
- `CON-03` Worker can attach to or launch SolidWorks
- `SES-01` No-document session status
- `SES-02` Active-document session status
- `SES-03` Clean worker shutdown
- `DOC-01` Create new part
- `DOC-02` Document metadata readback
- `IO-01` Save new part to explicit `.sldprt` path
- `IO-02` Save active existing part in place
- `DIA-01` Worker version info
- `DIA-02` SolidWorks version info
- `DIA-03` Typed no-document failure

Practical execution note:

- until dedicated diagnostics tools exist, collect Level 0 evidence from:
  - raw worker handshake output
  - `execute_command_response.execution`
  - `execute_command_response.error.details`
  - normalized `nextState` / `observedState`

See also:

- [WINDOWS_2022_SLICE1_CHECKLIST.md](/Users/guidopacciani/Documents/SOLIDWORKS/docs/architecture/WINDOWS_2022_SLICE1_CHECKLIST.md)

## Tests to defer to later milestones

These should wait until the bootstrap slice is proven:

- `DOC-03` Reopen saved part
- `SKT-01` Select standard plane
- `SKT-02` Start sketch
- `SKT-03` Close sketch
- `FEA-01` Draw minimal native sketch geometry
- `FEA-02` Boss extrusion
- `FEA-03` Cut extrusion
- `FEA-04` Fillet
- `IO-03` Export STEP
- `DIA-04` Typed precondition failures for sketch/feature commands

## Immediate recommendation

Do **not** claim “SolidWorks 2022 support” after only the first worker slice.

After slice 1, the honest status is closer to:

- real SolidWorks 2022 connectivity and document bootstrap: validated
- real SolidWorks 2022 sketch/feature workflow: not yet validated

That distinction should remain explicit in all project status reporting.
