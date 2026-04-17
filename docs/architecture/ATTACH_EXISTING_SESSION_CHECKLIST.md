# ATTACH EXISTING SESSION CHECKLIST

## Purpose

Capture the minimum real validation needed to prove that the worker can attach to a SolidWorks session that already exists before the worker starts.

## Scope

This validation is intentionally narrow:

- existing SolidWorks process already alive before worker launch
- worker `handshake`
- `new_part`
- `save_part`
- `shutdown`

This checklist does **not** claim:

- general session introspection tools
- attach behavior for assemblies or drawings
- attach behavior for every possible SolidWorks startup condition

## 2026-04-09 company Windows evidence

Environment:

- Windows 11 build `26200`
- `.NET SDK 8.0.419`
- SolidWorks 2022 revision `30.4.0`

Primary evidence files from the real run:

- `C:\SolidWorksMcpValidation\attach-existing\attach-existing-sequence.ndjson`
- `C:\SolidWorksMcpValidation\attach-existing\attach-existing-sequence.out.ndjson`
- `C:\SolidWorksMcpValidation\attach-existing\attach-existing-summary.txt`
- `C:\SolidWorksMcpValidation\attach-existing\attach-existing-part.SLDPRT`

## Validated flow

The validated worker flow was:

1. detect a pre-existing visible `SLDWORKS.exe` process before worker launch
2. `handshake_request`
3. `new_part`
4. `save_part`
5. `shutdown`

## What passed

- the worker connected successfully to SolidWorks 2022
- `execution.session.connectionMode` was `attached_existing`
- the worker created and saved a real part without crashing the pre-existing session
- the saved `.SLDPRT` file existed on disk after the run

## What is implemented vs proven

- implemented:
  - attach-first bootstrap path
- compiled locally:
  - yes
- tested locally:
  - yes
- tested on real SolidWorks 2022:
  - yes for a pre-existing visible SolidWorks process that was already alive before worker start

## Validation notes

- on this machine, a visible `SLDWORKS.exe` process may exist without being directly discoverable through ROT.
- because of that, the worker now classifies `attached_existing` not only from direct ROT success, but also from the observed reuse of an already-running `SLDWORKS.exe` process during COM activation.
- this checklist proves the runtime attach path for the current baseline machine and slice.

## Exit criteria

Attach-existing should be considered passed only when all of the following are true:

- a SolidWorks process exists before worker launch
- the worker returns `connectionMode = attached_existing`
- `new_part` succeeds
- the saved file exists on disk
- the worker shuts down cleanly
