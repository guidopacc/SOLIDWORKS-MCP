# LOCAL AUTOMATION DECISION

## Date

- 2026-04-10

## Decision

Keep the current internal automation model lightweight and local:

- one preferred cycle entrypoint:
  - `scripts\run-windows-worker-cycle.ps1`
- one serialized real-regression entrypoint:
  - `scripts\run-real-worker-regression.ps1`
- one evidence collector:
  - `scripts\collect-real-worker-evidence.ps1`

Do not introduce a separate pseudo-CI framework or cloud pipeline as part of this milestone.

## Reason

The project already has the important primitives:

- Windows preflight
- worker build
- worker publish
- wrapper smoke check
- serialized real regression
- evidence bundling

The main value now is disciplined composition and readable artifacts, not more orchestration layers.

## Preferred operating modes

Routine local baseline:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-windows-worker-cycle.ps1
```

Fuller evidence-preservation run:

```powershell
pwsh -File C:\Tools\SOLIDWORKS-MCP\scripts\run-windows-worker-cycle.ps1 -IncludeCadFiles
```

## What this is

- local/internal automation
- CI-like in discipline
- explicit about failures
- grounded in the same scripts used manually

## What this is not

- a product installer
- a deployment system
- cloud CI
- a new worker/runtime layer

## Validation status

- implemented: yes
- tested locally: yes
- tested on real SolidWorks 2022:
  - standard cycle: yes
  - `-IncludeCadFiles` cycle: yes

## Consequence

The project can now recommend one routine path and one fuller evidence path without widening the CAD capability surface or pretending to have a heavier CI system than it really has.
