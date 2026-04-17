# DECISIONS

## 2026-04-05

### Decision

Keep the MCP server and application orchestration in TypeScript, but plan the real SolidWorks adapter as a separate Windows-focused .NET component.

### Reason

This keeps the protocol layer portable and testable while respecting the robustness needs of COM automation on Windows.

### Alternatives rejected

- Directly embedding COM concerns into the MCP server
- Driving SolidWorks from a pure Node/TypeScript implementation as the final architecture

### Impact

The repository can progress now in mock mode without blocking the future production-grade Windows path.

## 2026-04-05

### Decision

Use a normalized document-state model as the core contract between the MCP layer and CAD backends.

### Reason

The server must not leak SolidWorks-specific runtime handles upward, and the mock backend must exercise the same workflow semantics as the real backend.

### Alternatives rejected

- Returning raw backend-specific objects
- Building tools directly around SolidWorks API calls

### Impact

The mock backend remains useful, and the future real backend can map CAD operations into the same state and error model.

## 2026-04-05

### Decision

Introduce structured domain/application errors with explicit codes such as `precondition_failed`, `not_found`, and `invalid_input`.

### Reason

LLM-driven clients need deterministic failures they can reason about. Generic errors are too weak for safe orchestration.

### Alternatives rejected

- Throwing generic `Error` objects everywhere
- Encoding all failure information only in plain-text messages

### Impact

Mock tests can lock down error semantics before the real SolidWorks adapter exists.

## 2026-04-05

### Decision

Pin TypeScript to `5.8.2` in this repository state.

### Reason

The local environment showed toolchain instability around newer resolution, and an exact pin keeps the current verification path deterministic.

### Alternatives rejected

- Floating `^5.x` versioning
- Moving immediately to a newer major without a reproducible baseline

### Impact

Build and typecheck remain reproducible while the repository stabilizes.

## 2026-04-05

### Decision

Use newline-delimited JSON as the internal IPC protocol between the TypeScript MCP core and the future Windows/.NET worker.

### Reason

It is simple to implement on both sides, easy to log and debug, and sufficient for the current request/response worker model.

### Alternatives rejected

- Embedding SolidWorks automation directly in the MCP process
- Introducing a heavier RPC stack before the boundary and lifecycle are proven

### Impact

The repository now has a concrete, versioned worker protocol and a .NET scaffold that can handshake honestly without claiming real SolidWorks execution.

## 2026-04-05

### Decision

Select the runtime CAD backend through environment-driven composition at the MCP server startup boundary, with `mock` as the safe default.

### Reason

This keeps the server deterministic in local development while allowing the worker-backed path to be enabled explicitly without changing tool contracts or server code.

### Alternatives rejected

- Hardcoding the mock backend forever in the server
- Auto-switching to the worker path implicitly without explicit configuration

### Impact

The stdio worker bridge is now part of the real runtime path, but still opt-in until Windows validation exists.

## 2026-04-05

### Decision

Keep the current stack and boundary direction unchanged after the public ecosystem audit: TypeScript MCP core, application-layer contracts, mock backend, and a Windows-focused .NET SolidWorks worker.

### Reason

The ecosystem audit found useful public comparators, but none justified a stack reversal. The strongest direct SolidWorks projects are still community-grade and partially validated, while the strongest adjacent references reinforce host-runtime separation rather than contradicting it.

### Alternatives rejected

- Switching the entire project to Python because some public SolidWorks MCP efforts use Python
- Switching the entire project to direct Node/COM control because a TypeScript comparator exists
- Collapsing the MCP core and the SolidWorks runtime into a single process

### Impact

The roadmap continues in the same direction, but with sharper comparative inputs for the Windows worker, validation strategy, diagnostics, and tool-scope discipline.

## 2026-04-05

### Decision

Treat SolidWorks API documentation search as a separate concern from the execution-critical control server in v1.

### Reason

The ecosystem audit found a useful docs-only SolidWorks MCP project, which confirms that documentation retrieval is valuable. It also confirms that mixing docs lookup and live automation under the same capability claims can blur testing, support status, and user expectations.

### Alternatives rejected

- Embedding docs-search features directly into the main control path from the start
- Treating docs-search MCP as equivalent to a real SolidWorks control backend

### Impact

The v1 control server stays focused on deterministic CAD operations. If a docs-search capability is added later, it should remain clearly scoped as a companion concern.

## 2026-04-05

### Decision

Constrain the first real Windows/.NET worker slice to backend bootstrap and document lifecycle only: handshake/version/session metadata, `new_part`, and minimal `save_part`.

### Reason

The code audit of the strongest public SolidWorks MCP repositories shows that real early failures cluster around runtime COM behavior, template resolution, and save semantics. Introducing sketch and feature execution in the same slice would blur backend-bootstrap risk with modeling risk.

### Alternatives rejected

- Starting the real worker implementation with sketch commands included
- Starting with `extrude_boss` as part of the first slice
- Starting with a wider “minimum useful” slice that also includes export and feature editing

### Impact

The next implementation pass stays intentionally small and measurable. A successful slice 1 proves the backend boundary and environment assumptions before modeling complexity is added.

## 2026-04-05

### Decision

Treat real SolidWorks 2022 support claims as matrix-gated, not implementation-gated.

### Reason

Source audits show that public repositories often implement more than they clearly validate. This project should only claim support after passing the explicit real-machine validation matrix for the corresponding support level.

### Alternatives rejected

