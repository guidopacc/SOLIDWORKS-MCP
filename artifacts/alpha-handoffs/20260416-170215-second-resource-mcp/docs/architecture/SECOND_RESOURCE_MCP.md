# SECOND RESOURCE MCP

## Purpose

Define the second public Resource MCP for `SOLIDWORKS-MCP` without widening the current CAD boundary.

## Current public resource surface

Supported today:

- `current_document_state`
  - URI: `solidworks://document/current-state`
- `public_alpha_boundary`
  - URI: `solidworks://alpha/public-boundary`

Not yet supported today:

- additional public resources beyond the current two-resource layer
- resources for project history or document browsing
- resources that imply open/reopen-document support

## Resource definition

Name:

- `public_alpha_boundary`

URI:

- `solidworks://alpha/public-boundary`

Purpose:

- provide static read-only context about the current public alpha boundary
- let a host or client preload the supported MCP surfaces before planning actions
- stay short, machine-friendly, and clearly distinct from the dynamic session resource

## What this resource exposes

The resource exposes only stable boundary facts already consistent with the public alpha docs and the current server surface:

- supported tool surface
- supported prompt surface
- supported resource surface
- safe workflow shape
- supported sketch primitives and planes
- companion references to `safe_modeling_session`, `current_document_state`, and the related query tools
- explicit out-of-scope requests
- known limits

The resource is useful for:

- host-side boundary preload before tool planning
- giving a model a compact static summary of what the alpha can and cannot do
- complementing `current_document_state` with non-session-specific context

## What this resource does not expose

It does not expose:

- arbitrary filesystem access
- project history
- historical document lookup
- open/reopen-document behavior
- extra CAD capability beyond the current public alpha
- a large dump of README or planning docs

## Relationship to existing public alpha surfaces

### Relation to `safe_modeling_session`

- `public_alpha_boundary` gives the prompt a static boundary snapshot before a workflow starts
- `safe_modeling_session` still provides the actual session guidance and stop rules

### Relation to `current_document_state`

- `public_alpha_boundary` is static boundary context
- `current_document_state` is dynamic session context
- the two resources are complementary, not interchangeable

### Relation to `get_document_state`, `list_available_tools`, and `get_project_status`

- `get_document_state` remains the explicit state refresh path during a workflow
- `list_available_tools` remains the live tool discovery query
- `get_project_status` remains the broader status query with project metadata
- `public_alpha_boundary` is the compact read-only boundary counterpart to those queries

### Relation to the public alpha boundary docs

- the resource is not a replacement for `PUBLIC_ALPHA_BOUNDARY.md`
- it is the machine-friendly runtime summary that should stay aligned with that document

## Example

Example read:

- read resource `solidworks://alpha/public-boundary`

Expected use:

- inspect the supported tool, prompt, and resource surfaces
- preload the safe workflow shape before asking for modeling actions
- decide whether to continue with `safe_modeling_session`, `current_document_state`, or an honest refusal

## Honest claim

This second resource does not add new CAD capability.

It adds one static boundary-safe resource that helps hosts and clients understand the current public alpha before acting.
