import { describe, expect, it } from 'vitest';
import { SolidWorksWorkerBackend } from '../../adapters/solidworks/src/solidworks-worker-backend.js';
import type {
  SolidWorksWorkerRequest,
  SolidWorksWorkerResponse,
  SolidWorksWorkerTransport,
} from '../../adapters/solidworks/src/worker-protocol.js';
import { isSolidWorksMcpError } from '../../mcp-server/src/domain/errors.js';
import { createInitialProjectState } from '../../mcp-server/src/domain/state.js';

describe('SolidWorksWorkerBackend', () => {
  it('performs a handshake and then executes a command through the worker transport', async () => {
    const transport = new RecordingTransport((request) => {
      if (request.messageType === 'handshake_request') {
        return {
          messageType: 'handshake_response',
          requestId: request.requestId,
          protocolVersion: request.protocolVersion,
          worker: {
            name: 'solidworks-worker',
            version: '0.1.0',
          },
          supportedSolidWorksMajorVersions: [2022],
          capabilities: {
            supportsStateRoundTrip: true,
            supportsObservedStateOnFailure: true,
          },
        };
      }

      if (request.messageType !== 'execute_command_request') {
        throw new Error(`Unexpected request type: ${request.messageType}`);
      }

      return {
        messageType: 'execute_command_response',
        requestId: request.requestId,
        protocolVersion: request.protocolVersion,
        ok: true,
        nextState: {
          ...request.stateBefore,
          backendMode: 'solidworks',
          currentDocument: {
            documentType: 'part',
            name: 'Bracket',
            units: 'mm',
            sketches: [],
            features: [],
            exports: [],
            modified: false,
            baselineVersion: '2022',
          },
        },
        execution: {
          backendName: 'solidworks-worker',
          durationMs: 12,
          solidWorksMajorVersion: 2022,
        },
      };
    });

    const backend = new SolidWorksWorkerBackend(transport);
    const nextState = await backend.execute(
      { kind: 'new_part', name: 'Bracket' },
      createInitialProjectState([], 'solidworks'),
    );

    expect(nextState.currentDocument?.name).toBe('Bracket');
    expect(transport.requests).toHaveLength(2);
    expect(transport.requests[0]?.messageType).toBe('handshake_request');
    expect(transport.requests[1]?.messageType).toBe('execute_command_request');
  });

  it('maps worker error responses into typed project errors', async () => {
    const transport = new RecordingTransport((request) => {
      if (request.messageType === 'handshake_request') {
        return {
          messageType: 'handshake_response',
          requestId: request.requestId,
          protocolVersion: request.protocolVersion,
          worker: {
            name: 'solidworks-worker',
            version: '0.1.0',
          },
          supportedSolidWorksMajorVersions: [2022],
          capabilities: {
            supportsStateRoundTrip: true,
            supportsObservedStateOnFailure: true,
          },
        };
      }

      return {
        messageType: 'execute_command_response',
        requestId: request.requestId,
        protocolVersion: request.protocolVersion,
        ok: false,
        error: {
          code: 'precondition_failed',
          message: 'A plane must be selected before starting a sketch.',
          retryable: false,
          details: {
            command: 'start_sketch',
            resyncRequired: false,
          },
        },
        resyncRequired: false,
      };
    });

    const backend = new SolidWorksWorkerBackend(transport);
    const error = await captureAsyncError(() =>
      backend.execute(
        { kind: 'start_sketch' },
        createInitialProjectState([], 'solidworks'),
      ),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('precondition_failed');
      expect(error.details.command).toBe('start_sketch');
    }
  });

  it('rejects workers that do not advertise SolidWorks 2022 support', async () => {
    const transport = new RecordingTransport((request) => {
      if (request.messageType !== 'handshake_request') {
        throw new Error('Unexpected request before handshake completes');
      }

      return {
        messageType: 'handshake_response',
        requestId: request.requestId,
        protocolVersion: request.protocolVersion,
        worker: {
          name: 'solidworks-worker',
          version: '0.1.0',
        },
        supportedSolidWorksMajorVersions: [2024],
        capabilities: {
          supportsStateRoundTrip: true,
          supportsObservedStateOnFailure: true,
        },
      };
    });

    const backend = new SolidWorksWorkerBackend(transport);
    const error = await captureAsyncError(() =>
      backend.execute(
        { kind: 'new_part', name: 'Bracket' },
        createInitialProjectState([], 'solidworks'),
      ),
    );

    expect(isSolidWorksMcpError(error)).toBe(true);
    if (isSolidWorksMcpError(error)) {
      expect(error.code).toBe('unsupported_operation');
    }
  });
});

class RecordingTransport implements SolidWorksWorkerTransport {
  readonly requests: SolidWorksWorkerRequest[] = [];

  constructor(
    private readonly handler: (
      request: SolidWorksWorkerRequest,
    ) => SolidWorksWorkerResponse | Promise<SolidWorksWorkerResponse>,
  ) {}

  async send(
    request: SolidWorksWorkerRequest,
  ): Promise<SolidWorksWorkerResponse> {
    this.requests.push(request);
    return await this.handler(request);
  }
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
