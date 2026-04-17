# OPEN DOCUMENT POLICY

## Current decision

Do not expose `open_document` or `reopen_document` in the real worker surface yet.

## Why

- the current real slice is still intentionally narrow and validation-backed
- the harness can already reopen saved `.SLDPRT` files externally for evidence without widening the product surface
- a real worker-side open/reopen flow would need honest handling for:
  - local path validation
  - active-document replacement semantics
  - session attach/launch interactions
  - deterministic error mapping for missing files and blocked paths
- none of that is required to preserve the currently proven modeling baseline

## What is allowed today

- external reopen by the validation harness
- saved-part inspection outside the worker process

## What is not claimed today

- user-facing worker support for opening an existing part from path
- worker support for reopening the last saved part
- support for assembly or drawing open flows

## Review trigger

Revisit this policy only when one of these becomes true:

- there is a concrete product need for part reuse across sessions
- the current sketch-authoring baseline is stable enough that document workflow becomes the next highest-value slice
- the worker has a clearer recovery strategy for open-document failures

## 2026-04-10 review note

This policy was reviewed again after the final bounded `add_dimension` probe round and after readback hardening in the external harness.

Current outcome:

- no change
- the project still gains more value from stronger validation/readback discipline than from widening the worker surface with document-open commands
