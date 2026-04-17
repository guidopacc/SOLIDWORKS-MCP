# VERSION SUPPORT

## Support language

- Designed: architecture and contracts account for the version
- Implemented: code path exists
- Tested in mock: validated only against the mock backend
- Tested on real SolidWorks: validated with a real installation
- Officially supported: verified and intentionally supported

## Current matrix

| SolidWorks version | Designed | Implemented | Tested in mock | Tested on real SolidWorks | Officially supported |
| --- | --- | --- | --- | --- | --- |
| 2022 | Yes | Mock path only | Yes | No | No |
| 2023+ | Version-aware intent only | No | No | No | No |

## Policy

- SolidWorks 2022 is the minimum design baseline.
- Newer versions should be treated as unverified until tested explicitly.
- The normalized state and backend contract should remain version-aware even before real validation exists.

