# SOLIDWORKS CODE AUDIT

## Purpose

This document captures a focused code-level audit of the three highest-signal public SolidWorks MCP repositories previously identified in the ecosystem survey.

The goal is not to copy their implementations directly. The goal is to extract:

- useful technical patterns
- concrete failure modes
- bootstrap lessons for a first real backend slice
- implications for `SOLIDWORKS-MCP`

This audit is intentionally narrow. It focuses on the minimum topics needed to de-risk the first real SolidWorks backend:

- session bootstrap
- document creation/open/save
- sketch lifecycle
- save/export behavior
- error handling
- backend / bridge structure

## Repositories analyzed

### Direct SolidWorks-control repositories

- [andrewbartels1/SolidworksMCP-python](https://github.com/andrewbartels1/SolidworksMCP-python)
- [vespo92/SolidworksMCP-TS](https://github.com/vespo92/SolidworksMCP-TS)
- [tylerstoltz/SW_MCP](https://github.com/tylerstoltz/SW_MCP)

## Audit method

- Read the runtime connection/bootstrap code
- Read the document lifecycle code (`new`, `open`, `save`, `export`)
- Read the sketch lifecycle code
- Read the backend factory / bridge code
- Read available real-integration tests or checklists when present

This is still a source audit, not an end-to-end execution audit.

## High-level conclusions

- The strongest direct lessons come from `SolidworksMCP-python` for operational realism and from `SW_MCP` for the .NET direction.
- `SolidworksMCP-TS` is highly useful as a cautionary repository: it documents real Node/COM pain clearly and therefore strengthens the case for a dedicated Windows/.NET worker.
- None of the three repositories provides a clean, already-proven answer for a production-grade SolidWorks MCP server.
- Together, they do provide enough evidence to define a small, realistic first worker slice:
  - connect / attach
  - health / version
  - session status
  - new part
  - minimal save

## Cross-repo patterns worth keeping

### Pattern 1: Try attach first, then create or launch

Observed in:

- `SolidworksMCP-python`: `GetActiveObject("SldWorks.Application")` then `Dispatch("SldWorks.Application")`
- `SW_MCP`: `Type.GetTypeFromProgID("SldWorks.Application")` then `Activator.CreateInstance(...)`

Why it is useful:

- preserves reuse of an existing SolidWorks session when available
- avoids starting extra instances unnecessarily
- gives a natural path for session-status reporting

### Pattern 2: Make environment assumptions explicit

Observed in:

- `SolidworksMCP-python`: Windows-only COM adapter, mock fallback on non-Windows, real integration tests explicitly marked `windows_only` and `solidworks_only`
- `SW_MCP`: startup initializes the SolidWorks connection directly and logs to `stderr`

Why it is useful:

- prevents accidental claims of cross-platform support
- makes real validation evidence traceable

### Pattern 3: Resolve templates from SolidWorks preferences instead of hardcoding a single path

Observed in:

- `SolidworksMCP-python`: `_resolve_template_path(...)` probes user-preference slots before `NewDocument(...)`
- `SW_MCP`: uses `GetUserPreferenceStringValue(...)`, although the current helper is too naive

Why it is useful:

- template paths vary significantly by workstation and company configuration
- it reduces brittle local assumptions

### Pattern 4: Verify save/export by the file system, not only by API return value

Observed in:

- `SolidworksMCP-python`: after `SaveAs3`, it checks that the file actually exists
- `SolidworksMCP-python` also tolerates API return idiosyncrasies where `0` can mean success

Why it is useful:

- SolidWorks and COM bindings do not always present a clean boolean-only success signal
- file existence is a practical, low-cost verification step for v1

### Pattern 5: Use real-integration tests for document lifecycle before broader modeling claims

Observed in:

- `SolidworksMCP-python`: real tests for connection health, create/save/open/close, and load/save lifecycle

Why it is useful:

- it matches the correct incremental order for our real backend rollout
- it is much safer than starting from sketch/feature complexity

## Cross-repo anti-patterns

### Anti-pattern 1: Use a fragile runtime substrate as the long-term architecture

Observed in:

- `SolidworksMCP-TS`: strong reliance on Node/COM through `winax`, plus complexity analyzers and macro fallbacks to work around COM call limits

Why to avoid:

- too much of the architecture becomes a workaround for the runtime instead of for SolidWorks itself
- parameter-count issues become a first-order design driver

### Anti-pattern 2: Let the first real slice inherit advanced feature complexity

Observed in:

- `SolidworksMCP-TS`: macro fallback and complexity analyzer exist largely because advanced feature methods are difficult through Node COM
- `SW_MCP`: `FeatureExtrusion2(...)` already introduces a large parameter surface in the core workflow tools

Why to avoid:

- it expands failure modes too early
- it hides whether the backend bootstrap itself is actually healthy

### Anti-pattern 3: Return loosely typed anonymous success/error payloads everywhere

Observed in:

- `SW_MCP`: anonymous `{ success, error }` responses dominate the tool surface
- `SolidworksMCP-TS`: throws generic `Error` objects across many runtime paths

Why to avoid:

- weak recovery semantics
- poor LLM guidance on next actions
- inconsistent mapping between runtime failures and tool-level failures

### Anti-pattern 4: Broad tool inventory without equally strong validation evidence

Observed in:

- all three repositories to different degrees

Why to avoid:

- encourages support claims beyond evidence
- makes triage and support much harder

## Detailed findings by topic

## Session bootstrap

### `andrewbartels1/SolidworksMCP-python`

Useful code patterns:

- Initializes COM apartment explicitly with `pythoncom.CoInitialize()`
- Attaches to an existing session via `GetActiveObject(...)`
- Falls back to creating a session via `Dispatch(...)`
- Forces visibility and disables warning/question dialogs for automation

Strengths:

- operationally realistic
- treats Windows COM as a real runtime concern
- clearly separates mock and real adapters

Risks:

- user preference toggles are mutated at connect/disconnect time
- adapter-level connection concerns are mixed with richer modeling behavior in one large class

Implication for us:

- the .NET worker should own COM apartment/threading and session bootstrap, not the MCP process
- attach-first behavior should be preserved
- visibility and user-preference mutation should be explicit and documented

### `vespo92/SolidworksMCP-TS`

Useful code patterns:

- `connect()` is very small and direct
- the repo documents the runtime limitation loudly and adds diagnostics around it

Strengths:

- honest about fragility
- good observability mindset

Risks:

- `new winax.Object("SldWorks.Application")` directly ties the long-term runtime path to Node/COM fragility
- no strong attach-vs-launch distinction is evident in the audited adapter path

Implication for us:

- confirms that COM runtime ownership belongs in the Windows worker, not in the TypeScript server

### `tylerstoltz/SW_MCP`

Useful code patterns:

- `Type.GetTypeFromProgID("SldWorks.Application")`
- `Activator.CreateInstance(...)`
- connection registered as a singleton service
- logs are routed to `stderr` so stdout stays clean for JSON-RPC/MCP

Strengths:

- stack direction matches our intended worker
- startup and DI structure are simple and understandable

Risks:

- startup connection is eager and global
- the code comment says “connect to running instance”, but `Activator.CreateInstance(...)` does not clearly distinguish attach from launch

Implication for us:

- keep the .NET stack direction
- be more explicit than this repository about attach/launch semantics and startup outcomes

## Document handling

### `SolidworksMCP-python`

Observed patterns:

- `create_part()` prefers `NewPart()` when available, else resolves a `.prtdot` template and calls `NewDocument(...)`
- `open_model()` uses `OpenDoc6(...)`
- `currentModel` is updated centrally

Useful techniques:

- template resolution through user preference slots
- a current-document pointer inside the adapter

Risks:

- the adapter class is very large and combines state, connection, document lifecycle, sketching, and features

Implication for us:

- for the first worker slice, document lifecycle should be implemented in a dedicated service/class, not directly in `Program.cs`

### `SolidworksMCP-TS`

Observed patterns:

- `createPart()` uses `NewPart()`
- `createAssembly()` and `createDrawing()` use `NewAssembly()` / `NewDrawing()`
- current document is tracked in memory

Useful techniques:

- very small document-creation paths

Risks:

- `NewPart()` convenience alone may not be enough for company-specific template realities
- less explicit evidence around template fallback or document activation details

Implication for us:

- prefer explicit template-aware document creation for the real worker slice, even if a convenience helper exists

### `SW_MCP`

Observed patterns:

- `CreatePart(...)` and `CreateAssembly(...)` call `NewDocument(template, ...)`
- helper `GetDefaultTemplate(...)` exists

Useful techniques:

- using `GetUserPreferenceStringValue(...)` for default templates is directionally correct

Risks:

- `GetDefaultTemplate(...)` currently always returns `swDefaultTemplatePart`, even though it accepts `docType`
- that is a concrete example of a helper that looks generic but is not actually version/type-correct

Implication for us:

- document-template lookup must be doc-type-specific and explicitly tested
- avoid “generic” helpers that silently ignore the requested document type

## Sketch lifecycle

### `SolidworksMCP-python`

Observed patterns:

- plane normalization map (`Top`, `Front`, `Right`, `XY`, `XZ`, `YZ`)
- prefers direct feature lookup via `FeatureByName(...)` and `Select2(...)`
- only falls back to `SelectByID2(...)` when needed
- `InsertSketch(True)` is tried first, then `InsertSketch()` without bool for compatibility
- `exit_sketch()` toggles sketch mode and clears local sketch references

Useful techniques:

- prefer feature lookup over string-ID selection when possible
- treat COM signature variation as a real compatibility concern

Risks:

- too much compatibility logic is embedded in the same large adapter that also owns unrelated features

Implication for us:

- when we eventually implement sketch support, plane selection should not rely only on `SelectByID2(...)`
- sketch open/close state should be mapped into normalized worker state

### `SolidworksMCP-TS`

Observed patterns:

- direct `SelectByID2("${plane} Plane", "PLANE", ...)`
- `InsertSketch(true)` to enter sketch mode
- separate helper `selectSketchForFeature()` prefers feature-tree traversal over `SelectByID2(...)`

Useful techniques:

- the helper’s preference for feature-tree traversal is a strong signal

Risks:

- the primary `createSketch()` path is simpler but more brittle than the helper logic
- primary sketch operations still rely on the Node/COM substrate

Implication for us:

- keep the feature-tree / direct-object selection idea in mind for later, but do not bring sketching into the first worker slice

### `SW_MCP`

Observed patterns:

- uses `SelectByID2(...)` for `"Front Plane"`, `"Top Plane"`, `"Right Plane"`
- enters sketch mode with `InsertSketch(true)`
- `CreateExtrude(...)` exits active sketch before feature creation

Useful techniques:

- explicit sketch exit before feature creation is a good deterministic step

Risks:

- plane selection depends entirely on `SelectByID2(...)`
- no stronger fallback path is visible in the audited code

Implication for us:

- useful for the eventual sketch implementation, but not enough to justify including sketch in slice 1

## Save / export

### `SolidworksMCP-python`

Observed patterns:

- `save_file(...)` supports save-in-place and save-as
- treats COM save return values carefully (`bool`, integer, fallback behavior)
- verifies output file existence
- `export_file(...)` maps known format names and calls `SaveAs3(...)`

Useful techniques:

- file existence verification after save/export
- fallback from `SaveAs3(...)` to `SaveAs(...)`
- path normalization and directory creation

Risks:

- save/export semantics are still embedded in a very broad adapter

Implication for us:

- the first real worker slice should include file existence verification for `save_part`
- `export_step` should be deferred until the document bootstrap slice is proven

### `SolidworksMCP-TS`

Observed patterns:

- `exportFile(...)` maps formats and uses `Extension.SaveAs3(...)`
- checklist documents export pain and known limitations

Useful techniques:

- explicit format mapping

Risks:

- less verification around file existence in the audited adapter path
- overall export success is entangled with the fragility of the runtime substrate

Implication for us:

- export should not be in slice 1
- when added, it should verify both API response and output artifact presence

### `SW_MCP`

Observed patterns:

- save-in-place uses `Save3(...)`
- save-as uses `Extension.SaveAs(...)`
- captures `errors` and `warnings`

Useful techniques:

- explicit use of SolidWorks save warnings/errors

Risks:

- responses remain weakly typed and anonymous
- no audited file-system verification step after save-as

Implication for us:

- a future .NET save implementation should carry both:
  - SolidWorks error/warning values
  - independent file-system verification

## Error handling

### Strongest useful pattern

`SolidworksMCP-python` has the best audited operational discipline here:

- wrapper `_handle_com_operation(...)`
- timing + success/failure accounting
- adapter health reporting
- real integration tests around document lifecycle

### Common weakness across repos

- errors are mostly strings or generic exceptions
- few responses are designed for deterministic recovery by an LLM client
- support-state labeling is stronger in docs than in runtime envelopes

Implication for us:

- our existing typed error taxonomy is already a strength and should be preserved
- the .NET worker should map COM/runtime failures into the repository’s existing structured error codes

## Bridge / backend architecture

### `SolidworksMCP-python`

- clear adapter abstraction
- explicit factory
- mock + real adapter split
- wrappers for circuit breaker and connection pooling

This is the strongest direct architecture signal among the three repos.

### `SolidworksMCP-TS`

- strong intent toward adapter factory, circuit breaker, connection pooling, and macro fallback
- however, the long-term runtime still centers on Node/COM

Useful as architecture thinking, less useful as final backend direction.

### `SW_MCP`

- clean DI bootstrap in `Program.cs`
- one connection service, tools from assembly, stdio transport
- closest public shape to the worker direction we want

The main limitation is that it is still a monolithic MCP server rather than a clean worker behind a separate MCP core.

## Techniques we should reuse

1. Attach-first session bootstrap, with explicit fallback to launch/create.
2. Explicit template resolution from SolidWorks preferences.
3. File existence verification after `save_part`.
4. Real-integration tests with explicit Windows/SolidWorks markers.
5. Worker-side health/version/session reporting.
6. Sketch-plane selection fallback ideas, but only after slice 1 is complete.

## Techniques we should avoid

1. Building the final architecture around Node/COM workarounds.
2. Starting the real backend rollout with sketch/feature operations.
3. Dynamic “execute any SolidWorks API method” as a v1 execution surface.
4. Anonymous/generic error envelopes as the only runtime contract.
5. Template helpers that silently ignore document type or workstation variability.

## Concrete implications for SOLIDWORKS-MCP

### Immediate implications

- Keep the current `TypeScript MCP core + .NET worker` direction.
- Keep the first real slice extremely small.
- Do not start with sketch or feature operations.
- Use the first slice to prove:
  - worker handshake
  - SolidWorks attach/launch
  - version reporting
  - session status
  - new part
  - minimal save

### Technical implications

- Worker-side code should separate:
  - COM/session bootstrap
  - document lifecycle
  - later sketch/feature services
- Save behavior should:
  - normalize target path
  - ensure target directory exists
  - call SolidWorks save
  - verify file existence afterward
- Template resolution must be baseline-safe for SolidWorks 2022 and document-type aware.

### Scope implications

- `select_plane`, `start_sketch`, and feature creation should stay out of the first real worker slice.
- `export_step` should also stay out of the first slice unless a company test machine proves it with low complexity and low ambiguity.

## Recommended next use of this audit

This audit should directly inform:

- `docs/architecture/SOLIDWORKS_2022_VALIDATION_MATRIX.md`
- `docs/architecture/FIRST_REAL_WORKER_SLICE.md`
- the first implementation pass of the .NET worker on a real Windows/SolidWorks 2022 machine

## Source links

- [andrewbartels1/SolidworksMCP-python](https://github.com/andrewbartels1/SolidworksMCP-python)
- [vespo92/SolidworksMCP-TS](https://github.com/vespo92/SolidworksMCP-TS)
- [tylerstoltz/SW_MCP](https://github.com/tylerstoltz/SW_MCP)
