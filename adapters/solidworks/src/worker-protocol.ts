import type { CadCommand } from '../../../mcp-server/src/domain/commands.js';
import {
  internalError,
  SolidWorksMcpError,
  type SolidWorksMcpErrorCode,
  unsupportedOperation,
} from '../../../mcp-server/src/domain/errors.js';
import type { ProjectRuntimeState } from '../../../mcp-server/src/domain/state.js';

export const SOLIDWORKS_WORKER_PROTOCOL_VERSION = '0.1.0';
export const SOLIDWORKS_BASELINE_MAJOR_VERSION = 2022;

export interface WorkerIdentity {
  name: string;
  version: string;
}

export interface WorkerCapabilities {
  supportsStateRoundTrip: boolean;
  supportsObservedStateOnFailure: boolean;
}

export interface WorkerExecutionMetadata {
  backendName: string;
  durationMs: number;
  solidWorksMajorVersion?: number;
}

export interface WorkerErrorPayload {
  code: SolidWorksMcpErrorCode;
  message: string;
  retryable: boolean;
  details: Record<string, unknown>;
}

export interface HandshakeRequest {
  messageType: 'handshake_request';
  requestId: string;
  protocolVersion: string;
  client: WorkerIdentity;
  desiredSolidWorksMajorVersion: number;
}

export interface HandshakeResponse {
  messageType: 'handshake_response';
  requestId: string;
  protocolVersion: string;
  worker: WorkerIdentity;
  supportedSolidWorksMajorVersions: number[];
  capabilities: WorkerCapabilities;
}

export interface ExecuteCommandRequest {
  messageType: 'execute_command_request';
  requestId: string;
  protocolVersion: string;
  command: CadCommand;
  stateBefore: ProjectRuntimeState;
  desiredSolidWorksMajorVersion: number;
  timeoutMs?: number;
}

export interface ExecuteCommandSuccessResponse {
  messageType: 'execute_command_response';
  requestId: string;
  protocolVersion: string;
  ok: true;
  nextState: ProjectRuntimeState;
  execution: WorkerExecutionMetadata;
}

export interface ExecuteCommandFailureResponse {
  messageType: 'execute_command_response';
  requestId: string;
  protocolVersion: string;
  ok: false;
  error: WorkerErrorPayload;
  observedState?: ProjectRuntimeState;
  resyncRequired: boolean;
}

export interface ShutdownRequest {
  messageType: 'shutdown_request';
  requestId: string;
  protocolVersion: string;
}

export interface ShutdownResponse {
  messageType: 'shutdown_response';
  requestId: string;
  protocolVersion: string;
}

export type SolidWorksWorkerRequest =
  | HandshakeRequest
  | ExecuteCommandRequest
  | ShutdownRequest;

export type SolidWorksWorkerResponse =
  | HandshakeResponse
  | ExecuteCommandSuccessResponse
  | ExecuteCommandFailureResponse
  | ShutdownResponse;

export interface SolidWorksWorkerTransport {
  send(request: SolidWorksWorkerRequest): Promise<SolidWorksWorkerResponse>;
}

export function createHandshakeRequest(requestId: string): HandshakeRequest {
  return {
    messageType: 'handshake_request',
    requestId,
    protocolVersion: SOLIDWORKS_WORKER_PROTOCOL_VERSION,
    client: {
      name: 'solidworks-mcp',
      version: '0.1.0',
    },
    desiredSolidWorksMajorVersion: SOLIDWORKS_BASELINE_MAJOR_VERSION,
  };
}

export function createExecuteCommandRequest(
  requestId: string,
  command: CadCommand,
  stateBefore: ProjectRuntimeState,
  timeoutMs?: number,
): ExecuteCommandRequest {
  return {
    messageType: 'execute_command_request',
    requestId,
    protocolVersion: SOLIDWORKS_WORKER_PROTOCOL_VERSION,
    command,
    stateBefore,
    desiredSolidWorksMajorVersion: SOLIDWORKS_BASELINE_MAJOR_VERSION,
    timeoutMs,
  };
}

export function assertHandshakeResponse(
  response: SolidWorksWorkerResponse,
): HandshakeResponse {
  if (response.messageType !== 'handshake_response') {
    throw internalError(
      'Worker returned a non-handshake response to handshake.',
    );
  }

  if (response.protocolVersion !== SOLIDWORKS_WORKER_PROTOCOL_VERSION) {
    throw unsupportedOperation('Worker protocol version mismatch.', {
      expected: SOLIDWORKS_WORKER_PROTOCOL_VERSION,
      received: response.protocolVersion,
    });
  }

  if (
    !response.supportedSolidWorksMajorVersions.includes(
      SOLIDWORKS_BASELINE_MAJOR_VERSION,
    )
  ) {
    throw unsupportedOperation(
      'Worker does not advertise SolidWorks 2022 support.',
      {
        expectedSolidWorksMajorVersion: SOLIDWORKS_BASELINE_MAJOR_VERSION,
        supportedSolidWorksMajorVersions:
          response.supportedSolidWorksMajorVersions,
      },
    );
  }

  return response;
}

export function assertExecuteCommandResponse(
  response: SolidWorksWorkerResponse,
): ExecuteCommandSuccessResponse | ExecuteCommandFailureResponse {
  if (response.messageType !== 'execute_command_response') {
    throw internalError(
      'Worker returned a non-command response to execute_command_request.',
    );
  }

  if (response.protocolVersion !== SOLIDWORKS_WORKER_PROTOCOL_VERSION) {
    throw unsupportedOperation('Worker protocol version mismatch.', {
      expected: SOLIDWORKS_WORKER_PROTOCOL_VERSION,
      received: response.protocolVersion,
    });
  }

  return response;
}

export function toSolidWorksWorkerError(
  response: ExecuteCommandFailureResponse,
): SolidWorksMcpError {
  return new SolidWorksMcpError(
    response.error.code,
    response.error.message,
    {
      ...response.error.details,
      resyncRequired: response.resyncRequired,
      observedState: response.observedState,
    },
    response.error.retryable,
  );
}