- Declaring support based on implementation completion alone
- Declaring support based on mock coverage plus one or two manual smoke checks

### Impact

Project status and README claims must continue to distinguish clearly between implemented, tested in mock, tested on real SolidWorks 2022, and officially supported.

## 2026-04-05

### Decision

Implement the first real Windows worker slice with late-bound COM instead of introducing SolidWorks primary interop assemblies immediately.

### Reason

The current development environment has no local SolidWorks installation and no local `.NET` validation path. Late-bound COM keeps the first real slice buildable in principle without adding a hard compile-time dependency on SolidWorks interop assemblies before the Windows validation path is proven.

### Alternatives rejected

- Blocking the first real slice on SolidWorks primary interop assemblies from day one
- Moving the real slice back into Node/COM because it is already in the repository

### Impact

The worker can implement real attach/bootstrap/document/save behavior now, but some API assumptions remain explicit and must be confirmed on the company Windows machine.

## 2026-04-05

### Decision

For the first real slice, create parts through default-template resolution plus `NewDocument(...)`, and save through `Save3` or `SaveAs4` with filesystem verification.

### Reason

This keeps the real slice small and deterministic while still avoiding hardcoded template paths and weak save success semantics.

### Alternatives rejected

- Using a hardcoded part template path
- Treating a save API return value as sufficient success evidence without checking the output file
- Pulling sketch or feature APIs into the same slice

### Impact

The current real worker slice is focused on bootstrap/document/save semantics and is ready for Windows validation without widening scope.

## 2026-04-05

### Decision

Do not auto-close SolidWorks when the worker shuts down in slice 1.

### Reason

The worker may attach to a user-owned SolidWorks session. Automatically quitting the application would be unsafe and surprising during early validation.

### Alternatives rejected

- Always calling `Quit` on shutdown
- Trying to infer safely whether a session is disposable in the first slice

### Impact

Worker shutdown currently means protocol shutdown plus COM reference release, not application termination. Real validation should confirm that this is acceptable operationally.

## 2026-04-09

### Decision

Serialize worker NDJSON responses from the concrete runtime response type, and keep the protocol DTOs public in the .NET worker.

### Reason

Real Windows validation showed that handshake and command responses could execute real SolidWorks actions while still emitting almost-empty NDJSON payloads. The combination of base-type serialization and non-public DTOs prevented the expected derived response data from being emitted consistently.

### Alternatives rejected

- Leaving the response envelopes structurally correct but semantically empty
- Adding duplicate ad-hoc response objects only for serialization
- Redesigning the worker protocol before fixing the actual serializer boundary

### Impact

The worker now returns complete handshake, success, and failure payloads with backend metadata, execution/session facts, and typed error details intact.

## 2026-04-09

### Decision

For late-bound SolidWorks save calls, invoke `Save3` and `SaveAs4` with explicit `byref` parameter metadata for the error and warning outputs.

### Reason

Real Windows validation showed that `SaveAs4` was failing with `DISP_E_TYPEMISMATCH` even though the command, template resolution, and filesystem path were otherwise valid. The root cause was the reflection-based COM invocation not marking the trailing `Errors` and `Warnings` parameters as `byref`.

### Alternatives rejected

- Keeping the broken reflection path and treating the failure as an environmental blocker
- Switching the worker wholesale to SolidWorks primary interop assemblies mid-slice
- Replacing explicit-path save with a broader workaround or a second save flow

### Impact

Explicit-path save and in-place save now both pass on the company SolidWorks 2022 machine while preserving the slice-1 late-bound COM direction.

## 2026-04-09

### Decision

Do not assume that a freshly launched SolidWorks instance provides a valid no-document validation state on company Windows machines.

### Reason

During the first real validation run, a worker-launched SolidWorks instance could begin with an active blank or restored document, which made the naive `save_part` no-document check unreliable as a launch-time assumption.

### Alternatives rejected

- Treating the first launch attempt as proof that the no-document failure path was broken
- Baking machine-specific startup assumptions into the checklist
- Widening the worker command surface just to query session state before validation

### Impact

The slice-1 checklist and future validation harnesses should prepare the no-document state explicitly instead of assuming that a new SolidWorks launch is sufficient.

## 2026-04-09

### Decision

Use `draw_circle` as the first real visible sketch primitive, and defer `draw_centered_rectangle` until it is independently validated.

### Reason

Direct SolidWorks 2022 probing on the company Windows machine showed that the circle path was the least ambiguous and most repeatable first geometry path. Pulling rectangle support into the same first visible slice would widen the scope without increasing confidence.

### Alternatives rejected

- Starting the first visible sketch slice with `draw_centered_rectangle`
- Adding multiple sketch primitives before any one primitive was proven end-to-end

### Impact

The first real 2D geometry slice is intentionally narrow but honest: `select_plane` + `start_sketch` + `draw_circle` + `close_sketch`.

## 2026-04-09

### Decision

Use sketch-feature selection plus `FeatureExtrusion3(...)` for the first real boss extrusion path.

### Reason

Direct probing on the company Windows machine showed that selecting the closed sketch feature and calling `FeatureExtrusion3(...)` was reliable, while `SimpleFeatureBossExtrude(...)` did not provide a stable first-slice path.

### Alternatives rejected

- Using `SimpleFeatureBossExtrude(...)` for the first extrude slice
- Widening the feature slice to include more extrusion variants before a single blind boss path was proven

### Impact

The first 3D feature slice stays constrained to one empirically validated extrusion path and avoids premature breadth.

