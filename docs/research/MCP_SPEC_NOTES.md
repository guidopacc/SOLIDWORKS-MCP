# MCP SPEC NOTES

## Primary official sources reviewed

- [MCP Architecture](https://modelcontextprotocol.io/specification/2025-06-18/architecture)
- [MCP Tools](https://modelcontextprotocol.io/specification/2025-06-18/server/tools)
- [MCP Resources](https://modelcontextprotocol.io/specification/2025-06-18/server/resources)
- [MCP Roots](https://modelcontextprotocol.io/specification/2025-06-18/client/roots)
- [OpenAI Codex MCP docs](https://developers.openai.com/codex/mcp)

## What a serious MCP server requires here

- Stable tool contracts with explicit input validation
- Structured tool errors rather than opaque failures
- Resource endpoints for inspectable state and project metadata
- Clear capability advertisement for tools and resources
- Transport-agnostic protocol logic with stdio as the current baseline

## Project implications

- Tools should remain task-oriented and workflow-aware rather than exposing raw backend internals.
- Resources are useful for state introspection such as project status, tool catalog, and current document state.
- The server should avoid embedding SolidWorks-specific COM handles in tool responses.
- Client/agent behavior can be improved by returning predictable, typed error envelopes.

## Notes on MCP SDK choice

- The official TypeScript SDK repository indicates that the `main` line is pre-alpha for v2 and that v1.x is the production recommendation.
- For this project state, the stable v1 TypeScript SDK is the correct baseline.

## Open MCP-related questions

- Whether future progress notifications are necessary for long-running real SolidWorks operations
- Whether roots should be leveraged for workspace-scoped save/export policies in later phases

