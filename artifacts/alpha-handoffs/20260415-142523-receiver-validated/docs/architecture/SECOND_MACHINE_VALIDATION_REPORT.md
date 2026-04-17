# SECOND MACHINE VALIDATION REPORT

## Purpose

Record the latest real-machine execution attempt against the repo-first second-machine validation path.

## Execution context

- date: 2026-04-13
- machine: `401LT02346`
- checkout path: `C:\Tools\SOLIDWORKS-MCP`
- PowerShell: `5.1.26100.8115`
- final Node: `v24.13.0`
- final npm: `11.6.2`
- .NET SDK: `8.0.419`
- SolidWorks version: `SOLIDWORKS 2022` / `30.4.0.0045`

## Node baseline closure

Initial state on this machine:

- Node `v20.18.0`
- below clean repo baseline `^20.19.0 || >=22.12.0`

Fix applied:

- switched the machine to the already-installed `nvm` version `24.13.0`

Result:

- the machine now satisfies the declared Node baseline

## Repo-first sequence executed

From the repository root:

```powershell
npm install
npm run build
npm test
.\scripts\validate-windows-environment.ps1
.\scripts\publish-solidworks-worker.ps1 -Configuration Release
```

## Outcomes

- `npm install`: pass
- `npm run build`: pass
- `npm test`: pass
- Windows preflight: pass with clean Node baseline
- worker publish: pass
- MCP stdio discovery: pass with 13 public alpha tools
- safe modeling flow: pass

## MCP discovery result

Confirmed visible tools:

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

## Safe modeling flow executed

The following flow succeeded through MCP stdio with the published worker:

1. `list_available_tools`
2. `new_part`
3. `select_plane` on `Front Plane`
4. `start_sketch`
5. `draw_circle` with center `(0, 0)` and radius `10`
6. `close_sketch`
7. `extrude_boss` with depth `20`
8. `save_part` to `C:\Temp\alpha-second-machine-circle-final.SLDPRT`
9. `export_step` to `C:\Temp\alpha-second-machine-circle-final.step`
10. `get_document_state`

Final artifacts confirmed on disk:

- `C:\Temp\alpha-second-machine-circle-final.SLDPRT`
- `C:\Temp\alpha-second-machine-circle-final.step`

## Failure classification

### Setup / toolchain

- issue closed: the machine now runs Node `v24.13.0`
- current impact: none on the strict validation result

### MCP client configuration

- no reproduced issue in this execution

### Worker runtime

- no reproduced issue in the final validated circle workflow

### SolidWorks environment

- one rerun-only issue reproduced:
  - after an earlier successful flow left a validation-created SolidWorks session open, later `attached_existing` reruns failed at `select_plane`
  - closing that stale validation-created session and rerunning clean resolved the issue

## Important notes on rerun discipline

One initial probe used `draw_circle` radius `0.01`, which failed with `CreateCircleByRadius returned null`.

That was not treated as a project regression because the real validated project sequences use circle radius `10` and the corrected public-alpha flow passed afterward.

Later, after Node baseline closure, two reruns attached to an already-open validation-created SolidWorks session and failed at `select_plane`.

That was classified as a session/environment issue for repeated reruns, not as a boundary or capability regression, because a clean-session rerun passed immediately afterward.

## Conclusion

Operational result:

- the repo-first path is runnable on this accessible Windows machine
- the 13-tool public alpha surface is visible through MCP
- the safe public-alpha modeling flow succeeded through `export_step`

Strict validation result:

- `second-machine validated`: yes

Reason:

- the machine meets the clean Node baseline
- the repo-first validation pack passed
- the 13-tool alpha discovery passed
- the safe modeling flow passed on real SolidWorks 2022

## Immediate next action

Use this completed validation to drive the next packaging/handoff decision, while keeping the CAD surface frozen.
