import { describe, expect, it } from 'vitest';
import { MockCadBackend } from '../../adapters/mock/src/mock-cad-backend.js';
import { createInitialProjectState } from '../../mcp-server/src/domain/state.js';

describe('MockCadBackend', () => {
  it('executes a basic part-to-feature flow', async () => {
    const backend = new MockCadBackend();
    let state = createInitialProjectState([], 'mock');

    state = await backend.execute({ kind: 'new_part', name: 'Bracket' }, state);
    state = await backend.execute(
      { kind: 'select_plane', plane: 'Front Plane' },
      state,
    );
    state = await backend.execute({ kind: 'start_sketch' }, state);
    state = await backend.execute(
      {
        kind: 'draw_line',
        start: { x: 0, y: 0 },
        end: { x: 10, y: 0 },
      },
      state,
    );
    state = await backend.execute(
      {
        kind: 'draw_circle',
        center: { x: 5, y: 5 },
        radius: 2,
      },
      state,
    );
    state = await backend.execute({ kind: 'close_sketch' }, state);
    state = await backend.execute(
      {
        kind: 'extrude_boss',
        sketchId: 'sketch-1',
        depth: 10,
      },
      state,
    );

    expect(state.currentDocument?.name).toBe('Bracket');
    expect(state.currentDocument?.sketches).toHaveLength(1);
    expect(state.currentDocument?.sketches[0]?.entities).toHaveLength(2);
    expect(state.currentDocument?.features).toHaveLength(1);
    expect(state.currentDocument?.features[0]).toMatchObject({
      kind: 'extrude_boss',
      sketchId: 'sketch-1',
      depth: 10,
    });
  });

  it('tracks save and export operations', async () => {
    const backend = new MockCadBackend();
    let state = createInitialProjectState([], 'mock');

    state = await backend.execute({ kind: 'new_part', name: 'Plate' }, state);
    state = await backend.execute(
      { kind: 'save_part', path: 'C:/temp/plate.sldprt' },
      state,
    );
    state = await backend.execute(
      { kind: 'export_step', path: 'C:/temp/plate.step' },
      state,
    );

    expect(state.currentDocument?.savedPath).toBe('C:/temp/plate.sldprt');
    expect(state.currentDocument?.exports).toEqual([
      { kind: 'step', path: 'C:/temp/plate.step' },
    ]);
  });
});
