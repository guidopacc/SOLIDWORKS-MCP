# READBACK HARDENING NOTES

## Date

- 2026-04-10

## Purpose

Strengthen external evidence for real SolidWorks 2022 runs without widening the worker surface.

## What changed

The real e2e harness now emits a structured inspection artifact for each scenario:

- `C:\SolidWorksMcpValidation\<scenario>\<scenario>-inspection.json`

The artifact records:

- schema version
- run timestamps and duration
- inspection mode (`reopened_saved_part` vs `active_session_document`)
- worker executable path used for the run
- protocol version and desired SolidWorks baseline
- structured artifact facts for:
  - input transcript
  - output transcript
  - summary
  - inspection json itself
  - saved `.SLDPRT`
  - exported `.step`
- outcome facts:
  - worker exit/error facts
  - completed command count and kinds
  - last execute failure metadata when present
- reopened-document title/path/modified flag
- feature readback:
  - profile/extrusion counts
  - profile/extrusion feature names
  - sketch segment count when available
  - feature tree count
  - feature type counts
  - reference planes found in the feature tree
- assertion summary grouped into:
  - passed
  - failed
  - other
- evidence summary grouped into:
  - strong
  - supporting
  - weak
  - cautions
- cleanup outcome

The text summary now also includes:

- `schemaVersion`
- `inspectionMode`
- `runStartedAtUtc`
- `runCompletedAtUtc`
- `durationMs`
- `workerExe`
- `protocolVersion`
- `desiredSolidWorksMajorVersion`
- `inspectionArtifactPath`
- `inputTranscriptLineCount`
- `outputTranscriptLineCount`
- `savePath`
- `saveExists`
- `saveSizeBytes`
- `profileFeatures`
- `extrusionFeatures`
- `featureTreeCount`
- `featureTypeCounts`
- `referencePlanes`
- `assertionPassCount`
- `assertionFailCount`
- `scenarioPassed`
- `strongEvidence`
- `supportingEvidence`
- `weakEvidence`
- `evidenceCautions`

## Why it helps

- evidence is now easier to consume programmatically without parsing free-form summary text
- saved-file presence and size are visible even when external geometry readback remains partial
- feature-tree evidence is preserved in both human-readable and machine-readable form
- the exact worker executable used for a run is now part of the evidence chain
- strong evidence is now explicitly separated from weaker hints such as sketch-segment counting
- the regression manifest and evidence bundle can now summarize run quality without reopening every scenario file
- evidence bundles now distinguish explicitly between:
  - `standard_minimal`
  - `full_with_cad_files`
- the evidence collector now emits a compact `evidence-summary.txt` alongside `evidence-manifest.json`
- the full Windows cycle now emits a compact `windows-worker-cycle-summary.txt` alongside `windows-worker-cycle.json`

## Limits still present

- reopened sketch-segment counting is still not fully reliable on this machine
- the new artifact improves evidence quality, but it does not turn that readback into a guaranteed geometric oracle
- copying CAD files into the repository evidence bundle is intentionally optional, not the default
