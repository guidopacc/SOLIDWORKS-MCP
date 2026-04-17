# SOLIDWORKS-MCP

`SOLIDWORKS-MCP` is a repo-first MCP server for narrow, validation-backed SolidWorks automation on Windows.

It is now past the "mock-only" stage, but it is still a developer alpha rather than a broad CAD automation product.

## Current status

Implemented:

- TypeScript MCP server
- deterministic mock backend
- Windows/.NET SolidWorks worker path
- public alpha tool surface aligned to the proven slice
- first public alpha prompt surface for safe modeling guidance
- two public alpha resources for read-only current session context and static boundary context
- quickstart, runbooks, config examples, and boundary docs

Validated on the company Windows machine with SolidWorks 2022:

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
- local MCP client connection that lists the current alpha tool surface

This is a narrow SolidWorks 2022 developer alpha, not a claim of general SolidWorks automation support.

Second-machine validation today:

- docs and config examples are prepared for a fresh developer path
- a second checkout path on the same Windows machine was exercised successfully during hardening
- a real second-machine rerun now passes on Windows + SolidWorks 2022 after moving the target machine to a clean Node baseline

Handoff status today:

- repo-first distributable: yes
- second-machine validated: yes
- handoff-ready: yes for a controlled technical alpha handoff
- receiver-validated: yes for a controlled technical receiver dry-run
- installer-grade: no

## Current public alpha surface

Commands:

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

Queries:

- `get_document_state`
- `list_available_tools`
- `get_project_status`

Prompts:

- `safe_modeling_session`

Resources:

- `current_document_state` at `solidworks://document/current-state`
- `public_alpha_boundary` at `solidworks://alpha/public-boundary`

## What This Alpha Is

- Windows-only
- SolidWorks 2022 baseline
- repo-first and script-driven
- suitable for a technical developer who can clone a repo, run scripts, and configure an MCP client
- designed for progressive part modeling one explicit step at a time

## What This Alpha Is Not

- not an installer
- not a Claude Desktop extension package
- not a marketplace release
- not a broad "draw anything in SolidWorks" surface
- not a claim that unsupported tools are safe just because mock support exists

## Not yet supported

- `add_dimension`
- `cut_extrude`
- `add_fillet`
- open or reopen existing documents through the public MCP surface
- assemblies
- drawings
- support claims for SolidWorks versions beyond the current 2022 baseline

## Repo-first developer path

From the repository root:

```powershell
npm install
npm run build
npm test
.\scripts\validate-windows-environment.ps1
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

Then configure a client using the examples in:

- `configs/examples/README.md`

Primary setup docs:

- `docs/getting-started/QUICKSTART_ALPHA.md`
- `docs/architecture/MCP_CLIENT_SETUP.md`
- `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md`
- `docs/architecture/FIRST_PROMPT_MCP.md`
- `docs/architecture/FIRST_RESOURCE_MCP.md`
- `docs/architecture/SECOND_RESOURCE_MCP.md`
- `docs/architecture/ALPHA_DELIVERY_MODEL.md`
- `docs/architecture/ALPHA_DISTRIBUTION_PLAN.md`
- `docs/architecture/ALPHA_PACKAGE_CONTENTS.md`
- `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md`
- `docs/architecture/DEVELOPER_ALPHA_VALIDATION.md`
- `docs/architecture/SECOND_MACHINE_VALIDATION.md`
- `docs/architecture/PACKAGING_READINESS_DECISION.md`

If you are reading this file from the alpha handoff companion package:

- use the copied docs there for initial orientation
- use the accompanying repository checkout for scripts, builds, and any repo-only docs not duplicated into the package

## Safe workflow shape

Use the current alpha for progressive, explicit flows such as:

1. create a new part
2. select `Front Plane`, `Top Plane`, or `Right Plane`
3. start a sketch
4. draw a line, circle, or centered rectangle
5. close the sketch
6. extrude if needed
7. save the part
8. export STEP if needed

Unsupported requests should be rejected honestly instead of guessed around.

If your client supports MCP prompts, start with `safe_modeling_session` to keep the session inside this validated flow without implying broader CAD support.

If your client supports MCP resources, use:

- `public_alpha_boundary` for static machine-friendly boundary context
- `current_document_state` for read-only current session context

Examples of unsupported requests today:

- "Add a dimension to this sketch."
- "Open an existing part and edit it."
- "Create a fillet."
- "Create a cut extrude."
- "Build an assembly."

## Windows execution scripts

Main scripts:

- preflight: `.\scripts\validate-windows-environment.ps1`
- debug build: `.\scripts\build-solidworks-worker.ps1`
- release publish: `.\scripts\publish-solidworks-worker.ps1`
- worker wrapper run: `.\scripts\run-solidworks-worker.ps1`
- serialized real regression: `.\scripts\run-real-worker-regression.ps1`
- full local cycle: `.\scripts\run-windows-worker-cycle.ps1`
- evidence collection: `.\scripts\collect-real-worker-evidence.ps1`

Key runbooks:

- `docs/architecture/WINDOWS_EXECUTION_RUNBOOK.md`
- `docs/architecture/REAL_REGRESSION_RUNBOOK.md`
- `docs/architecture/WINDOWS_BASELINE_AUTOMATION.md`

## Alpha handoff companion

For internal handoff or controlled early-adopter sharing, prepare the curated companion package from the repository root:

```powershell
.\scripts\prepare-alpha-handoff.ps1
```

Optional deeper review attachment:

```powershell
.\scripts\new-handoff-evidence-pack.ps1
.\scripts\prepare-alpha-handoff.ps1 -IncludeEvidencePack
```

This creates a lightweight folder under `artifacts\alpha-handoffs\...` with selected docs, config examples, validation notes, and the published worker when present.

It does not replace the repository and it is not an installer.

If the companion package includes `worker\SolidWorksWorker.exe`, the receiver may either:

- keep the standard repo-first publish path
- or keep the repo checkout as the server working root and override `SOLIDWORKS_WORKER_COMMAND` to the package-local worker copy

## Runtime backend selection

Default backend:

- `mock`

To use the SolidWorks worker path:

- `SOLIDWORKS_MCP_BACKEND=solidworks-worker`
- `SOLIDWORKS_WORKER_COMMAND=<worker executable path>`
- `SOLIDWORKS_WORKER_ARGS_JSON=<optional JSON array>`
- `SOLIDWORKS_WORKER_CWD=<optional working directory>`
- `SOLIDWORKS_WORKER_RESPONSE_TIMEOUT_MS=<optional positive integer>`
- `SOLIDWORKS_WORKER_SHUTDOWN_TIMEOUT_MS=<optional positive integer>`

## Current repo-first distributable shape

The current shareable alpha is:

- repository source
- alpha handoff companion package
- build and publish scripts
- public alpha docs and boundary notes
- Codex and Claude-based config examples
- published worker artifact or exact publish path
- Windows runbooks and optional evidence helpers

It is not yet:

- installer-grade
- extension-packaged
- broadly packaged beyond the current repo-first developer handoff