## 2026-04-09

### Decision

When a worker-launched SolidWorks session is not reliably inspectable from a second process through ROT, validate modeling slices through saved-part reopen evidence instead of broadening the execution surface.

### Reason

During the real slice-2 and slice-3 validation runs, the worker could execute real actions successfully while a second-process ROT inspection remained unreliable for that launched session. Reopening the saved `.SLDPRT` in a dedicated probe still provided honest external evidence without introducing generic “execute any method” utilities or redesigning the worker protocol.

### Alternatives rejected

- Expanding the worker with generic arbitrary API execution hooks just for validation
- Treating the lack of second-process ROT visibility as a blocker for the minimal modeling slice
- Claiming visual/modeling success without any external saved-file evidence

### Impact

The current validation harnesses preserve deterministic transcripts plus reopened-file evidence, while richer external geometry inspection can be improved later.

## 2026-04-09

### Decision

Implement the first real STEP export path through `ModelDoc2.SaveAs4(...)` to a `.step` or `.stp` target, with explicit file-existence verification and part-only scope.

### Reason

Real probing on the company Windows machine showed that `ModelDoc2.SaveAs4(...)` could export STEP successfully while keeping the active `.SLDPRT` document path stable. In the same probing pass, the first attempted `IModelDocExtension.SaveAs(...)` path produced `DISP_E_TYPEMISMATCH`, which made it a poor choice for the first tight export slice.

### Alternatives rejected

- Starting the first STEP export slice with `IModelDocExtension.SaveAs(...)`
- Broadening the export slice to include IGES, STL, or other formats at the same time
- Treating a `true` API return value as sufficient without verifying the exported file on disk

### Impact

`export_step` is now intentionally narrow, real-machine-backed, and aligned with the existing late-bound COM direction. It currently supports only part documents and only `.step` / `.stp` targets.

## 2026-04-09

### Decision

When a pre-existing `SLDWORKS.exe` process exists but ROT does not expose an active object, classify worker `connectionMode` from observed process reuse during COM activation instead of treating the session as automatically `launched_new`.

### Reason

Real company-Windows validation showed a concrete case where SolidWorks was already running, but `GetActiveObject(...)` still returned no active ROT entry. In that condition, `Activator.CreateInstance(...)` reused the existing SolidWorks process for real work, but the worker was still reporting `launched_new`, which made the attach metadata dishonest.

### Alternatives rejected

- Treating ROT-only discovery as the sole definition of `attached_existing`
- Leaving the attach behavior working but reporting misleading connection metadata
- Broadening the worker protocol just to expose process-level diagnostics

### Impact

Attach-first validation is now honest on the baseline machine even when the existing SolidWorks session is reachable only through COM activation reuse rather than direct ROT discovery.

## 2026-04-09

### Decision

Implement the first real centered-rectangle slice through `SketchManager.CreateCenterRectangle(...)`, while keeping the normalized state contract at one `centered_rectangle` entity instead of mirroring every raw SolidWorks sketch segment.

### Reason

The current backend contract is intentionally geometry-level, not raw-interop-level. Real SolidWorks 2022 validation proved that the rectangle path is stable, but the raw `segmentCount` observed from the API was `6` on this machine for the validated centered rectangle. Treating those raw segments as the contract would overfit the worker state to a SolidWorks-specific detail that the higher layers do not need.

### Alternatives rejected

- Delaying `draw_centered_rectangle` until every raw segment detail was modeled explicitly
- Expanding the normalized worker state to mirror SolidWorks segment-by-segment output
- Pretending the raw segment count was always `4` just because the geometric intent is a rectangle

### Impact

`draw_centered_rectangle` is now part of the real worker surface and is validated on SolidWorks 2022 for both 2D and rectangle-based `extrude_boss` flows, while the protocol still exposes a clean geometry-level state model.

## 2026-04-09

### Decision

Promote `draw_line` into the advertised real worker surface only after a dedicated real 2D saved-sketch validation pass, and keep the first proven case intentionally limited to one simple line segment.

### Reason

The project needed one more primitive beyond circle and centered rectangle, but without widening scope into arbitrary polylines or dimensions. A single `draw_line` command gives a real new capability while staying easy to validate honestly on SolidWorks 2022.

### Alternatives rejected

- Skipping directly from rectangle work to `add_dimension`
- Treating mock support for `draw_line` as sufficient proof
- Broadening the line slice into arbitrary multi-segment sketch authoring

### Impact

`draw_line` is now part of the real worker surface and has real SolidWorks 2022 evidence through a saved-part validation flow.

## 2026-04-09

### Decision

Keep `add_dimension` out of the advertised real worker surface until sketch-segment selection and dimension creation are proven stable on SolidWorks 2022.

### Reason

The first bounded real attempt failed with a typed `unable_to_add_dimension` error because coordinate-based selection could not resolve the target sketch segment. A follow-up fallback probe could hang the live SolidWorks session instead of returning a deterministic success or failure. Shipping that surface now would make the real worker less honest and less safe.

### Alternatives rejected

- Advertising `add_dimension` as supported just because local parsing/state-mapping scaffolding exists
- Leaving a potentially hanging command in the real worker surface while documentation says it is not proven
- Expanding the dimension slice to a more generic smart-dimension system before the first bounded case is stable

### Impact

`add_dimension` remains a planned capability, but not part of the current advertised real worker slice. The next work on dimensions should start from a dedicated real-machine blocker probe, not from broader feature work.

