import { describe, expect, it } from 'vitest';
import { isSolidWorksMcpError } from '../../mcp-server/src/domain/errors.js';
import { getToolDefinition } from '../../mcp-server/src/tools/tool-definitions.js';

describe('tool definition validation', () => {
  it('rejects unsupported plane names', () => {
    const definition = getToolDefinition('select_plane');
    if (!definition || definition.kind !== 'command') {
      throw new Error('select_plane definition missing');
    }

    const error = captureSyncError(() =>
      definition.parse({ plane: 'Diagonal Plane' }),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('invalid_input');
    }
  });

  it('rejects invalid mergeResult values', () => {
    const definition = getToolDefinition('extrude_boss');
    if (!definition || definition.kind !== 'command') {
      throw new Error('extrude_boss definition missing');
    }

    const error = captureSyncError(() =>
      definition.parse({
        sketchId: 'sketch-1',
        depth: 12,
        mergeResult: 'yes',
      }),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('invalid_input');
    }
  });
});

function captureSyncError(callback: () => unknown): unknown {
  try {
    callback();
  } catch (error) {
    return error;
  }

  throw new Error('Expected callback to throw.');
}
