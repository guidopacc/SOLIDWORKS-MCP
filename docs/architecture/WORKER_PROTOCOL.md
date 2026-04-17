# WORKER PROTOCOL

## Purpose

This document defines the internal protocol between the TypeScript MCP core and the Windows/.NET SolidWorks worker.

## Transport choice

- Framing: newline-delimited JSON
- Ownership: internal protocol, not exposed as MCP
- Current compatibility rule: exact `protocolVersion` match

## Message families

### Handshake

- `handshake_request`
- `handshake_response`

Used to verify:

- protocol compatibility
- worker identity
- advertised SolidWorks major-version support
- worker capabilities
- backend metadata and currently supported command kinds

### Command execution

- `execute_command_request`
- `execute_command_response`

The request carries:

- the normalized CAD command
- the full `stateBefore`
- the desired SolidWorks baseline version
- an optional timeout

The response returns either:

- `ok: true` with `nextState` and execution metadata
- `ok: false` with a typed error, optional `observedState`, and `resyncRequired`

### Shutdown

- `shutdown_request`
- `shutdown_response`

Used for orderly worker termination.

## Why the request includes `stateBefore`

- The worker can validate preconditions against the orchestrator's view of state.
- The mock and real backends can share the same semantic contract.
- Future recovery logic can compare expected state with observed state.

## Why failures may include `observedState`

Real SolidWorks execution can partially mutate a document before failing. The boundary therefore reserves room for:

- `observedState`
- `resyncRequired`

The current TypeScript orchestration does not automatically apply observed state on failure, but the protocol is already designed for that future recovery step.

## Current command surface in the real worker

The worker currently advertises and implements:

- `new_part`
- `select_plane`
- `start_sketch`
- `draw_line`
- `draw_circle`
- `draw_centered_rectangle`
- `close_sketch`
- `extrude_boss`
- `save_part`
- `export_step`

Notes:

- `backendMetadata.sliceName` now reports `level1-real-modeling-v1`, matching the currently proven worker milestone.
- `draw_line` is now part of the real worker surface and has real SolidWorks 2022 validation for a minimal 2D saved-sketch flow.
- the first real visible-geometry path intentionally uses `draw_circle` because it is the most robust path validated on SolidWorks 2022 so far.
- `draw_centered_rectangle` is now part of the worker protocol surface and has separate real validation on SolidWorks 2022.
- `export_step` is intentionally narrow in the current worker slice:
  - only part documents are exportable
  - only `.step` / `.stp` targets are supported
- `add_dimension` remains outside the advertised real worker surface for now:
  - a bounded real investigation was attempted
  - the first attempt failed because the target sketch segment could not be selected reliably
  - follow-up probes then showed that multiple dimension-creation entry points remain unsafe on the baseline machine
  - the current milestone therefore keeps `add_dimension` frozen outside the real worker surface

## Serialization rule

Worker responses must be serialized from the concrete runtime response type, not just from the abstract base type.

Reason:

- the 2026-04-09 real Windows validation found a real bug where SolidWorks actions were succeeding but NDJSON payloads were nearly empty
- the fix was to serialize concrete runtime response objects and keep the protocol DTOs public

## Current implementation status

- TypeScript protocol types: implemented
- TypeScript worker-backed backend wrapper: implemented
- TypeScript stdio launcher/transport: implemented
- Runtime backend resolver: implemented
- .NET worker: implemented
- Local .NET regression tests: implemented for the two real slice-1 bugs
- Real SolidWorks execution:
  - implemented for the Level 0 bootstrap slice
  - implemented for the first minimal visible-modeling slice
- Windows validation:
  - executed on the company Windows machine on `2026-04-09`
  - Level 0 bootstrap slice validated on real SolidWorks 2022
  - minimal modeling flow validated on real SolidWorks 2022 for:
    - `select_plane` on `Front Plane`
    - `select_plane` on `Top Plane`
    - `select_plane` on `Right Plane`
    - `start_sketch`
    - `draw_line`
    - `draw_circle`
    - `draw_centered_rectangle`
    - `close_sketch`
    - `extrude_boss`
    - `save_part`
    - `export_step`
  - attach-to-existing-session behavior validated on real SolidWorks 2022
  - published framework-dependent worker executable also validated on real SolidWorks 2022 through the current serialized baseline

## Honesty rule

Protocol support claims must still distinguish clearly between:

- implemented
- compiled locally
- tested locally
- tested on real SolidWorks 2022
- officially supported

The current protocol and worker now cover a complete Level 1 initial real-modeling slice, but they are still not an official full-v1 support claim.