## 2026-04-09

### Decision

When `SketchManager.InsertSketch(...)` leaves `ActiveSketch` null on a real rerun, allow `ModelDoc2.EditSketch()` as a narrow fallback before declaring `start_sketch` failed.

### Reason

During this session, real reruns after the dimension investigation exposed an intermittent case where the selected plane was valid but `InsertSketch(...)` did not actually leave an active sketch handle. A small fallback on the already-selected plane improved robustness without widening the worker surface.

### Alternatives rejected

- Leaving the intermittent `start_sketch` failure untreated
- Reworking the whole sketch lifecycle around a broader new abstraction
- Hiding the issue behind harness retries instead of fixing the worker boundary

### Impact

The real sketch baseline remains stable on rerun after the dimension investigation, and the worker now records which activation path opened the sketch.

## 2026-04-09

### Decision

Do not expose `open_document` or `reopen_document` yet; keep reopen behavior as an external validation-harness concern for now.

### Reason

The current project needs are still better served by a narrow, validation-backed modeling slice than by widening the worker surface to path-based document lifecycle commands. The harness already has enough reopen capability to validate saved files externally.

### Alternatives rejected

- Adding `open_document` immediately because the harness already reopens saved parts
- Adding `reopen_document` as a thin convenience command without explicit error and state semantics
- Leaving the policy implicit

### Impact

The product surface stays disciplined, and future document-open work now has an explicit gate instead of remaining an unspoken maybe.

## 2026-04-09

### Decision

Run real SolidWorks worker regressions sequentially, not in parallel, and provide a scriptable path that enforces that discipline.

### Reason

This session showed a concrete false-negative mode when two real harness runs were launched against the same SolidWorks installation at the same time. The worker and harness logic were both fine, but the shared interactive CAD session made the results misleading.

### Alternatives rejected

- Continuing to run multiple real harness scenarios in parallel
- Treating the resulting failures as product regressions
- Keeping the sequential rule only as tribal knowledge

### Impact

The repository now includes a PowerShell regression helper for serialized real-machine runs, and future real validation should treat mutual exclusion as a hard requirement.

## 2026-04-09

### Decision

Treat the current `add_dimension` blocker as a dimension-creation API blocker, not just a selection-resolution blocker.

### Reason

The dedicated company-machine probe proved that direct `Select2(...)` can work on the created line, the live sketch segment, and both line endpoints. Additional bounded probe variants then showed that `AddHorizontalDimension2(...)` can still hang after endpoint selection and `AddVerticalDimension2(...)` can hang on a selected vertical line. That means the current failure is no longer explained by selection alone. The remaining unstable boundary is the dimension-creation call itself: the first orientation-specific calls can hang, and `AddDimension2(...)` can fail with `0x800706BE`.

### Alternatives rejected

- Continuing to frame the blocker as only a `SelectByID2(...)` issue
- Re-exposing `add_dimension` just because a direct selection fallback exists
- Assuming `AddDimension2(...)` is automatically the safe replacement for `AddHorizontalDimension2(...)`

### Impact

Future work on dimensions should probe dimension-creation APIs first. Selection mechanics are no longer the only or the main unknown for the first real dimension slice.

## 2026-04-10

### Decision

Freeze `add_dimension` outside the advertised real worker surface for the current milestone after the final bounded API probe round.

### Reason

The final bounded probe did not just retry the already-known hanging calls. It also tested the most sensible remaining minimal alternatives available from the local SolidWorks interop surface:

- typed `IAddHorizontalDimension2(...)`
- typed `IAddVerticalDimension2(...)`
- typed `IAddDimension2(...)`
- `ISketchManager.AddAlongXDimension(...)`
- `ISketchManager.AddAlongYDimension(...)`

All of them still produced unsafe real behavior on the baseline SolidWorks 2022 machine. At this point, continuing to probe the same API families would be persistence without information gain.

### Alternatives rejected

- Re-exposing `add_dimension` because selection itself can now succeed
- Keeping the current dimension work item as the immediate next product slice
- Continuing to spend session time on the same known-unsafe API families without a new hypothesis

### Impact

`add_dimension` stays out of the real worker surface for now. The next practical work should move to readback hardening, regression discipline, and packaging unless a genuinely new dimension-creation hypothesis appears.

## 2026-04-10

### Decision

Emit structured inspection artifacts from the external real e2e harness.

### Reason

The project still needs stronger external evidence for saved-file and reopened-file validation, especially while sketch-segment readback remains imperfect. A machine-readable inspection artifact improves evidence quality without widening the worker surface.

### Alternatives rejected

- Keeping all evidence only in free-form summary text
- Widening the worker just to ask for more diagnostics
- Building a large separate validation framework before adding a small useful artifact

### Impact

Real harness runs now produce `*-inspection.json` files with saved/exported file facts, reopened-document state, feature tree, and assertion results.

## 2026-04-10

### Decision

Standardize the current Windows worker packaging path around debug build plus framework-dependent `win-x64` publish, and validate the published worker executable against the real serialized baseline.

### Reason

The project needed a repeatable Windows execution discipline more than a flashy installer. A framework-dependent publish is enough on the validated company machine, keeps artifacts smaller, and is honest about the current deployment maturity. The key proof point is not that `dotnet publish` succeeds, but that the published executable can pass the current real SolidWorks baseline.

### Alternatives rejected

- Jumping directly to self-contained packaging
- Jumping directly to single-file packaging
- Building an installer layer before the publish path itself was validated

