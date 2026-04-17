# INSPECTION ARTIFACT SCHEMA

## Purpose

Describe the structure and intent of the real-machine `*-inspection.json` artifacts emitted by the external Windows harness.

## Current schema version

- `inspection-2026-04-10-v2`

## Top-level groups

- `scenario`, `workerExe`, `protocolVersion`, `desiredSolidWorksMajorVersion`
- `inspectionMode`
  - `reopened_saved_part`
  - `active_session_document`
- `runStartedAtUtc`, `runCompletedAtUtc`, `durationMs`
- legacy convenience fields such as `savePath`, `saveExists`, `exportPath`, `scenarioPassed`
- `artifacts`
- `outcome`
- `reopenedDocument`
- `featureReadback`
- `assertionSummary`
- `evidence`
- `cleanup`

## Artifacts

`artifacts` records structured facts for:

- input transcript
- output transcript
- summary
- inspection json itself
- saved `.SLDPRT`
- exported `.step`

Each artifact currently exposes:

- `path`
- `exists`
- `sizeBytes`
- `extension`
- `lineCount` when relevant

## Outcome

`outcome` captures execution-level facts such as:

- worker exit code and stderr
- last execute error metadata when present
- requested vs completed command count
- completed command kinds
- final `scenarioPassed`

## Feature readback

`featureReadback` is the main external model-inspection block. It records:

- profile feature count and names
- extrusion feature count and names
- `sketchSegmentCount` when available
- feature tree count
- feature type counts
- reference planes seen in the feature tree
- full feature tree snapshot

## Assertion summary

Assertions are grouped explicitly into:

- `passed`
- `failed`
- `other`

Counts are included so downstream manifests can summarize runs quickly.

## Evidence strength

The `evidence` block separates inspection facts into:

- `strong`
- `supporting`
- `weak`
- `cautions`

Current intent:

- strong: files on disk, reopened-document path matches, profile/extrusion features found
- supporting: feature-tree summary, worker exit code, reference planes
- weak: sketch-segment counting and similar hints
- cautions: known limitations or missing weaker signals

## Important honesty rule

The artifact is designed to improve external evidence, not to pretend that all readback is equally authoritative.

In particular:

- saved/exported files plus feature-tree evidence are stronger than segment-count readback
- absence of `sketchSegmentCount` is not by itself a failure
- the artifact should support honest validation claims, not overstate geometry certainty
