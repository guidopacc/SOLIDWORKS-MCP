# FIRST RESOURCE MCP

## Purpose

Define the first public Resource MCP for `SOLIDWORKS-MCP` without widening the current CAD boundary.

## Current public resource surface

Supported today:

- `current_document_state`
  - URI: `solidworks://document/current-state`
- `public_alpha_boundary`
  - URI: `solidworks://alpha/public-boundary`

Not yet supported today:

- additional public resources beyond `current_document_state` and `public_alpha_boundary`
- resources for project history or document browsing
- resources that imply open/reopen-document support

## Resource definition

Name:

- `current_document_state`

URI:

- `solidworks://document/current-state`

Purpose:

- provide read-only context about the current modeling session
- let a host or client preload normalized document state without calling a tool first
- stay aligned to the same validated state model used by `get_document_state`

## What this resource exposes

The resource exposes only data already derivable from the current validated state model:

- whether a current document exists
- normalized `documentState`
- a small derived `documentSummary`
- the query equivalent: `get_document_state`
- the prompt companion: `safe_modeling_session`

The resource is useful for:

- initial host-side context loading
- showing a model the current document snapshot before a next step
- giving a receiver a read-only view of the current session state

## What this resource does not expose

It does not expose:

- arbitrary filesystem access
- historical document lookup
- open/reopen-document behavior
- extra CAD facts that are not already part of the validated state model
- generic project-wide discovery beyond the current active document snapshot

## Relationship to existing public alpha surfaces

### Relation to `get_document_state`

- `current_document_state` is the read-only resource counterpart to `get_document_state`
- use the resource when the host wants passive context
- use the tool query when the agent wants an explicit state refresh during a workflow

### Relation to `safe_modeling_session`

- `safe_modeling_session` can use `current_document_state` as initial read-only context when the host provides it
- after mutations, the prompt still prefers `get_document_state` for explicit refresh rather than guessing

### Relation to the public alpha boundary

- the resource does not add new CAD capability
- it reflects only the current active document snapshot
- it complements `public_alpha_boundary`, which exposes the static boundary contract rather than current session state
- it must not be treated as evidence of open/reopen-document support or broader document management

## Example

Example read:

- read resource `solidworks://document/current-state`

Expected use:

- inspect whether a document exists
- inspect known sketch/feature/export facts already recorded in normalized state
- decide whether a next step needs `get_document_state`, a tool call, or an honest refusal

## Honest claim

This first resource does not widen the CAD surface.

It adds one narrow read-only context surface that complements the existing tools and the first prompt.
