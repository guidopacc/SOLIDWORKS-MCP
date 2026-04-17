# FIRST HANDOFF GUIDE

## Purpose

Give one short, practical sequence for preparing and receiving the current repo-first alpha handoff without relying on oral project history.

## If you are preparing the handoff

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

## What to send

Send:

1. the repository source
2. the generated folder under `artifacts\alpha-handoffs\<timestamp>\`

Optionally also send:

3. the generated evidence pack under `artifacts\handoff-packs\<timestamp>\`

The alpha handoff folder is a companion package, not a replacement for the repository.

## If you are receiving the handoff

Read in this order:

1. `README.md`
2. `PROJECT_HANDOFF.md`
3. `docs/architecture/ALPHA_DELIVERY_MODEL.md`
4. `docs/architecture/FIRST_HANDOFF_GUIDE.md`
5. `docs/getting-started/QUICKSTART_ALPHA.md`
6. `docs/architecture/MCP_CLIENT_SETUP.md`
7. `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md`

Then:

1. choose a config example from `configs/examples/`
2. connect the MCP client
3. ask for the current tool list
4. ask for project status
5. ask for document state
6. only then run one safe modeling flow

## Best current client paths

Primary repo-first paths:

- Codex using `configs/examples/codex.public-alpha.config.toml`
- Claude Code using `configs/examples/claude-code.project.public-alpha.mcp.json`

Secondary manual path:

- Claude Desktop using `configs/examples/claude-desktop.public-alpha.json`

## First supported workflow to try

Use a narrow flow such as:

1. `new_part`
2. `select_plane`
3. `start_sketch`
4. `draw_circle`
5. `close_sketch`
6. `extrude_boss`
7. `save_part`
8. `export_step`

## What you get

- a repo-first Windows developer alpha
- a validated 13-tool public alpha surface
- config examples for serious MCP clients
- a published worker path for real SolidWorks execution
- docs that describe the boundary honestly

## What you do not get

- an installer
- a packaged desktop extension
- broad CAD workflow support
- support for dimensions, cut extrudes, fillets, assemblies, or drawings
- a claim beyond the current SolidWorks 2022 baseline
