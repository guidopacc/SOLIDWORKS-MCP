# QUICKSTART ALPHA

## Purpose

Get a technical developer from repository checkout to first real `SOLIDWORKS-MCP` use on Windows without relying on project memory.

## Scope

This quickstart is only for the current public alpha:

- Windows
- SolidWorks 2022 baseline
- narrow progressive part workflow
- first prompt-backed guidance layer for that same workflow
- first read-only resource for current session context

It is not a path for dimensions, cut extrudes, fillets, assemblies, drawings, or generic document editing.

## If you received an alpha handoff package

Treat the handoff as:

1. repository source
2. alpha handoff companion package
3. optional evidence pack

Read first:

- `README.md`
- `PROJECT_HANDOFF.md`
- `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md`

The companion package helps orientation, but the repo remains required.

If the companion package includes `worker\SolidWorksWorker.exe`, you may either:

- follow the normal repo-first publish step below
- or keep the repo checkout as your working root and point `SOLIDWORKS_WORKER_COMMAND` at the package-local worker copy

## Prerequisites

- local repository checkout
- Node.js `^20.19.0 || >=22.12.0`
- npm
- .NET 8 SDK/runtime
- SolidWorks 2022 installed
- SolidWorks launched at least once under the target Windows user
- default part template configured in SolidWorks
- interactive desktop session available

If you are below the Node baseline, do not treat the machine as a clean second-machine validation environment until Node is upgraded.

Known symptom of being below the clean verification baseline:

- `npm test` can fail before any worker startup with a `rolldown` or native-binding startup error
- that is a setup/toolchain problem, not evidence of a SolidWorks worker regression
- on a previously prepared checkout, commands may still appear to work, but that does not satisfy the clean repo baseline

## 1. Run Windows preflight

From the repository root:

```powershell
.\scripts\validate-windows-environment.ps1
```

Useful artifact:

- `C:\SolidWorksMcpValidation\preflight\worker-preflight.json`

## 2. Build the MCP server

```powershell
npm install
npm run build
npm test
```

## 3. Publish the worker

```powershell
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

Expected worker path:

- `.\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe`

## 4. Pick a client path

Use the examples in:

- `configs/examples/README.md`

Recommended choices:

- Codex: merge `configs/examples/codex.public-alpha.config.toml` into `~/.codex/config.toml`
- Claude Code: copy `configs/examples/claude-code.project.public-alpha.mcp.json` to the repo root as `.mcp.json`
- Claude Desktop manual path: adapt `configs/examples/claude-desktop.public-alpha.json`

Then follow:

- `docs/architecture/MCP_CLIENT_SETUP.md`
- `docs/architecture/SECOND_MACHINE_VALIDATION.md` if you are validating on a fresh machine or a fresh checkout path

## 5. Verify the alpha boundary first

Before any modeling request, ask the client to:

1. list available tools
2. list available prompts
3. list available resources
4. report project status
5. report current document state or read the current document resource

That confirms the client sees the current public alpha tool, prompt, and resource surfaces, not the broader planned history.

## 6. First prompt-assisted path

If your client supports Prompt MCP discovery, start with:

- `safe_modeling_session`

Use it when:

- you want a safe first modeling flow
- you want the session to stay inside the current public alpha boundary
- you want the model to stop clearly on unsupported requests

It does not add new CAD features.

Good first prompt retrieval intent:

- get prompt `safe_modeling_session` with `user_request = "Create a simple extruded circle and export STEP."`

## 7. First safe requests

Good first requests:

- "List the currently available SOLIDWORKS-MCP tools."
- "List the currently available SOLIDWORKS-MCP prompts."
- "List the currently available SOLIDWORKS-MCP resources."
- "Show me the current SOLIDWORKS-MCP project status."
- "Read the `solidworks://document/current-state` resource."
- "Create a new part and tell me the current document state."

Good first modeling flow:

- "Create a new part, select the Front Plane, start a sketch, draw a circle centered at the origin with radius 10 mm, close the sketch, extrude 10 mm, save the part to `C:\\Temp\\alpha-cylinder.SLDPRT`, then export it to `C:\\Temp\\alpha-cylinder.step`."

## 8. Preferred safe sequence

Use the workflow in this order:

1. `new_part`
2. `select_plane`
3. `start_sketch`
4. draw geometry
5. `close_sketch`
6. `extrude_boss` if needed
7. `save_part`
8. `export_step` if needed

If the next action depends on state, query `get_document_state` instead of guessing.

## 9. Unsupported requests today

Treat these as unsupported:

- "Add a dimension to this sketch."
- "Open an existing part from disk."
- "Create a cut extrude."
- "Add a fillet."
- "Build an assembly."
- "Create a drawing."

The correct alpha behavior is to say the request is outside the current boundary.

## 10. Optional instruction snippet

If your client supports project instructions, use:

- `configs/examples/AGENTS.public-alpha-snippet.md`

## Validation state

- quickstart written: yes
- public alpha docs/config examples present: yes
- rerun on the company Windows baseline during this session: yes
- alternate checkout path simulated during this session: yes
- second-machine validation on a real Windows + SolidWorks 2022 machine: yes
- controlled receiver dry-run of the alpha handoff package: yes
- first public Prompt MCP supported locally: yes
- first public Resource MCP supported locally: yes
