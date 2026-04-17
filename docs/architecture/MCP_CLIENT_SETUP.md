# MCP CLIENT SETUP

## Purpose

Give one honest, repo-first path for connecting the current `SOLIDWORKS-MCP` developer alpha to serious MCP clients.

## Scope

This document covers:

- prerequisites
- build and publish steps
- where the main client configs go
- what to adapt locally
- how to verify the server is visible to the client
- how to verify the first public prompt is visible to the client
- how to verify the public alpha resource surface is visible to the client
- how to separate setup errors from worker/runtime errors

It does not claim:

- an installer
- a packaged Claude Desktop extension
- marketplace-style distribution

## Prerequisites

Minimum expected environment for the real alpha path:

- Windows 10 or Windows 11
- Node.js `^20.19.0 || >=22.12.0`
- npm
- .NET 8 SDK/runtime
- SolidWorks 2022 installed
- repository checkout on a local path

Recommended:

- keep the checkout outside cloud-synced folders
- use a deterministic output root such as `C:\SolidWorksMcpValidation\`
- meet the clean verification baseline before debugging client setup:
  - Node.js `^20.19.0 || >=22.12.0`

## Build and publish first

From the repository root:

```powershell
.\scripts\validate-windows-environment.ps1
npm install
npm run build
npm test
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

Expected published worker path:

- `.\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe`

## If you received a companion package with a worker copy

The recommended default is still the repo-first publish path above.

If the alpha handoff companion package already includes:

- `worker\SolidWorksWorker.exe`

you may skip a local worker republish and point:

- `SOLIDWORKS_WORKER_COMMAND`

to that package-local executable instead.

In that shortcut mode:

- keep the server entrypoint rooted in the repository checkout
- keep any repo-relative `dist/...` paths unchanged
- change only the worker executable path

## Recommended config files

The repo now keeps the examples in:

- `configs/examples/README.md`

### Codex

Recommended file:

- `configs/examples/codex.public-alpha.config.toml`

Where it goes:

- merge the `mcp_servers.solidworksMcp` block into `~/.codex/config.toml`

What to adapt:

- replace `C:\path\to\SOLIDWORKS-MCP` with your checkout path in `cwd`
- keep the repo-relative `dist/...` and `.\artifacts\...` paths once `cwd` is correct

How to verify:

```powershell
codex mcp list
```

Then ask the agent to:

1. list available tools
2. list available prompts
3. list available resources
4. show project status
5. show current document state

### Claude Code

Recommended file:

- `configs/examples/claude-code.project.public-alpha.mcp.json`

Where it goes:

- copy it to the repository root as `.mcp.json`

Why this path is preferred:

- it is repo-first
- it keeps the entrypoint and worker executable repo-relative
- it avoids pretending the project is already packaged for global install

What to adapt:

- usually nothing except the repo location itself, because the example is meant to live in the repo root
- if you move the config elsewhere, update the relative `dist/...` and `.\artifacts\...` paths

### Claude Desktop manual local config

Secondary manual path:

- `configs/examples/claude-desktop.public-alpha.json`

How to use it:

- merge the `mcpServers.solidworks-mcp` block into your manual `claude_desktop_config.json`

Important boundary:

- Anthropic currently emphasizes Desktop Extensions for Claude Desktop distribution.
- `SOLIDWORKS-MCP` is not packaged as an `.mcpb` extension yet.
- keep this JSON example framed as a manual developer-alpha local setup, not as a packaged desktop-distribution story.

## First connection checks

Do these in order:

1. list available tools
2. list available prompts
3. list available resources
4. get project status
5. read `solidworks://alpha/public-boundary`
6. get current document state
7. only then run a small modeling flow

Expected current public alpha tool list:

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
- `get_document_state`
- `list_available_tools`
- `get_project_status`

Expected current public alpha prompt list:

- `safe_modeling_session`

Expected current public alpha resource list:

- `current_document_state` at `solidworks://document/current-state`
- `public_alpha_boundary` at `solidworks://alpha/public-boundary`

## Distinguishing setup errors from worker errors

Treat it as setup/config trouble when:

- the client does not show the server at all
- the server appears but no tools are listed
- the Node entrypoint path is wrong
- the worker executable path is wrong
- `node` is missing from PATH for the chosen client path

Treat it as worker/SolidWorks runtime trouble when:

- the client sees the server and lists the 13 alpha tools
- `get_project_status` or `list_available_tools` works
- failures begin only when the worker-backed modeling flow starts

Useful narrow checks:

- `list_available_tools`
- prompt retrieval for `safe_modeling_session`
- resource read for `solidworks://alpha/public-boundary`
- resource read for `solidworks://document/current-state`
- `get_project_status`
- `get_document_state`

Those checks tell you whether the client can see the server and its current public alpha guidance/context layers before you spend time on CAD-specific debugging.

Treat these as setup/toolchain issues before you debug the client or worker:

- `npm test` fails before any MCP connection with a `rolldown` or native-binding startup error
- the local Node version is below `^20.19.0 || >=22.12.0`
- a warmed checkout works only because compatible native bindings are already present under `node_modules`

For a stricter rerun on a fresh machine or checkout path, use:

- `docs/architecture/SECOND_MACHINE_VALIDATION.md`

## Safe first modeling flow

If the client supports Prompt MCP:

- load `safe_modeling_session`
- use it to keep the session inside the validated progressive flow

If the client supports Resource MCP:

- read `solidworks://alpha/public-boundary` once at the start when host-side boundary context helps
- read `solidworks://document/current-state` before or between modeling steps when host-side read-only context helps

- "Create a new part, select the Front Plane, start a sketch, draw a circle centered at the origin with radius 10 mm, close the sketch, extrude 10 mm, save the part to `C:\\Temp\\alpha-demo.SLDPRT`, then export it to `C:\\Temp\\alpha-demo.step`."

## Optional instruction snippet

If the client supports project instructions, use:

- `configs/examples/AGENTS.public-alpha-snippet.md`

## Verification state

- config examples present: yes
- Codex repo-relative config path validated locally in this session: yes
- Claude Code repo-relative `.mcp.json` example added for repo-first use: yes
- manual Claude Desktop JSON example present: yes
- second checkout path MCP discovery rerun in this session: yes
- clean Node-baseline second-machine rerun now completed: yes
- first public prompt discovery validated locally: yes
- second public resource discovery validated locally: yes
- packaged Claude Desktop extension path: not yet
