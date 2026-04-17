import type { CadBackend } from '../../../mcp-server/src/application/cad-backend.js';
import type { CadCommand } from '../../../mcp-server/src/domain/commands.js';
import type { ProjectRuntimeState } from '../../../mcp-server/src/domain/state.js';
import {
  assertExecuteCommandResponse,
  assertHandshakeResponse,
  createExecuteCommandRequest,
  createHandshakeRequest,
  type HandshakeResponse,
  SOLIDWORKS_WORKER_PROTOCOL_VERSION,
  type SolidWorksWorkerTransport,
  toSolidWorksWorkerError,
} from './worker-protocol.js';

export interface SolidWorksWorkerBackendOptions {
  requestIdFactory?: () => string;
  timeoutMs?: number;
}

export class SolidWorksWorkerBackend implements CadBackend {
  readonly backendName = 'solidworks-worker-backend';
  readonly mode = 'solidworks' as const;

  private handshake: Promise<HandshakeResponse> | null = null;
  private requestOrdinal = 0;

  constructor(
    private readonly transport: SolidWorksWorkerTransport,
    private readonly options: SolidWorksWorkerBackendOptions = {},
  ) {}

  async execute(
    command: CadCommand,
    state: ProjectRuntimeState,
  ): Promise<ProjectRuntimeState> {
    await this.ensureHandshake();

    const response = assertExecuteCommandResponse(
      await this.transport.send(
        createExecuteCommandRequest(
          this.nextRequestId(),
          command,
          state,
          this.options.timeoutMs,
        ),
      ),
    );

    if (!response.ok) {
      throw toSolidWorksWorkerError(response);
    }

    return {
      ...response.nextState,
      backendMode: this.mode,
      baselineVersion: state.baselineVersion,
      availableTools: [...state.availableTools],
    };
  }

  private async ensureHandshake(): Promise<HandshakeResponse> {
    if (!this.handshake) {
      this.handshake = this.performHandshake();
    }

    return await this.handshake;
  }

  private async performHandshake(): Promise<HandshakeResponse> {
    const response = await this.transport.send(
      createHandshakeRequest(this.nextRequestId()),
    );

    return assertHandshakeResponse(response);
  }

  private nextRequestId(): string {
    if (this.options.requestIdFactory) {
      return this.options.requestIdFactory();
    }

    this.requestOrdinal += 1;
    return `${SOLIDWORKS_WORKER_PROTOCOL_VERSION}-${this.requestOrdinal}`;
  }
}
