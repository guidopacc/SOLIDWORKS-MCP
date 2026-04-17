# RECEIVER DRY-RUN REPORT

## Purpose

Record the first controlled receiver-side dry-run of the alpha handoff companion package.

## Execution context

- date: 2026-04-15
- package path: `C:\Tools\SOLIDWORKS-MCP\artifacts\alpha-handoffs\20260415-141310-alpha-ready`
- repo path used for validation context: `C:\Tools\SOLIDWORKS-MCP`
- dry-run style: documentation and package-structure receiver simulation

## Receiver path followed

1. open `alpha-handoff-summary.txt`
2. inspect package root contents
3. follow the recommended read order:
   - `README.md`
   - `PROJECT_HANDOFF.md`
   - `docs/architecture/ALPHA_DELIVERY_MODEL.md`
   - `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md`
   - `docs/getting-started/QUICKSTART_ALPHA.md`
   - `docs/architecture/MCP_CLIENT_SETUP.md`
   - `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md`
4. inspect `configs/examples/`
5. inspect the included `worker\SolidWorksWorker.exe`

## What was clear immediately

- the package is a companion package, not an installer
- the current alpha boundary is narrow and honest
- the receiver still needs the repository source checkout
- the main client paths are Codex and Claude Code
- the first safe connection checks are:
  - tool list
  - project status
  - document state

## Real receiver-side friction found before fixes

### 1. Repo-only references were still easy to encounter from the package copy

When reading the package copy of `README.md` and `PROJECT_HANDOFF.md`, the receiver could still encounter references to docs that are intentionally repo-only and not duplicated into the companion package.

That was not fatal, but it was still mildly sender-centric because the distinction was not always explicit at the point of use.

### 2. The included worker copy did not have an explicit receiver setup story

The package includes `worker\SolidWorksWorker.exe`, but the config examples remained repo-relative and the receiver docs did not yet say clearly when to:

- rebuild and publish the worker from the repo
- reuse the included worker copy by overriding `SOLIDWORKS_WORKER_COMMAND`

That made the worker copy feel present but not fully explained.

## Fixes applied after the first pass

- clarified in the receiver-facing docs that some references remain repo-only and should be opened from the accompanying repo checkout
- added explicit receiver setup modes:
  - recommended repo rebuild/publish path
  - optional included-worker shortcut path
- updated config guidance to explain how to point `SOLIDWORKS_WORKER_COMMAND` at the package-local worker copy
- updated the generated alpha handoff summary to surface the included-worker shortcut more clearly

## Post-fix rerun result

After the fixes:

- the package still reads as repo-first, not installer-like
- the receiver path is clearer about what happens in the repo and what happens in the companion package
- the included worker copy now has an explicit, documented use path
- the package/repo/evidence distinction remains clear

## Final judgment

- `public alpha supported`: yes
- `repo-first distributable`: yes
- `second-machine validated`: yes
- `handoff-ready`: yes
- `receiver-validated`: yes for the controlled technical receiver dry-run
- `installer-grade`: no

## Meaning of `receiver-validated`

For this project state, `receiver-validated` means:

- a controlled receiver dry-run of the alpha handoff package was completed
- the remaining friction was small and documentational, not structural
- a technical receiver can now orient, find the right files, understand the setup path, and distinguish package vs repo vs evidence without relying on oral project memory

It does not mean:

- a new second-machine CAD validation
- installer-grade packaging
- extension-grade packaging
