Use the `solidworks-mcp` server only within its current public alpha surface.

Supported tool flow today:

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

Behavior rules:

- Prefer progressive modeling one explicit step at a time.
- Ask or report document state when the next step depends on current sketch/feature state.
- Save before export when the user is building a real artifact.
- If the user asks for dimensions, cut extrudes, fillets, assemblies, drawings, or open-existing-document workflows, say those are not yet supported in the current public alpha instead of guessing.