### Impact

The repository now has:

- preflight, build, publish, run, export-regression, and evidence-collection scripts
- repository-side build/publish manifests
- a published-worker baseline regression pass on real SolidWorks 2022

## 2026-04-10

### Decision

Expose the worker executable path to the real validation harness through an environment override instead of hardcoding only one executable path forever.

### Reason

The harness must now validate both the default debug worker and the published worker executable without forking the harness codebase or duplicating scenarios.

### Alternatives rejected

- Keeping the harness permanently tied to one hardcoded debug executable path
- Forking separate harnesses for built and published workers
- Widening the worker protocol just for packaging concerns

### Impact

Real regression scripts can now target:

- the default debug worker
- a specific published worker executable

while keeping the same scenario set and evidence format.

## 2026-04-10

### Decision

Align `backendMetadata.sliceName` to the currently proven worker milestone and report `level1-real-modeling-v1`.

### Reason

The original slice name was still historically tied to the bootstrap phase even after the real worker had already proven the full current Level 1 slice on SolidWorks 2022. Leaving the older name in place made handshake metadata less honest and less useful during regression review and evidence collection.

### Alternatives rejected

- Leaving the stale bootstrap-oriented slice name in the worker metadata
- Renaming the slice to something broader than the currently proven real surface
- Avoiding the metadata correction just because the protocol shape itself was already stable

### Impact

Worker handshake metadata, regression summaries, and inspection artifacts now expose a slice name that matches the current proven baseline without widening the actual CAD surface claim.

## 2026-04-10

### Decision

Structure external inspection artifacts around explicit evidence strength instead of treating every readback fact as equally strong.

### Reason

The project now has multiple kinds of external evidence with very different reliability on the company SolidWorks 2022 machine. Saved-file existence, exported STEP existence, reopened feature-tree evidence, and matching reopened document paths are strong signals. Sketch-segment counting, when available at all, is only a weaker hint. Putting those facts into separate groups keeps the evidence honest and easier to read.

### Alternatives rejected

- Keeping every inspection fact in one flat undifferentiated list
- Promoting sketch-segment counts to the primary geometric oracle
- Widening the worker surface just to obtain stronger validation facts internally

### Impact

`*-inspection.json` artifacts, summaries, regression manifests, and evidence bundles now carry a clearer distinction between strong, supporting, weak, and cautionary evidence.

## 2026-04-10

### Decision

Prefer one lightweight Windows cycle script over multiple manual invocations when running the current published-worker baseline locally.

### Reason

The repository already had the necessary individual scripts, but the manual sequence was still easy to reconstruct incorrectly. A single orchestration script provides one disciplined local path without pretending to be a product installer or a CI platform.

### Alternatives rejected

- Keeping the full local cycle only as a documented checklist
- Building a heavier installer or pseudo-enterprise pipeline first
- Duplicating the same orchestration logic in multiple wrapper scripts

### Impact

The repository now has `scripts\run-windows-worker-cycle.ps1` as the preferred local path for:

- preflight
- debug build plus worker tests
- framework-dependent publish
- wrapper smoke check
- serialized real regression
- evidence bundle collection

## 2026-04-10

### Decision

Keep `standard_minimal` as the default repository-side evidence-bundle mode, and make `full_with_cad_files` an explicit opt-in through `-IncludeCadFiles`.

### Reason

Routine reruns need readable evidence without copying every generated CAD artifact into the repository by default. At the same time, handoff and audit situations sometimes do need the saved `.SLDPRT` and exported `.step` files. A mode split keeps both needs honest.

### Alternatives rejected

- Always copying CAD files into every repository-side bundle
- Keeping bundle shape implicit and undocumented
- Creating a separate heavy bundle tool instead of using the same collector

### Impact

The evidence collector and the Windows cycle now expose explicit bundle modes, emit `evidence-summary.txt`, and preserve the default smaller bundle while still allowing a fuller evidence-preservation run.

## 2026-04-10

### Decision

Treat the current Windows cycle as the project's internal CI-like automation boundary, but keep it local and script-based rather than introducing a heavier CI platform.

### Reason

The repository now has a disciplined local path that already performs the useful sequence:

- preflight
- build
- publish
- smoke
- serialized real regression
- evidence bundle

Adding another orchestration layer right now would mostly duplicate behavior and risk hiding failures.

### Alternatives rejected

- Building a new pseudo-enterprise pipeline layer inside the repository
- Keeping the cycle purely as tribal knowledge and manual command ordering
- Introducing cloud CI for a machine-dependent SolidWorks validation problem

### Impact

`scripts\run-windows-worker-cycle.ps1` remains the preferred internal automation path, with:

- default routine use in `standard_minimal` mode
- optional fuller evidence capture through `-IncludeCadFiles`
- explicit cycle manifest and cycle summary artifacts for quick review

## 2026-04-10

### Decision

Keep artifact pruning repository-side, conservative, and dry-run by default.

### Reason

The repository-side artifact trees are useful but duplicative compared with the validation-root source artifacts. They are the right first place to manage growth, but deleting them automatically would still be risky without an explicit review step.

### Alternatives rejected

- Aggressive auto-deletion on every cycle run
- Pruning the validation-root raw scenario artifacts first
- Leaving retention entirely undocumented and manual

### Impact

The project now has a documented retention policy and a safe pruning helper that:

- plans deletions first
- scopes pruning to repository-side copied artifacts
- keeps recent, passing, mode-distinguishing, and failed-reference directories protected

