import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { SolidWorksWorkerBackend } from '../../adapters/solidworks/src/solidworks-worker-backend.js';
import { StdioWorkerTransport } from '../../adapters/solidworks/src/stdio-worker-transport.js';
import { isSolidWorksMcpError } from '../../mcp-server/src/domain/errors.js';
import { createInitialProjectState } from '../../mcp-server/src/domain/state.js';

describe('StdioWorkerTransport', () => {
  it('spawns a worker fixture and completes a command round-trip', async () => {
    const transport = createFixtureTransport();
    const backend = new SolidWorksWorkerBackend(transport);

    const nextState = await backend.execute(
      { kind: 'new_part', name: 'FixturePart' },
      createInitialProjectState([], 'solidworks'),
    );

    expect(nextState.currentDocument?.name).toBe('FixturePart');
    await transport.close();
  });

  it('preserves observedState and resyncRequired on worker failures', async () => {
    const transport = createFixtureTransport();
    const backend = new SolidWorksWorkerBackend(transport);
    const initialState = createInitialProjectState(
      ['start_sketch'],
      'solidworks',
    );

    const error = await captureAsyncError(() =>
      backend.execute({ kind: 'start_sketch' }, initialState),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('precondition_failed');
      expect(error.details.resyncRequired).toBe(true);
      expect(error.details.observedState).toEqual(initialState);
    }

    await transport.close();
  });
});

function createFixtureTransport(): StdioWorkerTransport {
  return new StdioWorkerTransport({
    command: process.execPath,
    args: [
      fileURLToPath(
        new URL('../fixtures/solidworks-worker-fixture.mjs', import.meta.url),
      ),
    ],
    responseTimeoutMs: 2_000,
    shutdownTimeoutMs: 1_000,
  });
}

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
