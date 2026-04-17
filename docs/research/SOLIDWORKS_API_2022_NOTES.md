# SOLIDWORKS API 2022 NOTES

## Primary official source family reviewed

SolidWorks API Help 2022 on `help.solidworks.com`, including pages for:

- Accessing SolidWorks objects
- `ISldWorks::INewDocument2`
- `IModelDocExtension::SelectByID2`
- `ISketchManager::InsertSketch`
- Sketch creation methods such as line and circle creation
- Dimension creation methods
- `IFeatureManager::FeatureExtrusion2`
- `IModelDocExtension::SaveAs3`

## v1-relevant areas

- Application and document acquisition
- Plane selection and sketch lifecycle
- Sketch primitives
- Dimension creation
- Boss/cut feature creation
- Save/export operations

## Architecture consequences

- The real adapter must own SolidWorks session lifecycle explicitly.
- Sketch and feature commands should be wrapped behind backend methods that preserve our normalized state contract.
- Save/export behavior must report explicit failure reasons because filesystem and document state issues are likely in real usage.

## Unverified points

- Exact implementation details for robust feature-definition usage on SolidWorks 2022
- Recovery behavior after failed COM calls
- Cross-version deltas between 2022 and newer releases for the v1 command set

## Evidence rule

Nothing in this note should be read as proof that the repository currently supports live SolidWorks execution. That must be verified on Windows with a real installation.

