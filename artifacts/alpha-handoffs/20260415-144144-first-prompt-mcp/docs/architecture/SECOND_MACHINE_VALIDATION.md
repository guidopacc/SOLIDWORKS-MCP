# SECOND MACHINE VALIDATION

## Purpose

Define the shortest honest path for validating `SOLIDWORKS-MCP` on a second developer machine or, if a second machine is not available yet, on a second checkout path that still exposes setup friction.

## What this should prove

- a technical developer can follow the repo-first docs without historical project memory
- the repo can be built and published outside the original baseline checkout path
- an MCP client can see the current 13-tool public alpha surface from that checkout
- setup errors can be distinguished from worker or SolidWorks environment errors

## What this does not prove

- installer-grade packaging
- Claude Desktop extension packaging
- support beyond the current public alpha surface
- broad validation across multiple SolidWorks versions

## Required prerequisites

- Windows 10 or Windows 11
- Node.js `^20.19.0 || >=22.12.0`
- npm
- .NET 8 SDK/runtime
- SolidWorks 2022 installed
- SolidWorks launched at least once under the target Windows user
- default part template configured in SolidWorks
- interactive desktop session available
- local repository checkout outside cloud-synced folders if possible

## Recommended checkout shape

Use a local path that is clearly not the historical baseline path, for example:

- `C:\Dev\SOLIDWORKS-MCP`
- `D:\Work\SOLIDWORKS-MCP`

This makes path assumptions show up quickly.

## Validation sequence

From the repository root:

```powershell
npm install
npm run build
npm test
.\scripts\validate-windows-environment.ps1
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

Then:

1. confirm the published worker exists at `.\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe`
2. configure one serious MCP client using `configs/examples/` and `docs/architecture/MCP_CLIENT_SETUP.md`
3. reload the client MCP servers
4. ask the client to list available tools
5. ask for `get_project_status`
6. ask for `get_document_state`
7. only then run one small safe modeling flow

## Expected minimum outputs

- `dist\mcp-server\src\index.js`
- `.\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe`
- `C:\SolidWorksMcpValidation\preflight\worker-preflight.json`
- 13 public alpha tools visible through MCP

## Expected safe first flow

- "Create a new part, select the Front Plane, start a sketch, draw a circle centered at the origin with radius 10 mm, close the sketch, extrude 10 mm, save the part to `C:\\Temp\\alpha-second-machine.SLDPRT`, then export it to `C:\\Temp\\alpha-second-machine.step`."

## Failure classification

Treat it as a setup error when:

- Node is below `^20.19.0 || >=22.12.0`
- `npm test` fails before worker startup with a `rolldown` or native-binding startup error
- the machine is below the clean Node baseline even if a previously prepared checkout still happens to pass `npm test`
- the client cannot see the server at all
- the server entrypoint path is wrong
- the worker executable path is wrong

Treat it as a worker or SolidWorks environment issue when:

- the client sees the server
- the 13 tools are listed
- metadata queries work
- failures begin only when the modeling flow touches the worker-backed runtime

## Evidence to capture

Minimum evidence:

- checkout path used
- `node -v`
- `npm -v`
- preflight JSON path
- published worker path
- a saved copy or transcript of the 13-tool discovery

Optional stronger evidence:

- `.\scripts\run-real-worker-regression.ps1 -WorkerExe .\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe`
- latest regression manifest under `C:\SolidWorksMcpValidation\regression-runs\`
- latest repository evidence bundle under `.\artifacts\real-evidence\`

## Current status after this session

Completed on 2026-04-13:

- same-machine alternate-path simulation at `C:\Temp\SOLIDWORKS-MCP-second-path`
- real-machine rerun on `401LT02346`
- Node baseline closure from `20.18.0` to `24.13.0`
- repo-first sequence rerun cleanly:
  - `npm install`
  - `npm run build`
  - `npm test`
  - preflight
  - worker publish
- MCP stdio discovery with the 13 public alpha tools visible
- safe circle workflow through `export_step`

Observed rerun-only issue:

- a stale `attached_existing` SolidWorks session created by an earlier validation attempt caused `select_plane` to fail twice
- closing that validation-created session and rerunning from a clean `launched_new` session resolved the issue

Latest real-machine execution captured in:

- `docs/architecture/SECOND_MACHINE_VALIDATION_REPORT.md`

## Exit criteria for calling the project "second-machine validated"

All of these should be true:

1. the sequence above is rerun on a different developer machine
2. the machine meets the declared Node baseline without local force-workarounds
3. the client sees the 13 public alpha tools
4. one safe modeling flow succeeds
5. the collected evidence is attached to planning state or handoff notes

Current result:

- met on 2026-04-13 for machine `401LT02346`