## 2026-04-10

### Decision

Use a lightweight file-based artifact index and handoff pack, not a database or heavier cataloging layer.

### Reason

The artifact problem is mostly discoverability and handoff clarity, not query complexity. JSON/TXT indexes and a copied handoff pack are enough for the current scale and fit the existing repository discipline.

### Alternatives rejected

- Introducing a database-backed artifact catalog
- Building a larger dashboard or web UI
- Relying only on manual folder browsing and tribal knowledge

### Impact

The repository now has:

- `scripts\update-worker-artifact-index.ps1`
- `scripts\new-handoff-evidence-pack.ps1`
- index summaries for quick lookup
- a repeatable internal pack for review and developer handoff

## 2026-04-10

### Decision

Expose only the current proven public alpha tool surface from the default MCP server, even though the repository still contains a broader planned v1 tool list.

### Reason

The repository is now moving from internal prototype discipline toward a developer alpha. At that point, the tool list shown by `list_tools` and `list_available_tools` must match what is honestly supported today, not the broader mock/planning scope.

### Alternatives rejected

- Leaving the broader planned v1 tool set exposed by default
- Relying on documentation alone while the runtime still advertised unsupported tools
- Promoting blocked tools such as `add_dimension` just because partial mock or parsing scaffolding exists

### Impact

The default MCP server now exposes only the narrow public alpha surface:

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

The broader v1 tool list remains documented as planned/internal scope, not as current alpha support.

## 2026-04-10

### Decision

Treat the current external/distributable alpha as a repo-first developer package with setup docs and example client configs, not as an installer or desktop-extension product.

### Reason

The project is ready for a more developer-facing alpha story, but not yet for installer-grade or marketplace-grade packaging. The most honest next step is a shareable repository shape with clear boundary docs, quickstart guidance, and concrete Codex/Claude client examples.

### Alternatives rejected

- Building an installer before the setup path is validated externally
- Packaging immediately as a Claude Desktop extension without validating the surrounding packaging semantics
- Publishing a broader "download and it just works" claim than the evidence supports

### Impact

The repository now includes:

