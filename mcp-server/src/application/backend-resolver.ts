import { MockCadBackend } from '../../../adapters/mock/src/mock-cad-backend.js';
import { SolidWorksWorkerBackend } from '../../../adapters/solidworks/src/solidworks-worker-backend.js';
import { StdioWorkerTransport } from '../../../adapters/solidworks/src/stdio-worker-transport.js';
import type { CadBackend } from './cad-backend.js';

export type RuntimeBackendKind = 'mock' | 'solidworks-worker';

export interface BackendRuntime {
  backend: CadBackend;
  backendKind: RuntimeBackendKind;
  dispose: () => Promise<void>;
}

export function resolveBackendRuntime(
  env: NodeJS.ProcessEnv = process.env,
): BackendRuntime {
  const backendKind = normalizeBackendKind(env.SOLIDWORKS_MCP_BACKEND);

  if (backendKind === 'mock') {
    return {
      backend: new MockCadBackend(),
      backendKind,
      dispose: async () => {},
    };
  }

  const command = env.SOLIDWORKS_WORKER_COMMAND;
  if (!command) {
    throw new Error(
      'SOLIDWORKS_WORKER_COMMAND is required when SOLIDWORKS_MCP_BACKEND=solidworks-worker.',
    );
  }

  const args = parseArgsJson(env.SOLIDWORKS_WORKER_ARGS_JSON);
  const responseTimeoutMs = parseOptionalPositiveInt(
    env.SOLIDWORKS_WORKER_RESPONSE_TIMEOUT_MS,
    'SOLIDWORKS_WORKER_RESPONSE_TIMEOUT_MS',
  );
  const shutdownTimeoutMs = parseOptionalPositiveInt(
    env.SOLIDWORKS_WORKER_SHUTDOWN_TIMEOUT_MS,
    'SOLIDWORKS_WORKER_SHUTDOWN_TIMEOUT_MS',
  );

  const transport = new StdioWorkerTransport({
    command,
    args,
    cwd: env.SOLIDWORKS_WORKER_CWD,
    env,
    responseTimeoutMs,
    shutdownTimeoutMs,
  });

  return {
    backend: new SolidWorksWorkerBackend(transport, {
      timeoutMs: responseTimeoutMs,
    }),
    backendKind,
    dispose: async () => {
      await transport.close();
    },
  };
}

function normalizeBackendKind(value: string | undefined): RuntimeBackendKind {
  if (!value || value === 'mock') {
    return 'mock';
  }

  if (value === 'solidworks-worker') {
    return value;
  }

  throw new Error(
    `Unsupported SOLIDWORKS_MCP_BACKEND value: ${value}. Expected "mock" or "solidworks-worker".`,
  );
}

function parseArgsJson(value: string | undefined): string[] {
  if (!value) {
    return [];
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(value);
  } catch (error) {
    throw new Error(
      `SOLIDWORKS_WORKER_ARGS_JSON must be a JSON array of strings. ${
        error instanceof Error ? error.message : String(error)
      }`,
    );
  }

  if (
    !Array.isArray(parsed) ||
    parsed.some((entry) => typeof entry !== 'string')
  ) {
    throw new Error(
      'SOLIDWORKS_WORKER_ARGS_JSON must be a JSON array of strings.',
    );
  }

  return [...parsed];
}

function parseOptionalPositiveInt(
  value: string | undefined,
  variableName: string,
): number | undefined {
  if (!value) {
    return undefined;
  }

  const parsed = Number.parseInt(value, 10);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    throw new Error(
      `${variableName} must be a positive integer when provided.`,
    );
  }

  return parsed;
}
