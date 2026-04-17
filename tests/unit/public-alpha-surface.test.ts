import { describe, expect, it } from 'vitest';
import {
  getToolDefinition,
  listPlannedButNotPublicToolNames,
  PUBLIC_ALPHA_TOOL_DEFINITIONS,
} from '../../mcp-server/src/tools/tool-definitions.js';

describe('public alpha surface', () => {
  it('keeps unstable or not-yet-supported tools outside the public list', () => {
    const publicAlphaNames = PUBLIC_ALPHA_TOOL_DEFINITIONS.map(
      (definition) => definition.name,
    );

    expect(publicAlphaNames).toContain('draw_circle');
    expect(publicAlphaNames).toContain('export_step');
    expect(publicAlphaNames).not.toContain('add_dimension');
    expect(publicAlphaNames).not.toContain('cut_extrude');
    expect(publicAlphaNames).not.toContain('add_fillet');
  });

  it('can still distinguish planned but not public tools', () => {
    expect(listPlannedButNotPublicToolNames()).toEqual(
      expect.arrayContaining(['add_dimension', 'cut_extrude', 'add_fillet']),
    );
    expect(getToolDefinition('add_dimension', 'public-alpha')).toBeUndefined();
    expect(getToolDefinition('add_dimension', 'all')).toBeDefined();
  });
});
