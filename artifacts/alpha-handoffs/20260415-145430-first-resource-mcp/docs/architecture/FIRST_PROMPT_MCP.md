# FIRST PROMPT MCP

## Purpose

Define the first public Prompt MCP for `SOLIDWORKS-MCP` without widening the current CAD boundary.

## Current public prompt surface

Supported today:

- `safe_modeling_session`

Not yet supported today:

- additional public prompts beyond `safe_modeling_session`
- Resource-backed prompt bundles
- prompts that imply unsupported CAD capability

## Prompt definition

Name:

- `safe_modeling_session`

Purpose:

- guide a client or model through the current public alpha workflow
- keep natural-language use honest and progressive
- stop clearly when the request exceeds the validated alpha boundary

Optional input:

- `user_request`
  - a short description of the part or modeling goal
  - used only to anchor the guidance to the user's actual request

## What this prompt tells the client to do

- stay inside the current public alpha tool surface
- keep the workflow progressive
- use `current_document_state` as initial read-only context when the host exposes it
- use `get_document_state` when the next step depends on current sketch or document facts
- use `list_available_tools` or `get_project_status` when the boundary or discovery state needs reconfirmation
- refuse unsupported requests clearly instead of improvising

Safe workflow shape reinforced by the prompt:

1. `new_part`
2. `select_plane`
3. `start_sketch`
4. `draw_line`, `draw_circle`, or `draw_centered_rectangle`
5. `close_sketch`
6. `get_document_state` when needed
7. `extrude_boss`
8. `save_part`
9. `export_step`

## When to use it

Use `safe_modeling_session` when:

- the client supports Prompt MCP discovery
- the user wants a safe first modeling flow inside the current alpha
- the session should stay aligned to the narrow public boundary

Do not use it as a claim that:

- dimensions are supported
- cut extrudes or fillets are supported
- assemblies or drawings are supported
- open/reopen-document editing is supported
- the system can design arbitrary parts from vague natural-language requests

## Example

Example prompt retrieval intent:

- get prompt `safe_modeling_session` with `user_request = "Create a simple extruded circle and export STEP."`

Expected result:

- the prompt guides the model toward the validated tool flow
- the prompt can consume `current_document_state` as initial context when available
- the prompt tells the model to use `get_document_state` instead of guessing
- the prompt tells the model to stop clearly if the request needs unsupported operations

## Honest claim

This first prompt does not add new CAD capability.

It adds one narrow guidance layer for using the existing public alpha tool surface more safely in natural-language sessions.
