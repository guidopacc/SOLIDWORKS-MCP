# SOLIDWORKS MCP ECOSYSTEM AUDIT

## Purpose

Map the public ecosystem around SolidWorks + MCP and adjacent CAD automation projects in order to:

- understand what already exists
- identify reusable architectural, testing, and validation patterns
- avoid copying weak designs
- identify gaps and differentiation opportunities for `SOLIDWORKS-MCP`

This audit does **not** change the project direction. The target remains:

- a real MCP server
- Windows as the production target
- SolidWorks 2022 as the baseline
- mock-first development plus a real backend
- professional architecture, explicit status tracking, and scalable boundaries

## Research methodology

### Search approach

- Web search across GitHub and public web sources for:
  - `solidworks mcp`
  - `mcp solidworks`
  - `solidworks api mcp`
  - `solidworks automation server`
  - adjacent CAD MCP projects (`freecad`, `fusion 360`, `onshape`, `blender`, `cadquery`)
  - `CADSmith`
- Targeted checks for official SolidWorks / Dassault / 3DS pages related to MCP
- README-level audit of selected repositories and public documentation pages

### Classification rules

Projects were classified into four buckets:

1. **Real SolidWorks-control MCP projects**
   These attempt to automate a live SolidWorks instance.
2. **SolidWorks documentation / knowledge MCP projects**
   These expose API docs or search, but do not control SolidWorks.
3. **Adjacent CAD MCP projects**
   Different CAD stacks, but useful for architecture, tooling, packaging, and testing patterns.
4. **Architecture / validation inspirations**
   Not directly reusable as runtime architecture, but valuable for quality or evaluation ideas.

### Audit limitations

- This is a public-source audit, not a claim that every repository was executed end-to-end.
- In most cases the assessment is based on repository structure, README/docs, and stated scope.
- No claim in this document should be treated as proof of real SolidWorks compatibility unless the repository itself provides explicit verification evidence.
- **Inference:** no official Dassault / SolidWorks MCP server was found during this reconnaissance. That is a search result, not proof that none exists anywhere.

## Executive summary

- The public ecosystem for **real SolidWorks-control MCP servers** exists, but it is still early and uneven.
- The strongest direct comparators are:
  - `andrewbartels1/SolidworksMCP-python`
  - `vespo92/SolidworksMCP-TS`
  - `tylerstoltz/SW_MCP`
- There is at least one useful **docs-only SolidWorks MCP**:
  - `kilwizac/solidworks-api-mcp`
- The best **architecture and productization patterns** are currently more visible in adjacent projects than in SolidWorks-specific ones:
  - `hedless/onshape-mcp`
  - `ncmlabs/fusion360_mcp`
  - `neka-nat/freecad-mcp`
  - `contextform/freecad-mcp`
- The most relevant **validation inspiration** is `jabarkle/CADSmith`, which is not an MCP server but demonstrates rigorous programmatic geometric validation and refinement loops.
- No audited project clearly matches the target profile of `SOLIDWORKS-MCP`: a **Windows-first, SolidWorks-2022-baseline, mock-first, version-aware, professionally documented, explicitly status-tracked MCP server with a clean separation between MCP core and CAD backend**.

## Project inventory and classification

