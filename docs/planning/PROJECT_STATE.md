# PROJECT STATE

## Final goal

Build a robust MCP server for narrow, validation-backed SolidWorks automation on Windows, starting from a SolidWorks 2022 baseline and a repo-first developer workflow.

## Current milestone

The project is now in a validated repo-first developer alpha state with a first serious handoff shape, a first public prompt-backed guidance layer, and a two-resource public alpha context layer.

What changed through 2026-04-16:

- the default MCP surface is aligned to the currently proven real slice
- the repo-first quickstart, boundary docs, and client config examples have been tightened
- the target second machine was moved to a clean Node baseline and the repo-first validation pack now passes there through a real SolidWorks 2022 safe modeling flow
- the project now defines what "repo-first distributable" and "handoff-ready" mean in concrete delivery terms
- the repo now has a lightweight alpha handoff companion package path separate from the evidence pack
- the alpha handoff companion package has now passed a controlled receiver dry-run
- the MCP server now exposes a first public Prompt MCP for safe modeling-session guidance
- the MCP server now exposes a first public Resource MCP for current document state context
- the MCP server now exposes a second public Resource MCP for static public alpha boundary context

This does not yet mean:

- installer-grade packaging
- any widening of the CAD surface

## Current public alpha supported

Implemented:

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

Prompt surface supported:

- `safe_modeling_session`

Resource surface supported:

- `current_document_state`
- `public_alpha_boundary`

Tested on real SolidWorks 2022:

- `new_part`
- `select_plane` on `Front Plane`, `Top Plane`, and `Right Plane`
- `start_sketch`
- `draw_line`
- `draw_circle`
- `draw_centered_rectangle`
- `close_sketch`
- `extrude_boss`
- `save_part`
- `export_step`
- attach-to-existing-session behavior
- published-worker serialized baseline reruns

Repo-first distributable today:

- repository source
- alpha handoff companion package under `artifacts/alpha-handoffs/...`
- build/publish/run scripts
- current docs and runbooks
- Codex and Claude-based config examples
- published worker artifact under `artifacts/worker/publish/...`

## Explicitly not public alpha supported

- `add_dimension`
- `cut_extrude`
- `add_fillet`
- open/reopen document workflows
- assemblies
- drawings
- installer-grade packaging
- packaged Claude Desktop extension distribution

## State classification

### Designed

- broader v1 scope beyond the public alpha
- worker protocol and adapter boundary
- repo-first alpha handoff model

### Implemented

- TypeScript MCP server
- mock backend
- SolidWorks worker-backed runtime path
- Windows scripts for preflight, build, publish, run, regression, evidence, and handoff
- current public alpha docs and config examples

### Compiled locally

- TypeScript server: yes
- .NET worker: yes on the company Windows machine

### Tested locally

- `npm run build`: passing
- `npm run typecheck`: passing
- `npm run lint`: passing
- `npm test`: passing
- `npm run audit`: passing
- `npm run verify`: passing

Clean Node baseline for repo verification:

- `^20.19.0 || >=22.12.0`

### Tested on SolidWorks real

- yes for the current public alpha slice on the company Windows machine with SolidWorks 2022
- yes for the current public alpha slice on the validated second Windows machine with SolidWorks 2022

### Public alpha supported

- yes for the narrow workflow described in `docs/architecture/PUBLIC_ALPHA_BOUNDARY.md`

### Prompt surface supported

- yes for `safe_modeling_session`
- yes as a narrow guidance layer on top of the current validated tool surface
- no as a claim of broader CAD support

### Resource surface supported

- yes for `current_document_state` and `public_alpha_boundary`
- yes as a dynamic-plus-static read-only context layer aligned to the validated state model and boundary docs
- no as a claim of document browsing or open/reopen support

### Repo-first distributable

- yes for technical developers
- no for installer-grade or extension-grade distribution

### Handoff-ready

- yes for controlled technical handoff
- yes with the companion package prepared by `.\scripts\prepare-alpha-handoff.ps1`
- no for installer-grade or marketplace-style distribution

### Receiver-validated

- yes for the controlled package dry-run documented in `docs/architecture/RECEIVER_DRY_RUN_REPORT.md`
- no for installer-grade or extension-grade packaging
- no as a substitute for future real external receiver feedback

### Second-machine validated

- yes on machine `401LT02346`
- yes after Node baseline closure to `v24.13.0`
- yes for the repo-first validation pack and the safe circle workflow through `export_step`

## Current evidence across the active alpha hardening cycle

Validated across the latest alpha hardening sessions:

- Windows preflight
- release worker publish
- direct published-worker smoke check
- serialized real regression against the published worker
- stdio MCP client connection with worker backend enabled
- 13-tool public alpha discovery through MCP
- same-machine alternate checkout path build
- same-machine alternate checkout path preflight
- same-machine alternate checkout path worker publish
- same-machine alternate checkout path stdio MCP 13-tool discovery
- second-machine repo-first sequence (`npm install`, `npm run build`, `npm test`, preflight, publish`)
- second-machine 13-tool MCP discovery
- second-machine safe circle workflow through `export_step`
- second-machine final saved part: `C:\Temp\alpha-second-machine-circle-final.SLDPRT`
- second-machine final STEP export: `C:\Temp\alpha-second-machine-circle-final.step`
- alpha handoff delivery model
- alpha package contents split
- first handoff guide
- lightweight alpha handoff companion package automation
- receiver dry-run checklist
- receiver dry-run report
- first public prompt discovery and retrieval validation
- first public resource discovery and retrieval validation
- second public resource discovery and retrieval validation

Latest regression manifest from the latest real SolidWorks validation session:

- `C:\SolidWorksMcpValidation\regression-runs\worker-regression-20260413-102318.json`

## Main current gaps

- current packaging is still repo-first and handoff-oriented
- Claude Desktop extension packaging is still not implemented
- the public prompt surface is still limited to one narrow prompt
- the public resource surface is still intentionally limited to a narrow two-resource layer
- `add_dimension` remains blocked and unsafe on the baseline SolidWorks 2022 machine
- reopened-part geometry readback is still weaker than file and feature-tree evidence
- repeated reruns against a stale validation-created `attached_existing` SolidWorks session can still be less stable than a clean-session rerun
- a real external receiver run would still be useful even though the controlled dry-run is now complete

## Current conclusion

The project is now meaningfully closer to a shareable developer alpha:

- a technical developer can build it
- publish the worker
- configure a client
- connect to the MCP server
- see the narrow supported surface
- run the proven workflow
- receive a curated handoff package instead of reconstructing the setup story manually
- understand the package/repo/worker relationship through a receiver-validated dry-run path

The project is still intentionally narrow and should stay that way while the next step shifts from the now-complete first non-CAD usability layer toward the next highest-leverage decision.
