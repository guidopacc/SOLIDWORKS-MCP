# PROJECT GOAL

## Final objective

Build a serious, modular, testable MCP server that can progressively control SolidWorks through text commands, starting with part modeling primitives and a SolidWorks 2022 compatibility baseline on Windows.

## Non-negotiable principles

- Do not mix MCP protocol concerns with SolidWorks API details.
- Keep the MCP server, application orchestration, mock backend, and real backend clearly separated.
- Make every important behavior traceable, version-aware, testable, and documentable.
- Treat the mock backend as a first-class development target, not as throwaway scaffolding.
- Do not claim real SolidWorks support until it is verified on a Windows machine with SolidWorks installed.

## Architectural assumptions

- The current development environment does not have SolidWorks installed.
- The real backend will need Windows-specific handling for COM and process lifetime.
- The long-term real adapter is best treated as a dedicated Windows/.NET worker behind a stable CAD contract.
- The TypeScript MCP core should remain portable and runnable without CAD software.

## Main risks

- SolidWorks API integration cannot be fully validated in the current environment.
- COM lifecycle, STA threading, and recovery behavior must be proven later on Windows.
- Future SolidWorks version differences may affect feature-building APIs and error behavior.
- LLM clients may issue commands out of sequence, so the server must enforce workflow preconditions.

## v1 in scope

- New part creation
- Plane selection
- Sketch lifecycle
- Basic sketch primitives
- Dimensions
- Core solid features
- Save and STEP export
- Normalized document state retrieval
- Tool catalog and project status queries

## v1 out of scope

- Complex assemblies
- Advanced drawings
- Lofts and sweeps
- Sheet metal
- Weldments
- Advanced surfacing
- Broad “support everything” promises

