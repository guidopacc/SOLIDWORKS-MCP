# TOOL CONTRACTS

## Current public alpha tool set

These are the tools currently exposed by the default MCP server surface.

| Tool | Purpose | Status |
| --- | --- | --- |
| `new_part` | Create a new part document | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `select_plane` | Select default reference plane | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `start_sketch` | Start a sketch on the selected plane | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `draw_line` | Add line entity to active sketch | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `draw_circle` | Add circle entity to active sketch | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `draw_centered_rectangle` | Add centered rectangle to active sketch | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `close_sketch` | Close active sketch | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `extrude_boss` | Create boss extrusion from sketch | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `save_part` | Save active part | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `export_step` | Export active part to STEP | Implemented in mock + real worker, validated on real SolidWorks 2022, public alpha |
| `get_document_state` | Return normalized document state | Implemented, public alpha |
| `list_available_tools` | Return the exposed tool catalog | Implemented, public alpha |
| `get_project_status` | Return project and alpha-readiness status | Implemented, public alpha |

## Planned or internal-only tools

These tools remain outside the current public alpha boundary:

| Tool | Purpose | Status |
| --- | --- | --- |
| `add_dimension` | Add dimension to sketch entity | Implemented in mock; real worker investigation attempted but not promoted |
| `cut_extrude` | Create cut extrusion from sketch | Implemented in mock only |
| `add_fillet` | Add fillet to feature | Implemented in mock only |

## Contract rules

- Inputs are validated before command execution.
- Failure paths return structured error codes.
- The server does not expose backend-native runtime handles.
- "Implemented in mock" does not mean "verified on real SolidWorks 2022".
- "Documented in the broader v1 scope" does not mean "public alpha".

## Future contract evolution

- Add assemblies only after the part workflow is real-machine validated.
- Add richer tool metadata once long-running operations and recovery paths exist.
- Keep broader items out of the public surface until they have their own real validation evidence.
