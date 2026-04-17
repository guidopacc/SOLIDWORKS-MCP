# FIRST REAL WORKER SLICE

## Purpose

Define the smallest real Windows/.NET worker increment that is worth implementing next.

This slice is intentionally narrow. Its job is not to prove modeling. Its job is to prove that the backend boundary can:

- handshake honestly
- talk to a real SolidWorks 2022 installation
- report version/session facts
- create a new part
- save a file reliably

If this slice is not stable, adding sketch and feature logic would only amplify uncertainty.

Historical note:

- this document remains the slice-1 boundary definition
- the repository has now moved beyond it
- slice 1 is currently frozen by local regression tests plus the real Windows evidence recorded on `2026-04-09`

## Current implementation status

Repository status after this session:

- implemented:
  - handshake response with worker identity and backend metadata
  - late-bound COM attach-first session bootstrap in the Windows worker
  - SolidWorks version detection through `RevisionNumber()`
  - `new_part` through default-template resolution plus `NewDocument(...)`
  - `save_part` through `Save3` for in-place saves and `SaveAs4` for explicit paths
  - post-save file existence verification
  - structured worker-side error mapping for bootstrap/document/save failures
- compiled locally on the company Windows machine:
  - `dotnet build` passed with `.NET SDK 8.0.419`
- locked by targeted local regression tests:
  - concrete-runtime NDJSON response serialization
  - late-bound `Save3` / `SaveAs4` `byref` handling
- tested locally on the company Windows machine:
  - handshake
  - worker version info
  - backend metadata
  - session/version facts
  - `new_part`
  - `save_part` to explicit path
  - `save_part` in place
  - typed no-document failure
  - clean `shutdown`
- validated on real SolidWorks 2022 for the Level 0 bootstrap slice:
  - confirmed against SolidWorks 2022 revision `30.4.0`
  - launch-path bootstrap is proven
  - no-document validation required an explicitly prepared no-document setup because a naive fresh launch was not reliable on that workstation class

## Slice-specific assumptions and limits

- The worker uses late-bound COM intentionally in slice 1, so it does not yet depend on SolidWorks primary interop assemblies.
- Default-part creation assumes that SolidWorks exposes a valid default part template through `GetUserPreferenceStringValue(swDefaultTemplatePart)`.
- The normalized repository state still supports only `mm`, so the worker currently maps part state into millimeters and this assumption must be validated on the target template.
- Active-document kind detection is intentionally conservative in slice 1:
  - it prefers path-based inference when a path exists
  - for the `new_part` and `save_part` success paths it coerces the result to `part`
  - broader document-type introspection should wait until the worker is validated on Windows
- Worker shutdown currently releases COM references but does **not** auto-close SolidWorks. This is deliberate to avoid accidentally terminating an attached user session.

## Scope of the first real slice

### Included

- worker handshake
- worker identity / version
- detected SolidWorks version info
- session status:
  - connected or not
  - active document present or not
  - active document type/name/path when available
- `new_part`
- minimal `save_part`

### Explicitly excluded

- plane selection
- sketch start / close
- sketch entities
- dimensions / constraints
- boss extrude
- cut extrude
- fillet
- STEP export
- assemblies
- drawings
- macro generation / execution
- advanced validation loops

## Why this is the right slice

The code audit shows that the most expensive early failures in public SolidWorks MCP projects happen when:

- runtime COM fragility is mixed with modeling scope too early
- save/export verification is weak
- advanced feature operations are used to prove a backend that has not yet proven session/document basics

This first slice avoids that trap.

## Proposed worker responsibilities in slice 1

### Handshake and baseline metadata

The worker should already continue to return:

- worker identity
- worker version
- supported SolidWorks major versions

For slice 1, the worker should additionally ensure that runtime metadata can be surfaced through:

- handshake response
- execution metadata
- or cached query-side project/document status

without inventing a second protocol that duplicates MCP.

### Session bootstrap

The worker should:

1. initialize its COM/STA ownership correctly
2. try to attach to an existing SolidWorks session
3. fall back to launching/creating a SolidWorks application instance if needed
4. make the session available for subsequent commands

### Document bootstrap

The worker should implement:

- `new_part`
- internal readback of active document metadata

`new_part` should use a template-aware path, not a brittle hardcoded template assumption.

### Minimal save

The worker should implement:

- `save_part` to an explicit target path
- save-in-place for an already-saved document if feasible in the same pass

For v1 slice quality, save must verify that the target file exists after the operation.

Current implementation note:

- explicit-path save uses `SaveAs4(...)` in this first late-bound slice to keep the save surface small and verifiable
- in-place save uses `Save3(...)`
- the worker currently accepts only `.sldprt` save targets in this slice

