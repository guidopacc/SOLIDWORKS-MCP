# ALPHA DISTRIBUTION PLAN

## Purpose

Define the current honest distribution shape for the validated repo-first alpha.

## Current state classification

- `public alpha supported`: yes
- `repo-first distributable`: yes
- `second-machine validated`: yes
- `handoff-ready`: yes for controlled technical handoff
- `installer-grade`: no

## What "repo-first distributable alpha" means now

Today the project is distributable as a repo-first technical alpha, not as a polished installable product.

That means the alpha is:

- cloned or shared as source
- prepared as a repository plus a curated handoff companion package
- built and published locally by the developer when needed
- connected to a client through explicit MCP config
- explained through focused docs and runbooks

It does not mean:

- one-click install
- extension-store packaging
- broad compatibility claims

## Delivery units

The current distribution story has three distinct units:

1. repository source
2. alpha handoff companion package from `artifacts\alpha-handoffs\...`
3. optional evidence pack from `artifacts\handoff-packs\...`

See:

- `docs/architecture/ALPHA_DELIVERY_MODEL.md`
- `docs/architecture/ALPHA_PACKAGE_CONTENTS.md`
- `docs/architecture/FIRST_ALPHA_HANDOFF_GUIDE.md`

## Minimum honest share contents

The current minimum honest alpha handoff is:

1. repository source
2. focused docs for setup, boundary, and handoff
3. `configs/examples/` for Codex and Claude-based client setup
4. published worker artifact or an exact publish path
5. validation report for the current repo-first path
6. generated alpha handoff companion package

## What does not belong yet

- fake installer layers
- fake setup wizards
- unsupported tools promoted into the default surface
- "works with any SolidWorks workflow" wording
- a Claude Desktop extension package before that packaging path is actually validated

## Recommended handoff shape

For an internal or early-adopter technical handoff, the recommended shape is:

1. repository checkout
2. alpha handoff companion package from `artifacts\alpha-handoffs\...`
3. published worker under `artifacts/worker/publish/framework-dependent/win-x64/Release/` when available
4. optional evidence pack for review

## Current alpha-ready vs not alpha-ready

### Alpha-ready today

- narrow public alpha tool surface
- repo-first setup path
- Windows build/publish/run scripts
- real SolidWorks 2022 evidence for the current slice
- Codex and Claude-based config examples
- second-machine validation pack and exit criteria
- first repeatable alpha handoff companion package

### Not alpha-ready yet

- installer-grade packaging
- desktop-extension packaging for Claude Desktop
- broader SolidWorks version support claims
- richer CAD surface beyond the current public alpha

## Next distribution milestone

The next useful distribution step is not a new installer layer.

It is:

1. run one controlled receiver dry-run using the new alpha handoff companion package
2. remove only the friction found there
3. then decide whether a slightly more structured packaging step adds real value
4. only after that reconsider a new CAD slice

## Honesty rule

Describe the project today as:

- developer alpha
- Windows only
- SolidWorks 2022 baseline
- repo-first and script-driven
- narrow modeling slice
- handoff-ready for controlled technical sharing

Do not describe it as:

- complete SolidWorks MCP
- general-purpose CAD assistant
- installer-ready
