# ADD DIMENSION PROBE DECISION

## Date

- 2026-04-10

## Decision

Keep `add_dimension` out of the advertised real worker surface for the current milestone.

## Why this decision is now final for the milestone

The project already proved that sketch-entity selection is no longer the only unknown:

- direct `Select2(...)` can work on:
  - the created line
  - the live sketch segment
  - both line endpoints

The last bounded probe round then tried a few remaining minimal and sensible dimension-creation entry points on real SolidWorks 2022:

- late-bound `AddHorizontalDimension2(...)`
- late-bound `AddVerticalDimension2(...)`
- late-bound `AddDimension2(...)`
- typed `IAddHorizontalDimension2(...)`
- typed `IAddVerticalDimension2(...)`
- typed `IAddDimension2(...)`
- `ISketchManager.AddAlongXDimension(...)`
- `ISketchManager.AddAlongYDimension(...)`

Observed result:

- all of the orientation-specific and typed/sketch-manager alternatives still blocked the live SolidWorks session instead of returning deterministically
- the previous late-bound generic `AddDimension2(...)` probe had already shown `0x800706BE`
- no safe, repeatable, minimal quota-creation path was found on the baseline SolidWorks 2022 machine

## Practical consequence

For this milestone:

- `add_dimension` is still:
  - designed
  - partially scaffolded locally
  - compiled locally
  - probed on real SolidWorks 2022
- `add_dimension` is not:
  - validated on real SolidWorks 2022
  - part of the advertised real worker surface

## Guardrail

Do not keep retrying the same current API families blindly:

- `AddHorizontalDimension2(...)`
- `AddVerticalDimension2(...)`
- `AddDimension2(...)`
- `IAdd...Dimension2(...)`
- `ISketchManager.AddAlongXDimension(...)`
- `ISketchManager.AddAlongYDimension(...)`

Resume dimension work only when there is a genuinely new API hypothesis or a narrower precondition model to test.
