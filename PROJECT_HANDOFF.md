# PROJECT HANDOFF

Purpose: give the next developer a clean starting point for the current repo-first developer alpha without requiring project history reconstruction.

## Current state

As of 2026-04-16, the repository is no longer just a mock-first prototype.

What is implemented:

- TypeScript MCP server
- deterministic mock backend
- Windows/.NET worker-backed SolidWorks adapter
- published-worker execution path
- repo-first setup/config docs for Codex and Claude-based clients
- current public alpha tool surface exposed by default
- first public alpha prompt surface exposed by default
- two-resource public alpha surface exposed by default

What is validated on the company Windows machine with SolidWorks 2022:

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
- worker handshake and metadata
- attach-to-existing-session behavior
- published-worker serialized regression baseline
- local MCP client connection that lists the current public alpha tools
- second-machine validated repo-first path
- lightweight alpha handoff companion package preparation

What is explicitly outside the current public alpha:

- `add_dimension`
- `cut_extrude`
- `add_fillet`
- open/reopen document commands
- assemblies
- drawings
- installer-grade or desktop-extension-grade packaging

Current classification:

- `public alpha supported`: yes
- `tool surface supported`: yes
- `prompt surface supported`: yes
- `resource surface supported`: yes
- `repo-first distributable`: yes
- `second-machine validated`: yes
- `handoff-ready`: yes for controlled technical handoff
- `receiver-validated`: yes for the controlled package dry-run
- `installer-grade`: no

## Read these first

1. `README.md`
2. `docs/architecture/ALPHA_DELIVERY_MODEL.md`
3. `docs/architecture/ALPHA_PACKAGE_CONTENTS.md`
4. `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md`
5. `docs/getting-started/QUICKSTART_ALPHA.md`
6. `docs/architecture/MCP_CLIENT_SETUP.md`
7. `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md`
8. `docs/architecture/FIRST_PROMPT_MCP.md`
9. `docs/architecture/FIRST_RESOURCE_MCP.md`
10. `docs/architecture/SECOND_RESOURCE_MCP.md`
11. `docs/architecture/ALPHA_DISTRIBUTION_PLAN.md`
12. `docs/architecture/SECOND_MACHINE_VALIDATION.md`
13. `docs/architecture/SECOND_MACHINE_VALIDATION_REPORT.md`
14. `docs/architecture/PACKAGING_READINESS_DECISION.md`
15. `docs/planning/PROJECT_STATE.md`
16. `docs/planning/NEXT_STEPS.md`
17. `docs/planning/DECISIONS.md`

## What the receiver should get

Minimum real handoff:

- repository source
- alpha handoff companion package from `artifacts\alpha-handoffs\...`
- published worker artifact when available or a clear publish path
- focused docs and config examples

Optional review add-on:

- evidence pack from `artifacts\handoff-packs\...`

Do not blur these together into one fake installer story.

Receiver note:

- the companion package is for orientation and handoff clarity
- the repository checkout remains the working root for scripts and builds
- if a doc reference is not duplicated into the companion package, open it from the accompanying repository checkout

## Fastest repo-first path

From the repository root:

```powershell
npm install
npm run build
npm test
.\scripts\validate-windows-environment.ps1
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

Clean source-verification baseline:

- Node `^20.19.0 || >=22.12.0`

Then use one of the config examples in `configs/examples/`.

Recommended client paths:

- Codex: merge `configs/examples/codex.public-alpha.config.toml` into `~/.codex/config.toml`
- Claude Code: copy `configs/examples/claude-code.project.public-alpha.mcp.json` to the repo root as `.mcp.json`
- Claude Desktop manual path: adapt `configs/examples/claude-desktop.public-alpha.json`

Worker path options:

- recommended: publish from the repo and keep the config examples unchanged
- optional shortcut: keep the repo checkout as the working root and point `SOLIDWORKS_WORKER_COMMAND` to the package-local `worker\SolidWorksWorker.exe`

First checks after connection:

1. list available tools
2. list available prompts
3. list available resources
4. get project status
5. read `solidworks://alpha/public-boundary`
6. get current document state or read `solidworks://document/current-state`

Only then run a small modeling flow.

## Fastest sender path

From the repository root:

```powershell
npm install
npm run build
npm test
.\scripts\validate-windows-environment.ps1
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
.\scripts\prepare-alpha-handoff.ps1
```

Optional deeper review attachment:

```powershell
.\scripts\new-handoff-evidence-pack.ps1
.\scripts\prepare-alpha-handoff.ps1 -IncludeEvidencePack
```

## Public alpha contract

The public alpha is a narrow, progressive part-modeling workflow on Windows for SolidWorks 2022.

Supported prompt surface:

- `safe_modeling_session` for boundary-aware guidance within the current public alpha workflow

Supported resource surface:

- `current_document_state` at `solidworks://document/current-state` for read-only current session context
- `public_alpha_boundary` at `solidworks://alpha/public-boundary` for static machine-friendly boundary context

Supported workflow shape:

1. create a new part
2. select `Front Plane`, `Top Plane`, or `Right Plane`
3. start a sketch
4. draw a line, circle, or centered rectangle
5. close the sketch
6. optionally extrude
7. save the part
8. optionally export STEP

Do not widen the claim surface casually.

## Important repo-first constraints

- This is shareable as a developer alpha repository, not as a polished installer.
- The current handoff shape is repository source + companion package + docs + config examples + published worker when available.
- The alpha handoff companion is not the same thing as the evidence pack.
- Keep docs and runtime surface aligned.
- Do not reintroduce blocked tools into the default MCP surface.

## Where to change things

Tool surface and parsing:

- `mcp-server/src/tools/tool-definitions.ts`
- `mcp-server/src/server.ts`

Prompt surface:

- `mcp-server/src/prompts/prompt-definitions.ts`
- `mcp-server/src/server.ts`

Resource surface:

- `mcp-server/src/resources/resource-definitions.ts`
- `mcp-server/src/server.ts`
- `mcp-server/src/application/project-service.ts`

Project status / query semantics:

- `mcp-server/src/application/project-service.ts`

Backend selection:

- `mcp-server/src/application/backend-resolver.ts`

Worker transport / protocol:

- `adapters/solidworks/src/`

Windows execution discipline:

- `scripts/`

Alpha docs and examples:

- `README.md`
- `docs/getting-started/`
- `docs/architecture/`
- `configs/examples/`

## Current known gaps

- packaging is still repo-first and handoff-oriented, not `.mcpb`, installer, or marketplace grade
- reopened-part geometric readback remains weaker than file/feature-tree evidence
- a machine below Node `^20.19.0 || >=22.12.0` can still fail early in `npm test` with a toolchain startup issue before any worker debugging is relevant
- a real external receiver run would still be useful eventually, even though the controlled package dry-run is now complete

## Immediate next work

1. preserve the narrow public alpha boundary
2. treat the current prompt-plus-two-resource layer as the stable non-CAD alpha baseline
3. keep packaging changes small unless a new real receiver exposes additional friction
4. avoid new CAD capability work unless it has its own bounded validation plan