## Proposed technical flow

## 1. Worker startup

- worker process starts
- worker enters the thread model required for SolidWorks COM
- worker waits for NDJSON requests

## 2. Handshake

- TypeScript core sends `handshake_request`
- worker returns:
  - worker identity
  - protocol version
  - supported SolidWorks versions
  - worker capabilities

This proves protocol compatibility, not yet runtime connectivity.

## 3. First real command path

Recommended order:

1. receive `new_part`
2. ensure SolidWorks session exists
3. resolve default part template from SolidWorks/user preferences
4. create a new part document through `NewDocument(template, 0, 0, 0)`
5. translate resulting state into normalized `ProjectRuntimeState`

## 4. Save command path

Recommended order:

1. receive `save_part`
2. verify there is an active part document
3. normalize target path
4. ensure output directory exists
5. call SolidWorks save API
6. verify output file exists
7. update normalized state with `savedPath` and `modified=false`

## Dependencies

- existing NDJSON worker protocol
- current TypeScript worker transport
- current normalized `ProjectRuntimeState`
- Windows machine with SolidWorks 2022
- template configuration available in that SolidWorks installation

Optional but helpful:

- small internal .NET service split:
  - `SolidWorksSessionService`
  - `SolidWorksDocumentService`
  - `WorkerCommandDispatcher`

## Recommended implementation shape

Even for this small slice, avoid putting all logic in `Program.cs`.

Recommended internal split:

- `Program.cs`
  - protocol loop only
- `SolidWorksSessionService`
  - attach / launch
  - version readback
  - session status
- `SolidWorksDocumentService`
  - new part
  - active document readback
  - save
- `WorkerStateMapper`
  - map SolidWorks runtime facts into normalized state

This is still small enough for a first pass, but avoids immediately creating a “god file”.

## How this slice fits the current contract

The current worker protocol already supports:

- handshake
- command execution
- typed success/failure envelopes
- execution metadata

No broad protocol redesign is required for slice 1.

The minimum useful command support in the worker is:

- `new_part`
- `save_part`

For session and version visibility, prefer:

- handshake metadata
- execution metadata
- existing query tools (`get_document_state`, `get_project_status`) enriched later if needed

rather than expanding the command surface prematurely.

## Risks

### Risk 1: template resolution fails on the target machine

Mitigation:

- read default template settings from SolidWorks preferences
- fail with a typed, explicit configuration error

### Risk 2: attach/launch semantics differ across workstation setups

Mitigation:

- distinguish clearly between:
  - attached to existing session
  - launched new session
- report this in execution metadata or diagnostics

### Risk 3: save API reports success ambiguously

Mitigation:

- verify file existence after save
- capture SolidWorks error/warning values where available

### Risk 4: protocol success without meaningful state change

Mitigation:

- require normalized state updates after `new_part` and `save_part`
- treat “success with no active document state” as incomplete

### Risk 5: expanding scope too early

Mitigation:

- explicitly reject sketch/feature commands as `unsupported_operation` in slice 1

## Completion criteria

This slice is complete only if all of the following are true on a real Windows/SolidWorks 2022 machine:

- handshake succeeds
- SolidWorks 2022 version information is detected and reported
- session status is known with and without an active document
- `new_part` succeeds and updates normalized state
- `save_part` succeeds to an explicit `.sldprt` path
- saved file exists on disk after command completion
- no-document save failure is returned as a typed error

For the repository state alone, “implemented” now means the worker code exists for this slice.
It still does **not** mean compiled, locally tested, or validated on a real SolidWorks 2022 installation.

## Non-completion criteria

This slice is **not** complete if any of the following is still true:

- worker can handshake but cannot prove real SolidWorks connectivity
- `new_part` works only with a hardcoded machine-specific template path
- `save_part` returns success without output-file verification
- active document metadata cannot be mapped into normalized state
- failures still come back as generic, opaque runtime errors
- the implementation only works by manually driving SolidWorks UI between calls

## Minimum associated tests

The slice should be validated against the following matrix IDs:

- `CON-01`
- `CON-02`
- `CON-03`
- `SES-01`
- `SES-02`
- `SES-03`
- `DOC-01`
- `DOC-02`
- `IO-01`
- `IO-02`
- `DIA-01`
- `DIA-02`
- `DIA-03`

## What should happen immediately after slice 1

Only after this slice is stable should the next real backend increment begin:

1. plane selection
2. sketch start / close
3. minimal sketch geometry
4. boss extrusion

That order preserves evidence quality and keeps the backend rollout measurable.
