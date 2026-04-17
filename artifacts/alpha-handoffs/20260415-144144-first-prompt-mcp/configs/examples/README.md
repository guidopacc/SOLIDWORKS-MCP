# Config Examples

Use these files as short, repo-first starting points for the current developer alpha.

## Files

- `codex.public-alpha.config.toml`
  - Merge the `mcp_servers.solidworksMcp` block into `~/.codex/config.toml`.
  - Replace `C:\path\to\SOLIDWORKS-MCP` with your real checkout path.
  - Once `cwd` is set, the server entrypoint and worker executable stay repo-relative.

- `claude-code.project.public-alpha.mcp.json`
  - Copy this file to the repository root as `.mcp.json` for a project-scoped Claude Code setup.
  - Keep the file in the repo root so the relative `dist/...` and `artifacts/...` paths resolve cleanly.

- `claude-desktop.public-alpha.json`
  - Merge the `mcpServers.solidworks-mcp` block into your Claude Desktop manual local MCP JSON config if you are intentionally using that path.
  - This repository does not yet ship a Claude Desktop `.mcpb` extension package.

- `AGENTS.public-alpha-snippet.md`
  - Optional instruction snippet to keep the client inside the current public alpha boundary.

## Before you use any config

1. Build the TypeScript server:

```powershell
npm install
npm run build
npm test
```

2. Publish the worker:

```powershell
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

3. Confirm the worker exists:

```powershell
Test-Path .\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe
```

## If you received a companion package with a worker copy

The recommended default is still the repo-first path above.

If the alpha handoff companion package already includes:

- `worker\SolidWorksWorker.exe`

you may skip a local republish and point:

- `SOLIDWORKS_WORKER_COMMAND`

to that package-local executable instead.

In that shortcut mode:

- keep the MCP server entrypoint rooted in the repository checkout
- keep `cwd` pointing at the repository checkout
- change only the worker executable path

Example adjustment for Codex:

- keep `cwd = "C:\\path\\to\\SOLIDWORKS-MCP"`
- replace `SOLIDWORKS_WORKER_COMMAND` with the absolute path to the received package copy, for example:
  - `C:\\path\\to\\alpha-handoff\\worker\\SolidWorksWorker.exe`

## First connection check

After the client reloads MCP servers, do this in order:

1. List available tools.
2. Ask for project status.
3. Ask for current document state.

If the client cannot see the server at all, treat it as config/setup trouble.
If the client sees the server and tool list, but modeling commands fail later, treat it as worker or SolidWorks runtime trouble.
