# PUBLIC ALPHA BOUNDARY

## Purpose

Define the current developer/public alpha boundary for `SOLIDWORKS-MCP` so the runtime, docs, and setup examples all describe the same thing.

## What this alpha is

- a narrow developer alpha
- Windows only
- SolidWorks 2022 baseline only
- repo-first and script-driven
- focused on progressive part modeling

## What this alpha is not

- not a claim of broad SolidWorks automation
- not a generic "edit any CAD document" system
- not an installer or packaged extension
- not proof of support for versions beyond SolidWorks 2022

## Current public alpha surface

### Commands

| Tool | Implemented | Tested on real SolidWorks 2022 | Public alpha supported |
| --- | --- | --- | --- |
| `new_part` | Yes | Yes | Yes |
| `select_plane` | Yes | Yes on `Front Plane`, `Top Plane`, and `Right Plane` | Yes |
| `start_sketch` | Yes | Yes | Yes |
| `draw_line` | Yes | Yes | Yes |
| `draw_circle` | Yes | Yes | Yes |
| `draw_centered_rectangle` | Yes | Yes | Yes |
| `close_sketch` | Yes | Yes | Yes |
| `extrude_boss` | Yes | Yes | Yes |
| `save_part` | Yes | Yes | Yes |
| `export_step` | Yes | Yes | Yes |

### Queries

| Tool | Implemented | Notes | Public alpha supported |
| --- | --- | --- | --- |
| `get_document_state` | Yes | Returns normalized document state | Yes |
| `list_available_tools` | Yes | Reflects only the current public alpha list | Yes |
| `get_project_status` | Yes | Returns project and boundary metadata | Yes |

## Safe workflow shape

The current alpha is designed for one narrow workflow shape:

1. create a new part
2. select one supported standard plane
3. start a sketch
4. draw supported primitive geometry
5. close the sketch
6. extrude if needed
7. save the part
8. export STEP if needed

This is what the current real evidence supports.

## Safe natural-language behavior

Good alpha behavior:

- keep requests progressive and explicit
- ask for or report document state when the next step depends on state
- refuse unsupported requests clearly

Bad alpha behavior:

- improvising unsupported operations through unrelated tools
- treating planned tools as public just because they exist in internal design docs
- acting as though the current slice is a broad CAD assistant

## Unsupported today

- `add_dimension`
  - investigated on real SolidWorks 2022
  - still unsafe
  - explicitly outside the public tool list
- `cut_extrude`
- `add_fillet`
- open/reopen document workflows
- arbitrary sketch editing beyond the current primitive set
- assemblies
- drawings
- support claims for SolidWorks versions beyond 2022

## Honesty rules

- "Implemented" does not mean "tested on real SolidWorks 2022".
- "Tested on real SolidWorks 2022" does not mean "broadly supported".
- "Public alpha supported" means usable within the narrow workflow above.
- Unsupported requests should be rejected honestly.

## Current honest claim

The current honest external claim is:

- `SOLIDWORKS-MCP` is a Windows developer alpha for a narrow SolidWorks 2022 part workflow.
- It supports progressive modeling from new part through basic sketch primitives, minimal boss extrusion, save, and STEP export.
- It does not yet support dimensions, richer features, assemblies, drawings, or packaging beyond a repo-first developer handoff.
