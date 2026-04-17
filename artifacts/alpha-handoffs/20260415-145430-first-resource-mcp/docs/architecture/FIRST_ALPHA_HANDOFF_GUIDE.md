# FIRST ALPHA HANDOFF GUIDE

## Purpose

Give one short, practical sequence for preparing, sending, and receiving the current repo-first alpha handoff without relying on oral project history.

## What this handoff actually is

The current handoff is:

1. repository source
2. alpha handoff companion package from `artifacts\alpha-handoffs\...`
3. optional evidence pack from `artifacts\handoff-packs\...`

It is not:

- an installer
- a packaged desktop extension
- a replacement for the repository

## Receiver setup modes

The receiver has two valid setup modes.

### Mode A: recommended repo-first path

Use the repository checkout as the working root:

1. run the build and publish steps from the repo
2. keep the config examples repo-relative
3. treat the companion package as orientation and handoff context

This is still the preferred default.

### Mode B: optional included-worker shortcut

If the companion package already includes `worker\SolidWorksWorker.exe`, the receiver may skip a local worker republish and point:

- `SOLIDWORKS_WORKER_COMMAND`

to the package-local worker copy instead.

In that mode:

- keep the MCP server `cwd` or project root pointing at the repository checkout
- keep the server entrypoint repo-relative
- change only the worker executable path to the package-local worker copy

## Package copy versus repo copy

The companion package duplicates the receiver-critical subset of docs.

That means:

- use the package copy for orientation, entry reading, config discovery, and worker handoff context
- use the repository checkout for scripts, builds, publish steps, and any repo-only docs that are not duplicated into the companion package

If you see a doc reference that is not present inside the companion package, open it from the accompanying repository checkout rather than treating it as a missing installer asset.

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

Start with:

1. `alpha-handoff-summary.txt`

Read in this order:

1. `README.md`
2. `PROJECT_HANDOFF.md`
3. `docs/architecture/ALPHA_DELIVERY_MODEL.md`
4. `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md`
5. `docs/getting-started/QUICKSTART_ALPHA.md`
6. `docs/architecture/MCP_CLIENT_SETUP.md`
7. `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md`

Then:

1. choose a config example from `configs/examples/`
2. decide whether you are using:
   - the recommended repo-first rebuild/publish path
   - the optional included-worker shortcut path
3. connect the MCP client
4. ask for the current tool list
5. ask for project status
6. ask for document state
7. only then run one safe modeling flow

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