| Project | Classification | Stack | Why it matters |
| --- | --- | --- | --- |
| [andrewbartels1/SolidworksMCP-python](https://github.com/andrewbartels1/SolidworksMCP-python) | Real SolidWorks-control MCP | Python | Most operationally grounded public SolidWorks MCP repo found |
| [vespo92/SolidworksMCP-TS](https://github.com/vespo92/SolidworksMCP-TS) | Real SolidWorks-control MCP | TypeScript / Node | Strongest public TypeScript comparator; explicit COM limitations |
| [tylerstoltz/SW_MCP](https://github.com/tylerstoltz/SW_MCP) | Real SolidWorks-control MCP | C# / .NET | Strong C# direction and useful tool/resource partitioning |
| [eyfel/mcp-server-solidworks](https://github.com/eyfel/mcp-server-solidworks) | Real SolidWorks-control MCP concept | Python + C# | Valuable architectural intent, lighter implementation signal |
| [Sam-Of-The-Arth/SolidWorks-MCP](https://github.com/Sam-Of-The-Arth/SolidWorks-MCP) | Real SolidWorks-control MCP prototype | Python | Narrow but concrete baseline for basic sketch/extrude flows |
| [kilwizac/solidworks-api-mcp](https://github.com/kilwizac/solidworks-api-mcp) | Docs / knowledge MCP | TypeScript / Bun | Good proof that docs search should be treated separately from execution |
| [hedless/onshape-mcp](https://github.com/hedless/onshape-mcp) | Adjacent CAD MCP | Python | Best evidence of mature tool organization and strong test discipline |
| [ncmlabs/fusion360_mcp](https://github.com/ncmlabs/fusion360_mcp) | Adjacent CAD MCP | Python + Fusion add-in | Excellent host-addin / external-server split |
| [neka-nat/freecad-mcp](https://github.com/neka-nat/freecad-mcp) | Adjacent CAD MCP | Python + FreeCAD addon | Good host-app bridge and runtime control model |
| [contextform/freecad-mcp](https://github.com/contextform/freecad-mcp) | Adjacent CAD MCP | Python + installer tooling | Strong packaging and onboarding signal |
| [bertvanbrakel/mcp-cadquery](https://github.com/bertvanbrakel/mcp-cadquery) | Adjacent CAD MCP | Python | Useful geometry-first + stdio/SSE + part-library patterns |
| [CommonSenseMachines/blender-mcp](https://github.com/CommonSenseMachines/blender-mcp) | Adjacent CAD MCP | Python + Blender addon | Useful bridge/addon/client-ecosystem thinking, but less CAD-manufacturing focused |
| [jabarkle/CADSmith](https://github.com/jabarkle/CADSmith) | Validation inspiration | Python / CadQuery | Strongest quality-validation pattern found |
| [caid-technologies/OpenCAD](https://github.com/caid-technologies/OpenCAD) | Architecture inspiration | Python + services | Useful service-oriented CAD decomposition, but not SolidWorks-specific |

## Detailed findings

### 1. Real SolidWorks-control MCP projects

#### [andrewbartels1/SolidworksMCP-python](https://github.com/andrewbartels1/SolidworksMCP-python)

**Strengths**

- Presents an explicitly **verified Windows setup path**.
- Clearly states that real COM automation requires Windows and that WSL/Linux are useful only for docs/tests/mock mode.
- Has a practical operational story: local `.venv`, PowerShell launcher, Windows-specific MCP setup.
- Broad tool surface and visible architecture docs.

**Limits and risks**

- Breadth alone does not prove deep validation across all tools.
- Public evidence is still community-level, not official or production-certified.
- Python COM control is workable, but not automatically the most robust final architecture for a Windows-first open-source product.

**Why it matters to us**

- Best current public reference for **operational realism**.
- Worth targeted code audit for:
  - connection bootstrap
  - process launch conventions
  - version handling
  - how tool failures are surfaced to the client

#### [vespo92/SolidworksMCP-TS](https://github.com/vespo92/SolidworksMCP-TS)

**Strengths**

- Best public TypeScript comparator found.
- Very explicit about its **alpha / experimental** status and limited live validation.
- Documents an important technical constraint: Node COM bridges struggle with high-parameter SolidWorks calls.
- Introduces an interesting fallback strategy: route simple calls to `winax`, complex ones through generated VBA macros.

**Limits and risks**

- Most tools are not validated against live SolidWorks.
- `winax`-based COM interop remains a fragile substrate for a serious long-lived Windows product.
- Large unvalidated tool surface increases perceived capability faster than proven capability.

**Why it matters to us**

- Strong evidence that **keeping COM complexity out of the MCP core is the right direction**.
- Useful as a cautionary example for:
  - COM parameter-count pain
  - fallback path design
  - honest capability labeling

#### [tylerstoltz/SW_MCP](https://github.com/tylerstoltz/SW_MCP)

**Strengths**

- C# / .NET direction aligns better with stable SolidWorks COM interop.
- Splits the server into distinct tool types:
  - high-level workflow tools
  - dynamic API execution
  - resources for state inspection
  - documentation search
- Makes SolidWorks state visible as resources, not just tool outputs.

**Limits and risks**

- README explicitly says some areas “barely work” or need improvement.
- Targets SolidWorks 2020 rather than our 2022 baseline.
- Dynamic “execute any API method” capability is risky as a primary interface.

**Why it matters to us**

- Strong architectural inspiration for:
  - **resources for document state**
  - separating deterministic tools from exploratory/documentation features
- Confirms the value of a **C# worker layer**.

#### [eyfel/mcp-server-solidworks](https://github.com/eyfel/mcp-server-solidworks)

**Strengths**

- Good high-level architectural instinct:
  - client UI
  - Python prompt/context layer
  - C# adapter layer
  - COM bridge
- Talks explicitly about version-aware adapters.

**Limits and risks**

- Public implementation signal appears much lighter than the architectural ambition.
- More useful as architecture intent than as a proven benchmark.

**Why it matters to us**

- Reinforces the direction of **version-aware adapter boundaries**.
- Not enough evidence to treat as a runtime-quality reference.

#### [Sam-Of-The-Arth/SolidWorks-MCP](https://github.com/Sam-Of-The-Arth/SolidWorks-MCP)

**Strengths**

- Narrow, concrete, and easy to understand.
- Useful as a minimal baseline for simple workflows:
  - sketch
  - rectangle / circle
  - extrude
- Explicitly mentions SolidWorks 2022+ and testing on SolidWorks 2024.

**Limits and risks**

- Prototype-scale scope.
- Very limited tool surface and architecture depth.
- Template paths and local assumptions look relatively brittle.

**Why it matters to us**

- Good reminder that **small validated flows are more useful than large unvalidated catalogs**.

### 2. SolidWorks docs / knowledge MCP

#### [kilwizac/solidworks-api-mcp](https://github.com/kilwizac/solidworks-api-mcp)

**Strengths**

- Clear, honest scope: API documentation lookup, not CAD execution.
- Useful search/lookup helpers over a local SolidWorks API corpus.
- Good example of a docs-oriented MCP that could complement an execution server.

**Limits and risks**

- Does not control SolidWorks.
- Must not be confused with a CAD automation backend.

**Why it matters to us**

- Strong evidence that **documentation-search concerns should remain clearly separated from control concerns**.
- If we ever add docs search, it should likely be:
  - a separate server, or
  - an optional companion module with clearly different support claims

### 3. Adjacent CAD MCP projects with reusable patterns

#### [hedless/onshape-mcp](https://github.com/hedless/onshape-mcp)

**Strengths**

- Strong modular structure (`api`, `builders`, `analysis`, `tools`).
- Strong documentation footprint and roadmap discipline.
- Explicitly claims **471 unit tests**, which is a standout signal.
- Clear distinction between modeling, analysis, export, and document-discovery capabilities.

**Limits and risks**

- Onshape is cloud API-based, not local COM automation.
- Runtime constraints are different from SolidWorks desktop automation.

**Why it matters to us**

- Best public pattern for:
  - tool grouping
  - builder modules
  - analysis helpers
  - roadmap hygiene
  - testing discipline

#### [ncmlabs/fusion360_mcp](https://github.com/ncmlabs/fusion360_mcp)

**Strengths**

- Excellent split between:
  - MCP server
  - host CAD add-in
  - shared code
- Good documentation around architecture, troubleshooting, testing, and user setup.
- Includes health, version, validation, and viewport-style capabilities.

**Limits and risks**

- Fusion 360 add-in architecture is not directly equivalent to SolidWorks COM automation.
- Some transport and add-in assumptions are Autodesk-specific.

**Why it matters to us**

- Strongest adjacent reference for:
  - **bridge architecture**
  - **server vs host-runtime separation**
  - structured diagnostics
  - packaging and onboarding

#### [neka-nat/freecad-mcp](https://github.com/neka-nat/freecad-mcp)

**Strengths**

- Clear host-app addon plus external MCP bridge model.
- Good lifecycle controls:
  - manual start
  - auto-start
  - remote connection configuration
  - allowlisted clients
- Shows how CAD-host runtime and MCP client can be decoupled cleanly.

**Limits and risks**

- Different CAD stack and object model.
- Includes an `execute_code` capability that is useful in development but unsafe as a default product pattern.

**Why it matters to us**

- Helpful for:
  - lifecycle management
  - diagnostics
  - remote/host connectivity thinking
  - permission boundaries

#### [contextform/freecad-mcp](https://github.com/contextform/freecad-mcp)

**Strengths**

- Best packaging/onboarding signal among the adjacent projects.
- Strong install/update story and cross-platform ergonomics.
- Good example of treating installation and user success as first-class work.

**Limits and risks**

- Product direction is broader and more end-user oriented than our current scope.
- Much less relevant to SolidWorks-specific COM and Windows worker concerns.

**Why it matters to us**

- Worth studying for later:
  - installer UX
  - update flow
  - user onboarding

#### [bertvanbrakel/mcp-cadquery](https://github.com/bertvanbrakel/mcp-cadquery)

**Strengths**

- Supports both stdio and SSE.
- Includes part-library and workspace concepts.
- Strong geometry-first workflow with export and preview functionality.
- TDD is explicitly called out.

**Limits and risks**

- CadQuery is not SolidWorks and not COM-driven.
- Executes geometry scripts directly, which is a different safety and abstraction model than our v1 control server.

**Why it matters to us**

- Useful for:
  - workspace ideas
  - export/preview patterns
  - dual-transport thinking

#### [CommonSenseMachines/blender-mcp](https://github.com/CommonSenseMachines/blender-mcp)

**Strengths**

- Useful bridge thinking across host application, addon, external services, and MCP clients.
- Includes a separate Python client for testing and debugging.

**Limits and risks**

- Focuses more on creative asset workflows than on deterministic mechanical CAD.
- CSM asset integration is outside our target.

**Why it matters to us**

- Secondary inspiration for:
  - addon/client split
  - demo-driven documentation
  - standalone test client patterns

### 4. Architecture and validation inspirations

#### [jabarkle/CADSmith](https://github.com/jabarkle/CADSmith)

**Strengths**

- Best validation pattern found in the broader ecosystem.
- Uses exact geometric measurements plus visual inspection.
- Separates generation from judgment to avoid self-confirmation bias.
- Benchmarks performance with explicit metrics instead of subjective claims.

**Limits and risks**

- Not an MCP server.
- Not a SolidWorks controller.
- Built around CadQuery / OpenCASCADE, not SolidWorks COM.

**Why it matters to us**

- Strong inspiration for future **post-execution validation** strategies:
  - geometric checks
  - export-based verification
  - independent validation loops
- Relevant for v2/v3, not for replacing the current architecture

#### [caid-technologies/OpenCAD](https://github.com/caid-technologies/OpenCAD)

**Strengths**

- Strong service-oriented CAD decomposition:
  - kernel
  - solver
  - feature tree
  - agent
  - viewport
- Explicit mock-mode story.
- Good example of modular CAD subsystems.

**Limits and risks**

- Not SolidWorks-specific.
- More of a standalone CAD platform than a CAD automation adapter.

**Why it matters to us**

- Useful only as a distant inspiration for:
  - internal subsystem boundaries
  - typed operation registries
  - future solver/tree thinking

## Patterns worth studying

### Pattern 1: Keep the MCP core separate from the host CAD runtime

Best evidence:

- `ncmlabs/fusion360_mcp`
- `neka-nat/freecad-mcp`
- `tylerstoltz/SW_MCP`
- `eyfel/mcp-server-solidworks`

Why it matters:

- Keeps client protocol concerns away from CAD-host lifecycle and automation fragility.
- Matches our current TypeScript MCP core + Windows worker direction.

### Pattern 2: Expose deterministic workflow tools first

Best evidence:

- `Sam-Of-The-Arth/SolidWorks-MCP`
- `tylerstoltz/SW_MCP`
- `hedless/onshape-mcp`

Why it matters:

- Narrow, explicit workflows are easier to validate than a large “do everything” API surface.

### Pattern 3: Treat document state and diagnostics as first-class outputs

Best evidence:

- `tylerstoltz/SW_MCP` resources
- `ncmlabs/fusion360_mcp` health/version/validation orientation
- `hedless/onshape-mcp` document discovery and analysis tools

Why it matters:

- LLM clients need state visibility, not only fire-and-forget commands.

### Pattern 4: Provide a clear operational story for the real host environment

Best evidence:

- `andrewbartels1/SolidworksMCP-python`
- `contextform/freecad-mcp`
- `neka-nat/freecad-mcp`

Why it matters:

- Installation, startup, diagnostics, and environment assumptions are part of product quality.

### Pattern 5: Be explicit about test boundaries

Best evidence:

- `vespo92/SolidworksMCP-TS` honesty around mock vs real gaps
- `hedless/onshape-mcp` strong unit-test signal
- `CADSmith` benchmark transparency

Why it matters:

- Public trust comes from evidence, not from broad claims.

### Pattern 6: Use programmatic validation, not only “tool succeeded”

Best evidence:

- `CADSmith`
- `hedless/onshape-mcp` analysis tools
- `ncmlabs/fusion360_mcp` validation-oriented tool surface

Why it matters:

- “API call succeeded” is weaker than “result geometry and state are valid”.

## Patterns not worth copying

### Pattern to avoid 1: Large unvalidated tool catalogs

Observed in the ecosystem:

- some SolidWorks repos expose many tools, but only a small subset has visible live verification evidence

Why to avoid:

- it inflates perceived support
- it makes roadmap control harder
- it weakens trust

### Pattern to avoid 2: Directly coupling the MCP layer to COM details

Observed risk:

- Node/Python repos often expose raw runtime fragility close to the tool surface

Why to avoid:

- brittle error handling
- hard-to-test behavior
- poor portability of the MCP layer

### Pattern to avoid 3: Dynamic “execute any API method” as a primary product surface

Observed in:

- `tylerstoltz/SW_MCP`

Why to avoid:

- too weakly typed for serious workflow reliability
- hard for LLMs to use safely
- encourages leaking backend-specific details into client behavior

### Pattern to avoid 4: Mixing docs search and execution support claims

Observed risk:

- docs-only MCP projects can be mistaken for automation servers

Why to avoid:

- muddies what is really supported
- confuses test strategy
- encourages overloaded, blurry product boundaries

### Pattern to avoid 5: Arbitrary code execution tools in the main control path

Observed in adjacent projects:

- `neka-nat/freecad-mcp`
- geometry-script ecosystems such as CadQuery/FreeCAD

Why to avoid:

- poor safety profile
- difficult support boundaries
- too much room for undefined behavior in a first serious release

## Open market and technical gaps

The audit suggests the following gaps remain open:

1. **No clearly mature, publicly visible SolidWorks MCP reference**
   Most public projects are early, prototype-like, or honest alpha-stage efforts.

2. **No clear public reference that combines mock-first development with real SolidWorks backend separation**
   This is a major opportunity for `SOLIDWORKS-MCP`.

3. **No strong public support matrix for SolidWorks version behavior**
   Claims often span multiple versions, but visible evidence is limited.

4. **Little evidence of recovery-grade error modeling**
   Few projects expose typed failure semantics, resync needs, or recovery workflows.

5. **Little evidence of rigorous real-machine validation**
   Public repos often mention local testing, but not structured validation matrices.

6. **Weak separation between documentation assistance and execution assistance**
   This is often blurred.

7. **Weak productization around installer, diagnostics, and environment discovery for SolidWorks**
   Adjacent CAD projects do better here than the SolidWorks-specific ones.

## Differentiation opportunities for SOLIDWORKS-MCP

`SOLIDWORKS-MCP` can differentiate clearly if it becomes:

1. **The most honest SolidWorks MCP**
   Every tool and backend path should be tagged as:
   - designed
   - implemented
   - tested in mock
   - tested on real SolidWorks
   - officially supported

2. **The cleanest architecture in the space**
   Maintain strict separation between:
   - MCP server
   - application/orchestration layer
   - backend contract
   - mock backend
   - Windows SolidWorks worker

3. **The most testable public SolidWorks MCP**
   Use the mock backend to lock down semantics before real COM validation.

4. **The most version-aware SolidWorks MCP**
   Maintain a real version-support ledger and adapter notes for SolidWorks 2022+.

5. **The most diagnosable SolidWorks MCP**
   Provide clear:
   - health checks
   - backend metadata
   - version info
   - document-state resources
   - recovery guidance

6. **A future validation-aware SolidWorks MCP**
   Over time, adopt selected ideas inspired by `CADSmith`:
   - geometry checks
   - export verification
   - validation loops
   - not as LLM magic, but as engineering evidence

## Concrete recommendations for our roadmap

### Recommendation 1

Keep the current architecture direction unchanged:

- TypeScript MCP core
- clean application layer
- mock backend
- Windows-focused real worker

The ecosystem audit supports this direction rather than contradicting it.

### Recommendation 2

Perform a **targeted code-level audit** of the top three direct SolidWorks repos:

- `andrewbartels1/SolidworksMCP-python`
- `vespo92/SolidworksMCP-TS`
- `tylerstoltz/SW_MCP`

Focus only on issues directly relevant to our v1:

- connection/session bootstrap
- plane selection
- sketch lifecycle
- save/export flow
- error propagation
- runtime assumptions on Windows

### Recommendation 3

Keep any future **SolidWorks API documentation search** capability separate from the execution-critical path.

That can be:

- a separate companion MCP server, or
- an optional module with very explicit support boundaries

### Recommendation 4

Add early **diagnostic and introspection tools** to the real-backend roadmap:

- backend health
- SolidWorks version
- document metadata
- active document state
- worker/runtime info

Adjacent repos show that these tools materially improve reliability.

### Recommendation 5

Do not introduce “execute arbitrary SolidWorks API method” early in v1.

Prefer:

- deterministic tools
- typed parameters
- explicit preconditions
- explicit state transitions

### Recommendation 6

Prepare a **real-machine validation matrix** before claiming backend support.

At minimum, validate:

- SolidWorks launch / attach
- new part
- plane selection
- sketch start / end
- line / circle / centered rectangle
- boss extrude
- cut extrude
- save
- STEP export
- error behavior when preconditions fail

### Recommendation 7

Reserve advanced validation work for v2/v3, but keep the design open for it now.

Potential later additions inspired by `CADSmith`:

- geometric sanity checks after operations
- export-and-measure validation
- benchmark-style scenario suites

## What to study next

High-priority deep study:

- `andrewbartels1/SolidworksMCP-python`
- `vespo92/SolidworksMCP-TS`
- `tylerstoltz/SW_MCP`
- `ncmlabs/fusion360_mcp`
- `hedless/onshape-mcp`
- `jabarkle/CADSmith`

Lower-priority study:

- `neka-nat/freecad-mcp`
- `contextform/freecad-mcp`
- `bertvanbrakel/mcp-cadquery`
- `OpenCAD`

Low-signal / not worth deep imitation right now:

- thin prototypes with minimal architecture evidence
- repos that mainly prove a one-off demo path
- repos that blur docs lookup and real execution capability

## Source links

- [andrewbartels1/SolidworksMCP-python](https://github.com/andrewbartels1/SolidworksMCP-python)
- [vespo92/SolidworksMCP-TS](https://github.com/vespo92/SolidworksMCP-TS)
- [tylerstoltz/SW_MCP](https://github.com/tylerstoltz/SW_MCP)
- [eyfel/mcp-server-solidworks](https://github.com/eyfel/mcp-server-solidworks)
- [Sam-Of-The-Arth/SolidWorks-MCP](https://github.com/Sam-Of-The-Arth/SolidWorks-MCP)
- [kilwizac/solidworks-api-mcp](https://github.com/kilwizac/solidworks-api-mcp)
- [hedless/onshape-mcp](https://github.com/hedless/onshape-mcp)
- [ncmlabs/fusion360_mcp](https://github.com/ncmlabs/fusion360_mcp)
- [neka-nat/freecad-mcp](https://github.com/neka-nat/freecad-mcp)
- [contextform/freecad-mcp](https://github.com/contextform/freecad-mcp)
- [bertvanbrakel/mcp-cadquery](https://github.com/bertvanbrakel/mcp-cadquery)
- [CommonSenseMachines/blender-mcp](https://github.com/CommonSenseMachines/blender-mcp)
- [jabarkle/CADSmith](https://github.com/jabarkle/CADSmith)
- [CADSmith paper page](https://arxiv.org/html/2603.26512v1)
- [caid-technologies/OpenCAD](https://github.com/caid-technologies/OpenCAD)
