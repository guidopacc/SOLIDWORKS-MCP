# WINDOWS BACKEND BOUNDARY

## Intent

Define the clean boundary between the TypeScript MCP core and the future real SolidWorks backend running on Windows.

## Proposed shape

- MCP server remains in TypeScript
- Real CAD adapter runs as a dedicated .NET process on Windows
- The .NET worker owns SolidWorks COM interaction and STA constraints
- Communication occurs through an explicit IPC boundary
- The current repository baseline uses newline-delimited JSON for that internal worker protocol

## Why not embed COM directly in the MCP server

- COM lifecycle concerns should not leak into tool parsing
- Testing without SolidWorks would become far harder
- Error recovery and process restarts are cleaner with a dedicated worker

## Minimum contract responsibilities of the Windows worker

- Accept normalized CAD commands
- Execute SolidWorks 2022 operations
- Return typed success/failure envelopes
- Map SolidWorks runtime details into the repository’s normalized state model
- Report version/build information for later support tracking

## Key design concerns to verify later

- Process startup and reconnection behavior
- STA threading ownership
- Session reuse versus one-shot execution
- Recovery after a failed COM call
- Timeout and cancellation policy for long operations

## Current status

- Designed at boundary level: yes
- Implemented: partially, as protocol, stdio launcher, runtime resolver, and worker scaffold
- Tested on Windows: no

## Related documents

- `docs/architecture/WORKER_PROTOCOL.md`
