# PACKAGING DECISION

## Date

- 2026-04-10

## Decision

Use a minimal Windows worker packaging strategy based on:

- `dotnet build` for local debug execution
- `dotnet publish` framework-dependent for `win-x64`
- no self-contained publish
- no single-file publish
- no installer layer at this stage

## Why

- the baseline company machine already has a working `.NET 8` installation
- framework-dependent publish keeps the artifact smaller and easier to inspect
- avoiding self-contained and single-file packaging removes noise while the real SolidWorks baseline is still validation-driven
- the real goal of this milestone is execution discipline, not product theater

## Status

- designed: yes
- implemented: yes
- compiled locally: yes
- tested locally: yes
- tested on real SolidWorks 2022: yes

Real-machine evidence:

- the published worker at
  - `C:\Tools\SOLIDWORKS-MCP\artifacts\worker\publish\framework-dependent\win-x64\Release\SolidWorksWorker.exe`
- passed the current sequential real regression baseline on SolidWorks 2022

## Practical consequences

The repository now has a disciplined Windows path for:

- preflight
- debug build
- release publish
- direct worker execution
- baseline regression against either:
  - the default debug worker
  - a specific published worker executable

## Deferred

The following remain explicitly deferred:

- self-contained publish
- single-file publish
- MSI/MSIX/installer work
- automatic worker discovery in external product packaging