- public alpha boundary documentation
- MCP client setup guidance
- a quickstart for developers / alpha users
- example client configs under `configs\examples\`
- an alpha distribution plan that stays explicit about what is and is not ready

## 2026-04-13

### Decision

Make repo-root relative script execution the primary documented onboarding path for the developer alpha.

### Reason

The previous docs were too dependent on one historical checkout path and on `pwsh -File ...` command spelling. A repo-first alpha should be followable from the repository root with relative script invocation and minimal local adaptation.

### Alternatives rejected

- Keeping `C:\Tools\SOLIDWORKS-MCP` hardcoded as the main path in onboarding docs
- Treating PowerShell 7 invocation style as mandatory documentation even when the repository scripts can be run from a normal PowerShell session

### Impact

README, quickstart, and client setup docs now center the repo-root path instead of one remembered machine path.

## 2026-04-13

### Decision

Treat Codex config plus repo-scoped Claude Code `.mcp.json` as the primary repo-first client connection paths, while keeping manual Claude Desktop JSON as a secondary path.

### Reason

The project needed a cleaner answer to "how do I connect this alpha to a serious MCP client today?" Codex and Claude Code fit the repo-first story directly. Claude Desktop is still relevant, but its current official direction emphasizes extensions, which this repository does not ship yet.

### Alternatives rejected

- Keeping only a Claude Desktop manual JSON example
- Pretending the repository is already ready for a Claude Desktop extension package
- Avoiding a Claude Code example even though it matches the repo-first distribution model

### Impact

`configs/examples/` now includes a project-scoped Claude Code example, and the docs distinguish clearly between primary repo-first paths and the still-manual Claude Desktop path.

## 2026-04-13

### Decision

Exclude generated build and artifact directories from Biome checks, and make the verification script Windows-shell compatible.

### Reason

The second-path validation exposed that `npm run lint` and `npm run verify` could fail for reasons unrelated to source quality: generated .NET outputs under `bin/`, copied repository artifacts, and a Windows `spawnSync npm.cmd` incompatibility in the verifier.

### Alternatives rejected

- Letting generated outputs keep breaking source linting
- Telling developers to delete build artifacts before every verification run
- Leaving `verify` as a fragile convenience command

### Impact

`npm run lint` and `npm run verify` are now credible repo checks again for the source tree.

## 2026-04-13

### Decision

Refresh the dependency tree to remove current audit findings and set the clean verification baseline to Node `^20.19.0 || >=22.12.0`.

### Reason

The developer alpha should not present a green setup story while its normal verification command fails security audit. The minimal useful fix was to update the direct toolchain packages, override the vulnerable transitive packages, and align the documented Node baseline to the current verified dependency requirements.

### Alternatives rejected

- Leaving `npm audit` red while still presenting `verify` as the standard path
- Hiding the audit step entirely from repository verification
- Claiming support for lower Node versions than the refreshed dependency tree actually supports cleanly

### Impact

`npm run audit` is now clean, `npm run verify` is green again, and the developer alpha docs now state a more honest Node baseline for source verification.

## 2026-04-13

### Decision

Treat "second-machine ready" as a distinct state from "second-machine validated".

### Reason

This session proved that docs and scripts can survive a fresh checkout path, but it did not prove a full second independent developer-machine rerun. Keeping those two states separate prevents claim inflation while still capturing useful onboarding progress.

### Alternatives rejected

- Calling the project second-machine validated after a same-machine alternate-path simulation
- Ignoring the alternate-path evidence even though it exposed real onboarding friction

### Impact

Planning and distribution docs now distinguish:

- repo-first distributable
- second-machine ready
- second-machine validated

## 2026-04-13

### Decision

Do not start packaging and do not start a new CAD slice before the second-machine validation is complete.

### Reason

The biggest remaining uncertainty is still whether another developer can rerun the current alpha path cleanly. Packaging now would optimize presentation before transfer is proven, and a new CAD slice would widen surface area before the current one is externally revalidated.

### Alternatives rejected

- start Claude Desktop extension packaging immediately
- start installer-style packaging immediately
- start another CAD slice immediately after the baseline machine rerun

### Impact

The next milestone remains:

1. run the second-machine validation pack
2. fix only the friction found there
3. only then reconsider packaging

## 2026-04-13

### Decision

Do not call a machine "second-machine validated" unless it meets the clean Node baseline `^20.19.0 || >=22.12.0`, even if an already-prepared checkout still passes locally.

### Reason

The accessible real-machine execution showed that Node `20.18.0` can still appear workable when compatible native bindings are already present in `node_modules`, but that does not represent a clean repo-first validation environment. Treating that as a green second-machine result would hide real transfer risk.

### Alternatives rejected

- calling the validation green just because one warmed checkout passed
- leaving the Windows preflight node check as a generic command-exists pass
- downgrading the documented baseline to match one non-clean local machine

### Impact

The Windows preflight now flags below-baseline Node versions as `WARN`, and the second-machine report distinguishes between operational flow success and strict validation success.

## 2026-04-13

### Decision

Prefer `node.exe` and `npm.cmd` explicitly in the Windows preflight instead of relying on PowerShell command resolution.

### Reason

After the second machine moved to Node `24.13.0`, PowerShell resolved `npm` to `npm.ps1`, which is fragile inside constrained-language shells and broke the preflight even though the underlying toolchain was healthy. The minimal robust fix was to call the actual executable entrypoints directly.

### Alternatives rejected

- keeping the preflight on bare `npm` resolution
- treating the constrained-shell failure as a CAD or worker problem
- weakening the preflight by removing the npm check entirely

### Impact

The Windows preflight now measures the real Node/npm runtime more reliably across shells.

## 2026-04-13

### Decision

Mark the project as `second-machine validated` after the clean rerun on machine `401LT02346`.

### Reason

The target machine now meets the declared Node baseline, the repo-first validation pack passed, MCP discovery exposed the 13 public alpha tools, and the safe real SolidWorks 2022 circle workflow succeeded through `export_step`.

### Alternatives rejected

- keeping the validation open after the clean rerun passed
- discounting the real second-machine result because a stale rerun-only session issue had to be reset first

### Impact

Project state can now move from `second-machine ready` to `second-machine validated`, while staying explicit that packaging and CAD-surface expansion are still separate decisions.

## 2026-04-13

### Decision

Make the next step a lightweight packaging / handoff decision, not a new CAD slice.

### Reason

Now that the repo-first path is validated on a second machine, the highest-value next step is turning that validated alpha into a cleaner shareable handoff shape. Opening new CAD scope first would spend the newly earned confidence in the wrong place.

### Alternatives rejected

- immediately opening another CAD slice after validation closure
- staying indefinitely in validation mode after the path is already green

### Impact

The planning focus moves from validation closure to first structured packaging/handoff decisions, while the public alpha surface remains frozen.

## 2026-04-15

### Decision

Define the current distributable alpha as a repo-first source handoff plus a lightweight companion handoff package, with the evidence pack kept separate.

### Reason

The project is no longer blocked on second-machine validation. The real ambiguity is now delivery shape: what the receiver actually gets, what is optional, and what should stay internal. Separating source repo, companion handoff package, and optional evidence pack keeps the handoff concrete without pretending the project is installer-grade.

### Alternatives rejected

- continuing with only verbal or ad hoc handoff guidance
- collapsing repo, worker, docs, and evidence into one ambiguous "package"
- treating the evidence pack as the default onboarding artifact

### Impact

The repo now has explicit delivery-model and package-contents docs, and the handoff language can distinguish clearly between:

- public alpha supported
- repo-first distributable
- second-machine validated
- handoff-ready
- installer-grade

## 2026-04-15

### Decision

Add a lightweight `prepare-alpha-handoff.ps1` path and mark the project as handoff-ready for controlled technical sharing.

### Reason

After second-machine validation, the highest-value missing piece was a repeatable way to stage the essential handoff materials without turning the project into a fake installer. A small companion-package script reduces sender friction, keeps docs/config/examples together, and stays honest about the repo remaining the primary artifact.

### Alternatives rejected

- jumping straight to installer-like packaging
- leaving handoff assembly fully manual
- returning to a new CAD slice before the transfer shape is tested

### Impact

The project can now generate a curated alpha handoff folder with:

- essential docs
- config examples
- optional published worker copy
- optional evidence-pack copy
- a manifest and summary

The recommended next validation is a receiver dry-run of that package, not a new CAD tool.

## 2026-04-15

### Decision

Do not start a first Prompt MCP or Resource MCP extension before the new alpha handoff package has been exercised by a receiver.

### Reason

The project goal is broader natural-language usability, but the immediate leverage is still packaging and transfer clarity. Adding Prompt or Resource MCP surface now would add one more concept to explain before confirming that the basic repo-first alpha handoff is already easy enough to receive and connect.

### Alternatives rejected

- opening a first Prompt MCP immediately after handoff packaging
- opening a first Resource MCP immediately after handoff packaging
- treating MCP affordance expansion as more urgent than validating the new handoff shape

### Impact

The next recommended step remains:

1. receiver dry-run of the current alpha handoff package
2. minimal packaging/handoff fixes
3. only then reevaluate whether Prompt MCP, Resource MCP, or a new CAD slice is the highest-value next move

## 2026-04-15

### Decision

Mark the project as `receiver-validated` after the controlled dry-run of the alpha handoff package, and prefer a first non-CAD usability extension over returning to CAD immediately.

### Reason

The receiver dry-run exposed real but small handoff frictions:

- repo-only references needed to be made more explicit when reading the package copy
- the included worker copy needed a clearer receiver setup story

Those issues were fixed without widening scope. The result is a receiver-validated package shape rather than an installer-like product. With that packaging baseline now credible, the next best leverage is a first Prompt MCP or, secondarily, a first Resource MCP that improves natural-language usability without opening a new CAD slice.

### Alternatives rejected

- staying on packaging by default after the dry-run was already green apart from small doc fixes
- returning to a new CAD slice immediately after the first receiver validation
- claiming installer-grade readiness from a documentation-level receiver success

### Impact

Project state can now distinguish:

- public alpha supported
- repo-first distributable
- second-machine validated
- handoff-ready
- receiver-validated
- installer-grade

The next decision focus moves to the first non-CAD usability extension, with CAD surface still frozen.

## 2026-04-15

### Decision

Introduce `safe_modeling_session` as the first public Prompt MCP for `SOLIDWORKS-MCP`.

### Reason

The project had already reached:

- public alpha supported
- repo-first distributable
- second-machine validated
- handoff-ready
- receiver-validated

At that point, the next leverage was not another CAD tool but a narrow prompt that improves natural-language behavior without expanding the validated CAD scope.

`safe_modeling_session` was chosen because it reinforces the exact safe workflow already proven on the public alpha tool surface:

- new part
- supported plane selection
- sketch start
- supported primitive sketch geometry
- sketch close
- optional state check
- boss extrude
- save
- optional STEP export

The prompt also makes the boundary behavior explicit:

- do not claim support for `add_dimension`
- do not imply `cut_extrude` or `add_fillet`
- do not imply assemblies, drawings, or open/reopen-document editing
- use `get_document_state` instead of guessing state-dependent facts

### Alternatives rejected

- adding a second prompt before the first prompt shape was proven useful
- opening a first Resource MCP before establishing a minimal prompt-backed guidance layer
- returning to a new CAD slice immediately after receiver validation

### Impact

Project state can now distinguish:

- public alpha supported
- tool surface supported
- prompt surface supported
- repo-first distributable
- second-machine validated
- handoff-ready
- receiver-validated
- installer-grade

The next recommended decision is whether a first Resource MCP would add more value than a second prompt, while the validated CAD tool surface stays frozen.

## 2026-04-15

### Decision

Introduce `current_document_state` at `solidworks://document/current-state` as the first public Resource MCP for `SOLIDWORKS-MCP`.

