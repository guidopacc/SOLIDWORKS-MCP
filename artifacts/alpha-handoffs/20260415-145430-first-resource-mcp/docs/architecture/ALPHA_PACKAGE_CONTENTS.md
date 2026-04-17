# ALPHA PACKAGE CONTENTS

## Purpose

Define what belongs in the current repo-first alpha handoff, what is optional, and what should stay out of the delivery shape.

## Package layers

The current handoff model has three layers:

1. source repository
2. alpha handoff companion package
3. optional evidence pack

Do not collapse these into one fake installer story.

## Mandatory for a real alpha handoff

These are the minimum pieces for a controlled technical handoff today.

| Path or artifact | Why it is mandatory |
| --- | --- |
| repository source checkout | the project is still repo-first; the companion package does not replace the repo |
| `README.md` | top-level project reality and boundary |
| `PROJECT_HANDOFF.md` | clean starting point for the next developer |
| `docs/getting-started/QUICKSTART_ALPHA.md` | shortest path from checkout to first use |
| `docs/architecture/MCP_CLIENT_SETUP.md` | current Codex / Claude connection path |
| `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md` | current supported surface and refusal boundary |
| `docs/architecture/FIRST_PROMPT_MCP.md` | defines the current public prompt surface and its honest limits |
| `docs/architecture/FIRST_RESOURCE_MCP.md` | defines the current public resource surface and its honest limits |
| `docs/architecture/ALPHA_DELIVERY_MODEL.md` | explains what "distributable alpha" means today |
| `docs/architecture/ALPHA_DISTRIBUTION_PLAN.md` | states the current repo-first distribution plan |
| `docs/architecture/ALPHA_PACKAGE_CONTENTS.md` | explains what is included and excluded |
| `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md` | sender / receiver read order and first actions |
| `configs/examples/` | concrete config starting points |
| published worker artifact or exact publish path | removes ambiguity around the worker execution path |
| `docs/architecture/SECOND_MACHINE_VALIDATION_REPORT.md` | proof that the repo-first path has been validated on a second machine |

## Useful but optional

These add value, but they should not be confused with the core alpha itself.

| Path or artifact | Why it is optional |
| --- | --- |
| `docs/architecture/SECOND_MACHINE_VALIDATION.md` | helpful when someone wants to repeat the validation path rather than just use the alpha |
| `docs/architecture/WINDOWS_EXECUTION_RUNBOOK.md` | useful for deeper Windows-side operation and reruns |
| `configs/examples/AGENTS.public-alpha-snippet.md` | helpful for clients that support project instructions |
| latest published worker manifest | helpful for provenance, not required for first understanding |
| latest handoff evidence pack | useful for deep technical review, not required for normal onboarding |
| `docs/architecture/WINDOWS_BASELINE_AUTOMATION.md` | useful for internal rerun discipline, not required for first client setup |

## Internal or non-delivery by default

These should usually stay out of an external or early-adopter handoff unless there is a specific reason.

| Path or artifact | Why it stays out by default |
| --- | --- |
| `docs/planning/PROJECT_STATE.md` | internal state tracking rather than first-use guidance |
| `docs/planning/NEXT_STEPS.md` | internal prioritization rather than external onboarding |
| `docs/planning/DECISIONS.md` | useful for internal continuity, but noisy for many receivers |
| `artifacts/real-evidence/` full history | too heavy for a default handoff |
| `artifacts/worker/cycles/` full history | operational history, not onboarding material |
| `artifacts/indexes/` and prune outputs | governance support, not delivery material |
| `node_modules/` | generated local dependency tree, not handoff material |
| `dist/` | generated output that can be rebuilt from source |
| `.recovery-backups/` | internal local safety data |

## What the companion package should contain

The companion package prepared by `.\scripts\prepare-alpha-handoff.ps1` should contain:

- `alpha-handoff-summary.txt`
- `alpha-handoff-manifest.json`
- selected docs copied into a small curated tree
- `configs/examples/`
- published worker artifact when present
- optional copied evidence pack only when explicitly requested

Important behavior:

- the companion package does not need to duplicate every repo doc
- some references still intentionally resolve to the accompanying repository checkout
- the included worker copy is a convenience shortcut, not a claim that the package replaces the repo working root

It should not contain:

- full repo history
- `node_modules`
- a blanket copy of every artifact under `artifacts/`
- fake installer wrappers

## Audience-specific content split

### Internal developer handoff

Required:

- repository source
- companion package
- published worker artifact when available

Recommended extras:

- planning docs
- evidence pack
- Windows runbook and automation docs

### Early technical adopter handoff

Required:

- repository source
- companion package
- config examples
- boundary docs

Recommended extras:

- published worker artifact

Avoid:

- roadmap-heavy planning material
- raw artifact histories

### Technical review handoff

Required:

- repository source or snapshot
- companion package
- validation report

Recommended extras:

- evidence pack
- published worker manifest

## Current exclusions to keep honest

Do not package or claim the following as part of the current alpha handoff:

- installer binaries
- marketplace-ready packages
- Claude Desktop `.mcpb` extension packaging
- unsupported CAD tools just because mock support exists
- claims beyond the SolidWorks 2022 validated baseline
