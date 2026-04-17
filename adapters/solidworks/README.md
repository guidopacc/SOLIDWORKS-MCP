# SolidWorks Adapter

This directory is reserved for the real Windows backend that will communicate with SolidWorks.

## Status

- Designed: yes
- Implemented: partially
- Tested in mock: not applicable
- Tested on real SolidWorks 2022: no
- Officially supported: no

## Intended direction

- Language/runtime: C# / .NET on Windows
- Integration style: out-of-process worker dedicated to COM and STA ownership
- Boundary reference: `docs/architecture/WINDOWS_BACKEND_BOUNDARY.md`
- Concrete protocol reference: `docs/architecture/WORKER_PROTOCOL.md`

## Why this is separate from the MCP server

- The MCP protocol layer should stay portable and testable without SolidWorks.
- COM and Windows lifecycle concerns should not leak into tool parsing or document state management.
- The mock backend and the future real backend must satisfy the same abstract CAD contract.

## What exists now

- `src/worker-protocol.ts`: versioned internal worker contract
- `src/solidworks-worker-backend.ts`: TypeScript backend wrapper that talks to a worker transport
- `src/stdio-worker-transport.ts`: stdio launcher/transport for a spawned worker process
- `dotnet/SolidWorksWorker/`: first .NET worker scaffold with handshake and stub execution responses

## What does not exist yet

- Live COM automation against SolidWorks
- Real Windows executable wiring and validation of the .NET worker against SolidWorks
- Real Windows validation