### Reason

After the first prompt was in place, the next best non-CAD extension was a narrow read-only context surface that:

- helps a host preload useful session state
- does not require a tool call for initial context
- does not widen the validated CAD capability claims

The best first resource was the current document snapshot because it can be derived directly from the same normalized state model already used by `get_document_state`.

### Alternatives rejected

- treating the older technical resources as automatically public without narrowing the public story
- opening a broader project-status or tool-catalog resource as the first public resource
- returning to a new CAD slice before adding any read-only context surface

### Impact

Project state can now distinguish:

- tool surface supported
- prompt surface supported
- resource surface supported
- not yet supported

The public resource story stays intentionally narrow:

- one read-only resource
- current session only
- no document browsing
- no open/reopen behavior

The next decision is whether another small resource or a second prompt adds more leverage than returning to CAD.

## 2026-04-16

### Decision

Introduce `public_alpha_boundary` at `solidworks://alpha/public-boundary` as the second public Resource MCP for `SOLIDWORKS-MCP`.

### Reason

After `safe_modeling_session` and `current_document_state` were in place, the highest-value missing non-CAD piece was not another dynamic session view but a compact static boundary resource that:

- gives hosts and clients a machine-friendly summary of the supported public alpha surfaces
- helps keep natural-language planning aligned before tool execution starts
- does not widen the validated CAD capability claims

`public_alpha_boundary` was chosen because it complements `current_document_state` cleanly:

- `current_document_state` answers "what is true in this session right now?"
- `public_alpha_boundary` answers "what is this public alpha allowed to do at all?"

### Alternatives rejected

- exposing a broader project-status or planning-style resource as public alpha context
- adding a third public resource immediately
- returning to CAD before the non-CAD layer had both dynamic and static context surfaces

### Impact

Project state can now distinguish and expose:

- tool surface supported
- prompt surface supported
- resource surface supported with one dynamic and one static public resource
- not yet supported

The current non-CAD public alpha layer is now more complete:

- `safe_modeling_session` for workflow guidance
- `current_document_state` for dynamic session context
- `public_alpha_boundary` for static boundary context

The next decision should no longer be driven by "missing basic MCP affordances", but by whether a bounded CAD slice or another narrow non-CAD addition has the higher leverage.
