# ALPHA DELIVERY MODEL

## Purpose

Define what "repo-first distributable alpha" means now that the current public alpha surface is validated and the next problem is technical handoff, not new CAD capability.

## Status terms

- `public alpha supported`
  - the narrow 13-tool surface is validated and intentionally exposed by default
- `repo-first distributable`
  - a technical developer can receive the repository, build or publish the needed pieces, configure a serious MCP client, and use the proven workflow
- `second-machine validated`
  - the repo-first path has passed on a second real Windows + SolidWorks 2022 machine
- `handoff-ready`
  - the repo-first alpha can now be handed to another developer or reviewer with a curated delivery shape, clear setup docs, and a repeatable handoff-preparation step
- `installer-grade`
  - a packaged install or extension product; this project is not there yet

## Current classification

As of 2026-04-15:

- `public alpha supported`: yes
- `repo-first distributable`: yes
- `second-machine validated`: yes
- `handoff-ready`: yes for controlled technical handoff
- `installer-grade`: no

## Delivery units

The current alpha is not one monolithic package. It is a small set of clearly different delivery units.

### 1. Source repository

This is the primary deliverable.

It contains:

- source code
- scripts
- tests
- config examples
- boundary and setup docs

It is still the center of the alpha story. Nothing in the handoff flow replaces the repository.

### 2. Alpha handoff companion package

This is a lightweight, generated companion package for handoff clarity.

It contains a curated subset of:

- essential docs
- config examples
- validation notes
- optional published worker artifact
- optional copied evidence pack
- a manifest and a short summary

It is produced by:

- `.\scripts\prepare-alpha-handoff.ps1`

This companion package exists to reduce orientation friction. It is not an installer, release bundle, or substitute for the repo.

### 3. Evidence pack

This is optional and review-oriented.

It contains validation evidence copied from existing artifacts, not new execution logic.

It is produced by:

- `.\scripts\new-handoff-evidence-pack.ps1`

Use it when a reviewer needs proof, not when a developer simply needs first-use setup.

### 4. Internal planning state

This includes:

- `docs/planning/PROJECT_STATE.md`
- `docs/planning/NEXT_STEPS.md`
- `docs/planning/DECISIONS.md`

These remain important for internal project continuity, but they are not part of the minimum external handoff to an early adopter.

## What "distribute the alpha" means today

Distributing this alpha today means:

1. share the repository source
2. include or clearly point to the published worker path when the receiver should avoid a local republish step
3. provide the curated alpha handoff companion package
4. attach the evidence pack only when review depth requires it
5. keep the claim surface aligned to the validated 13-tool public alpha

It does not mean:

- one-click install
- MSI/MSIX packaging
- Claude Desktop extension packaging
- a broad "SolidWorks automation product" claim

## Audience-specific handoff shape

### Internal developer

Give:

- repository source
- alpha handoff companion package
- published worker artifact when available
- planning docs
- evidence pack when review or rerun context matters

### Early technical adopter

Give:

- repository source
- alpha handoff companion package
- config examples
- quickstart and boundary docs
- published worker artifact when possible

Do not lead with planning notes or broad roadmap material.

### Technical reviewer

Give:

- repository source or source snapshot
- alpha handoff companion package
- validation report
- optional evidence pack

The reviewer may not need the published worker unless they are expected to rerun the workflow personally.

## Honest boundaries

This delivery model does not change the current public alpha boundary:

- Windows only
- SolidWorks 2022 baseline
- narrow progressive part workflow
- 13-tool public alpha surface

Still not part of the distributable claim:

- `add_dimension`
- `cut_extrude`
- `add_fillet`
- open or reopen existing documents through the public MCP surface
- assemblies
- drawings
- installer-grade distribution

## Related documents

- `docs/architecture/ALPHA_PACKAGE_CONTENTS.md`
- `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md`
- `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md`
- `docs/architecture/SECOND_MACHINE_VALIDATION_REPORT.md`
