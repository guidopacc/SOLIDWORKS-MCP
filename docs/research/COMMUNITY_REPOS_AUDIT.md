# COMMUNITY REPOS AUDIT

## Scope

This file is the short-form companion to the broader ecosystem survey in:

- [SOLIDWORKS_MCP_ECOSYSTEM_AUDIT.md](./SOLIDWORKS_MCP_ECOSYSTEM_AUDIT.md)

It focuses on the community repositories most directly relevant to `SOLIDWORKS-MCP`.

## Audit principle

Community repositories are useful for:

- comparative study
- identifying engineering risks
- extracting useful patterns
- avoiding repeated mistakes

They are **not** treated as official references or implementation authority.

## Direct SolidWorks-control repositories reviewed

- `andrewbartels1/SolidworksMCP-python`
- `vespo92/SolidworksMCP-TS`
- `tylerstoltz/SW_MCP`
- `eyfel/mcp-server-solidworks`
- `Sam-Of-The-Arth/SolidWorks-MCP`

## Related but separate category

- `kilwizac/solidworks-api-mcp`

This project is useful, but it is a **documentation MCP**, not a live SolidWorks control server.

## Useful observations

- Community work confirms real demand for SolidWorks MCP workflows.
- Windows-specific operational reality is central in every serious attempt.
- The strongest public direct references still look early or partially validated.
- Version awareness is often acknowledged, but public validation evidence is usually thin.
- Mocking and local development without SolidWorks remain recurring pain points.
- The most robust patterns for testing and productization often come from adjacent CAD projects rather than SolidWorks-specific ones.

## Recurring weaknesses

- Tight coupling between tool layer and backend/runtime details
- Limited separation between mock capability and real CAD capability
- Large tool catalogs with weak validation evidence
- Incomplete handling of lifecycle, error recovery, and version-support boundaries
- Blurry separation between docs/knowledge assistance and actual CAD execution

## Practical takeaways for this project

- Keep using community repos as audit inputs, not as foundations to copy.
- Preserve the current `TypeScript MCP core + Windows worker` architecture.
- Prefer narrow, validated workflow tools over broad but weakly proven API surfaces.
- Keep documentation search concerns separate from real control capability.
- Maintain explicit verification status for every capability.
