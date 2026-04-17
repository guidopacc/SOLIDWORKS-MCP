# WINDOWS 2022 SLICE 1 CHECKLIST

## Purpose

Provide a practical, repeatable validation checklist for the first real Windows/.NET worker slice on a company machine with SolidWorks 2022.

This checklist is intentionally limited to:

- handshake
- worker version info
- backend metadata
- session status
- SolidWorks version info
- `new_part`
- `save_part`
- typed no-document failure

It does **not** validate sketch or feature workflows yet.

## Preconditions

- Windows 10 or Windows 11
- SolidWorks 2022 installed, licensed, and launched once under the test user
- `.NET 8` SDK or runtime available
- default part template configured in SolidWorks
- writable local directory for test outputs
- no modal SolidWorks prompts left open

Recommended test output directory:

- `C:\SolidWorksMcpValidation\slice1\`

## Build step

From the repository root:

```powershell
dotnet build .\adapters\solidworks\dotnet\SolidWorksWorker\SolidWorksWorker.csproj
```

Record:

- build success or failure
- exact compiler/runtime version
- any warnings that look related to COM or target framework

## Launch step

Start the worker directly:

```powershell
dotnet run --project .\adapters\solidworks\dotnet\SolidWorksWorker\SolidWorksWorker.csproj
```

Keep the worker process running in that terminal for the following raw NDJSON checks.

## Check 1: Handshake

Send:

```json
{"messageType":"handshake_request","requestId":"check-1","protocolVersion":"0.1.0","client":{"name":"manual-validator","version":"0.1.0"},"desiredSolidWorksMajorVersion":2022}
```

Verify:

- `messageType` is `handshake_response`
- `protocolVersion` is `0.1.0`
- `worker.name` is present
- `worker.version` is present
- `supportedSolidWorksMajorVersions` includes `2022`
- `backendMetadata.sliceName` is `real-bootstrap-v1`
- `backendMetadata.solidWorksProgIdRegistered` is truthful for that machine

Matrix coverage:

- `CON-01`
- `CON-02`
- `DIA-01`

## Check 2: Typed no-document failure

Start from a SolidWorks session with no active document if possible.

Validation note from the 2026-04-09 company Windows run:

- do **not** assume a freshly launched SolidWorks instance satisfies this precondition
- on that machine, a worker-launched session could start with an active blank/restored document
- validate this check against an explicitly prepared no-document session or an equivalent harness

Send:

```json
{"messageType":"execute_command_request","requestId":"check-2","protocolVersion":"0.1.0","command":{"kind":"save_part","path":"C:\\SolidWorksMcpValidation\\slice1\\no-document-test.sldprt"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":[],"currentDocument":null},"desiredSolidWorksMajorVersion":2022}
```

Verify:

- response is `execute_command_response`
- `ok` is `false`
- `error.code` is `precondition_failed`
- `error.details.classification` is `no_active_document`
- `error.details.session.connected` is present
- `error.details.session.solidWorksMajorVersion` is `2022`

Matrix coverage:

- `SES-01`
- `DIA-02`
- `DIA-03`

## Check 3: Create new part

Send:

```json
{"messageType":"execute_command_request","requestId":"check-3","protocolVersion":"0.1.0","command":{"kind":"new_part","name":"Slice1Validation"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["new_part","save_part"],"currentDocument":null},"desiredSolidWorksMajorVersion":2022}
```

Verify:

- `ok` is `true`
- `nextState.currentDocument.documentType` is `part`
- `nextState.currentDocument.name` is present
- `execution.session.connected` is `true`
- `execution.session.hasActiveDocument` is `true`
- `execution.session.activeDocumentKind` is `part`
- `execution.solidWorksMajorVersion` is `2022`
- `execution.operationDetails.templatePath` is populated

Matrix coverage:

- `CON-03`
- `SES-02`
- `DOC-01`
- `DOC-02`
- `DIA-02`

## Check 4: Save new part to explicit path

Send:

```json
{"messageType":"execute_command_request","requestId":"check-4","protocolVersion":"0.1.0","command":{"kind":"save_part","path":"C:\\SolidWorksMcpValidation\\slice1\\slice1-part.sldprt"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["new_part","save_part"],"currentDocument":{"documentType":"part","name":"Slice1Validation","units":"mm","sketches":[],"features":[],"exports":[],"modified":false,"baselineVersion":"2022"}},"desiredSolidWorksMajorVersion":2022}
```

Verify:

- `ok` is `true`
- output file exists on disk
- `nextState.currentDocument.savedPath` matches the requested path
- `nextState.currentDocument.modified` is `false`
- `execution.operationDetails.saveMode` is present
- `execution.operationDetails.solidWorksErrors` is `0` or otherwise understood and documented

Matrix coverage:

- `IO-01`
- `DOC-02`

## Check 5: Save existing part in place

Repeat the save with the same `.sldprt` path as the already-saved active document.

Verify:

- `ok` is `true`
- file still exists
- `execution.operationDetails.saveMode` indicates the in-place path
- no unexpected path change occurs in `nextState.currentDocument.savedPath`

Matrix coverage:

- `IO-02`

## Check 6: Clean shutdown

Send:

```json
{"messageType":"shutdown_request","requestId":"check-6","protocolVersion":"0.1.0"}
```

Verify:

- response is `shutdown_response`
- worker process exits cleanly
- SolidWorks remains in a safe state
- if the worker attached to an existing session, that session is still open

Matrix coverage:

- `SES-03`

## Evidence to capture

For each run, store:

- machine identifier or role
- Windows version
- SolidWorks version and service pack if visible
- `.NET` SDK/runtime version
- raw request/response transcript
- screenshot or file listing proving `slice1-part.sldprt` exists
- any HRESULT, warning code, or modal dialog encountered

## Exit criteria

This checklist is complete only when:

- the worker builds on Windows
- all checks above run to completion
- failures, if any, are recorded against the validation matrix
- no unsupported scope was pulled in during the run
