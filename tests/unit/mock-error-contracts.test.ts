import { describe, expect, it } from 'vitest';
import { MockCadBackend } from '../../adapters/mock/src/mock-cad-backend.js';
import { isSolidWorksMcpError } from '../../mcp-server/src/domain/errors.js';
import { createInitialProjectState } from '../../mcp-server/src/domain/state.js';

describe('mock backend error contracts', () => {
  it('returns precondition_failed when starting a sketch without a plane', async () => {
    const backend = new MockCadBackend();
    let state = createInitialProjectState([], 'mock');

    state = await backend.execute({ kind: 'new_part' }, state);

    const error = await captureAsyncError(() =>
      backend.execute({ kind: 'start_sketch' }, state),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('precondition_failed');
    }
  });

  it('returns not_found when dimensioning an unknown entity', async () => {
    const backend = new MockCadBackend();
    let state = createInitialProjectState([], 'mock');

    state = await backend.execute({ kind: 'new_part' }, state);
    state = await backend.execute(
      { kind: 'select_plane', plane: 'Top Plane' },
      state,
    );
    state = await backend.execute({ kind: 'start_sketch' }, state);

    const error = await captureAsyncError(() =>
      backend.execute(
        {
          kind: 'add_dimension',
          entityId: 'line-404',
          value: 10,
        },
        state,
      ),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('not_found');
    }
  });

  it('returns precondition_failed when extruding with an open sketch', async () => {
    const backend = new MockCadBackend();
    let state = createInitialProjectState([], 'mock');

    state = await backend.execute({ kind: 'new_part' }, state);
    state = await backend.execute(
      { kind: 'select_plane', plane: 'Right Plane' },
      state,
    );
    state = await backend.execute({ kind: 'start_sketch' }, state);

    const error = await captureAsyncError(() =>
      backend.execute(
        {
          kind: 'extrude_boss',
          sketchId: 'sketch-1',
          depth: 8,
        },
        state,
      ),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('precondition_failed');
    }
  });
});

async function captureAsyncError(
  callback: () => Promise<unknown>,
): Promise<unknown> {
  try {
    await callback();
  } catch (error) {
    return error;
  }

  throw new Error('Expected callback to throw.');
}
