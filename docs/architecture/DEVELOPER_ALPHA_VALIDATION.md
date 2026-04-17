# DEVELOPER ALPHA VALIDATION

## Purpose

Record the current "second path" validation pass for the repo-first developer alpha.

## Validation date

- 2026-04-13

## Goal

Check whether another technical developer could follow the current docs and arrive at a working alpha connection without relying on historical project memory.

## Path exercised in this session

From the repository root on the company Windows machine:

```powershell
npm install
npm run build
npm test
.\scripts\validate-windows-environment.ps1
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

Then:

1. direct published-worker smoke check through `.\scripts\run-solidworks-worker.ps1`
2. serialized real regression against the published worker through `.\scripts\run-real-worker-regression.ps1`
3. local MCP stdio client connection to the built server with the published worker enabled
4. tool discovery through `tools/list` and `list_available_tools`
5. project metadata read through `get_project_status`

## What was confirmed

Confirmed in this session:

- `npm run build` passes
- `npm test` passes
- `npm run verify` passes after the verifier and lint-path fixes
- Windows preflight passes on the baseline machine
- worker publish passes
- published-worker handshake and shutdown smoke pass
- published-worker serialized regression baseline passes for:
  - `plane-top`
  - `plane-right`
  - `line2d`
  - `rect2d`
  - `rect3d`
  - `slice4`
- the built MCP server is connectable through stdio with the worker backend enabled
- the connected client sees exactly the 13 public alpha tools
- `get_project_status` reports the narrow alpha boundary instead of the broader historical v1 scope

## Onboarding gaps found

1. The docs were overly anchored to `C:\Tools\SOLIDWORKS-MCP` and `pwsh -File ...` even though a repo-first path should work from the repo root with relative script invocations.
2. The repo had no project-scoped Claude Code `.mcp.json` example, even though that is a strong repo-first fit.
3. `PROJECT_HANDOFF.md` was stale and misleading for the current alpha state.
4. `npm run lint` could fail after real worker/build runs because generated artifacts were not excluded from Biome checks.
5. `npm run verify` was not a clean signal of source readiness because it inherited the lint/generated-artifact problem.
6. The refreshed dependency tree makes the clean verification baseline explicit: source verification should use Node `^20.19.0 || >=22.12.0`.

## Fixes applied in this session

- switched docs toward repo-root relative script execution
- added `configs/examples/claude-code.project.public-alpha.mcp.json`
- added `configs/examples/README.md`
- updated the Codex example to use repo-relative paths once `cwd` is set
- refreshed `PROJECT_HANDOFF.md` to the current developer alpha reality
- excluded generated build/publish artifact directories from Biome scanning
- refreshed the dependency tree so `npm run audit` is clean again
- fixed the Windows verifier so `npm run verify` works as a real repo check again
- tightened and aligned README, quickstart, client setup, boundary, and distribution docs

## What this validation did not prove

- a second independent external developer machine
- installer-grade packaging
- Claude Desktop extension packaging
- support outside the current public alpha tool surface

## Current conclusion

The project now has a more credible repo-first developer alpha path:

- buildable
- publishable
- locally connectable as an MCP server
- boundary-aligned
- documented without requiring full project history

It is still a developer alpha, not a general external release.
