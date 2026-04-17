# WINDOWS BASELINE AUTOMATION

## Purpose

Document the preferred local Windows path for rerunning the currently proven SolidWorks 2022 baseline from any repo checkout.

This is a baseline-rerun helper, not a substitute for second-machine validation.

## Preferred command

```powershell
.\scripts\run-windows-worker-cycle.ps1
```

Optional fuller evidence mode:

```powershell
.\scripts\run-windows-worker-cycle.ps1 -IncludeCadFiles
```

## What the cycle does

1. runs Windows preflight
2. builds the debug worker and runs local worker tests
3. publishes the framework-dependent release worker
4. runs a minimal protocol smoke check through the wrapper
5. runs the serialized real SolidWorks baseline against the published worker
6. collects a repository-side evidence bundle

Default automation intent:

- keep the standard cycle as the routine path
- reserve `-IncludeCadFiles` for fuller evidence capture, not for every run
- refresh the artifact index after meaningful governance changes or before internal handoff
- use pruning only through an explicit dry-run review first

## Main outputs

- cycle manifest:
  - `<repo-root>\artifacts\worker\cycles\<timestamp>\windows-worker-cycle.json`
- cycle summary:
  - `<repo-root>\artifacts\worker\cycles\<timestamp>\windows-worker-cycle-summary.txt`
- smoke transcripts:
  - `<repo-root>\artifacts\worker\cycles\<timestamp>\worker-smoke.ndjson`
  - `<repo-root>\artifacts\worker\cycles\<timestamp>\worker-smoke.out.ndjson`
- regression stdout capture:
  - `<repo-root>\artifacts\worker\cycles\<timestamp>\worker-regression.stdout.txt`
- real regression manifest:
  - `C:\SolidWorksMcpValidation\regression-runs\worker-regression-*.json`
- evidence bundle:
  - `<repo-root>\artifacts\real-evidence\<timestamp>\`
  - includes `evidence-manifest.json`
  - includes `evidence-summary.txt`

## Why this exists

- to reduce manual reconstruction of the correct local Windows sequence
- to keep the proven baseline serialized and repeatable
- to package evidence collection with the same run, not as an afterthought

## Scope limit

This is an internal execution discipline helper.

It is not:

- an installer
- a deployment product
- a CI system
- a claim of broader CAD surface support
- proof of second-machine readiness by itself

## Adjacent governance helpers

- index refresh:
  - `.\scripts\update-worker-artifact-index.ps1`
- prune plan:
  - `.\scripts\prune-worker-artifacts.ps1`
- handoff pack:
  - `.\scripts\new-handoff-evidence-pack.ps1`
