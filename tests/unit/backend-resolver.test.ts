import { describe, expect, it } from 'vitest';
import { SolidWorksWorkerBackend } from '../../adapters/solidworks/src/solidworks-worker-backend.js';
import { resolveBackendRuntime } from '../../mcp-server/src/application/backend-resolver.js';

describe('resolveBackendRuntime', () => {
  it('defaults to the mock backend', async () => {
    const runtime = resolveBackendRuntime({});

    expect(runtime.backendKind).toBe('mock');
    expect(runtime.backend.mode).toBe('mock');

    await runtime.dispose();
  });

  it('builds the worker backend when worker env vars are present', async () => {
    const runtime = resolveBackendRuntime({
      SOLIDWORKS_MCP_BACKEND: 'solidworks-worker',
      SOLIDWORKS_WORKER_COMMAND: process.execPath,
      SOLIDWORKS_WORKER_ARGS_JSON: JSON.stringify(['--version']),
      SOLIDWORKS_WORKER_RESPONSE_TIMEOUT_MS: '2000',
      SOLIDWORKS_WORKER_SHUTDOWN_TIMEOUT_MS: '1000',
    });

    expect(runtime.backendKind).toBe('solidworks-worker');
    expect(runtime.backend.mode).toBe('solidworks');
    expect(runtime.backend).toBeInstanceOf(SolidWorksWorkerBackend);

    await runtime.dispose();
  });

  it('rejects invalid worker argument JSON', () => {
    expect(() =>
      resolveBackendRuntime({
        SOLIDWORKS_MCP_BACKEND: 'solidworks-worker',
        SOLIDWORKS_WORKER_COMMAND: process.execPath,
        SOLIDWORKS_WORKER_ARGS_JSON: '{"not":"an array"}',
      }),
    ).toThrow('SOLIDWORKS_WORKER_ARGS_JSON must be a JSON array of strings.');
  });
});
