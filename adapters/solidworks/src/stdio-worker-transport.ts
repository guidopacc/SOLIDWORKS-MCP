import { type ChildProcessWithoutNullStreams, spawn } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { EOL } from 'node:os';
import { internalError } from '../../../mcp-server/src/domain/errors.js';
import type {
  ShutdownResponse,
  SolidWorksWorkerRequest,
  SolidWorksWorkerResponse,
  SolidWorksWorkerTransport,
} from './worker-protocol.js';
import { SOLIDWORKS_WORKER_PROTOCOL_VERSION } from './worker-protocol.js';

export interface StdioWorkerTransportOptions {
  command: string;
  args?: string[];
  cwd?: string;
  env?: NodeJS.ProcessEnv;
  responseTimeoutMs?: number;
  shutdownTimeoutMs?: number;
}

interface PendingRequest {
  resolve: (response: SolidWorksWorkerResponse) => void;
  reject: (error: Error) => void;
  timeout: NodeJS.Timeout | undefined;
}

export class StdioWorkerTransport implements SolidWorksWorkerTransport {
  private readonly responseTimeoutMs: number;
  private readonly shutdownTimeoutMs: number;
  private readonly pending = new Map<string, PendingRequest>();
  private readonly stderrLines: string[] = [];

  private process: ChildProcessWithoutNullStreams | null = null;
  private stdoutBuffer = '';
  private closed = false;

  constructor(private readonly options: StdioWorkerTransportOptions) {
    this.responseTimeoutMs = options.responseTimeoutMs ?? 5_000;
    this.shutdownTimeoutMs = options.shutdownTimeoutMs ?? 1_500;
  }

  async send(
    request: SolidWorksWorkerRequest,
  ): Promise<SolidWorksWorkerResponse> {
    if (this.closed) {
      throw internalError('Worker transport is already closed.');
    }

    const workerProcess = this.ensureProcess();
    const payload = `${JSON.stringify(request)}\n`;

    return await new Promise<SolidWorksWorkerResponse>((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pending.delete(request.requestId);
        reject(
          internalError('Worker response timed out.', {
            requestId: request.requestId,
            messageType: request.messageType,
            timeoutMs: this.responseTimeoutMs,
            stderrTail: this.stderrLines.slice(-10),
          }),
        );
      }, this.responseTimeoutMs);

      this.pending.set(request.requestId, {
        resolve,
        reject,
        timeout,
      });

      workerProcess.stdin.write(payload, (error) => {
        if (!error) {
          return;
        }

        this.clearPending(request.requestId);
        reject(
          internalError('Failed to write request to worker stdin.', {
            requestId: request.requestId,
            messageType: request.messageType,
            cause: error.message,
          }),
        );
      });
    });
  }

  async close(): Promise<void> {
    if (this.closed) {
      return;
    }

    this.closed = true;

    if (!this.process) {
      return;
    }

    const workerProcess = this.process;
    this.process = null;

    try {
      const response = await this.sendShutdownRequest(workerProcess);
      if (response.messageType !== 'shutdown_response') {
        throw internalError('Worker returned a non-shutdown response.', {
          messageType: response.messageType,
        });
      }
    } catch {
      workerProcess.kill();
    }

    await new Promise<void>((resolve) => {
      const timer = setTimeout(() => {
        workerProcess.kill();
        resolve();
      }, this.shutdownTimeoutMs);

      workerProcess.once('exit', () => {
        clearTimeout(timer);
        resolve();
      });
    });

    this.rejectAllPending(
      internalError(
        'Worker transport closed before pending requests completed.',
      ),
    );
  }

  private ensureProcess(): ChildProcessWithoutNullStreams {
    if (this.process) {
      return this.process;
    }

    const workerProcess = spawn(this.options.command, this.options.args ?? [], {
      cwd: this.options.cwd,
      env: this.options.env,
      stdio: 'pipe',
    });

    workerProcess.stdout.setEncoding('utf8');
    workerProcess.stderr.setEncoding('utf8');
    workerProcess.stdout.on('data', (chunk: string) => {
      this.handleStdout(chunk);
    });
    workerProcess.stderr.on('data', (chunk: string) => {
      this.handleStderr(chunk);
    });
    workerProcess.once('error', (error) => {
      this.rejectAllPending(
        internalError('Worker process failed to start.', {
          cause: error.message,
          command: this.options.command,
          args: this.options.args ?? [],
        }),
      );
    });
    workerProcess.once('exit', (code, signal) => {
      this.rejectAllPending(
        internalError('Worker process exited before completing all requests.', {
          exitCode: code,
          signal,
          stderrTail: this.stderrLines.slice(-10),
        }),
      );
      this.process = null;
    });

    this.process = workerProcess;
    return workerProcess;
  }

  private handleStdout(chunk: string): void {
    this.stdoutBuffer += chunk;
    const lines = this.stdoutBuffer.split(/\r?\n/);
    this.stdoutBuffer = lines.pop() ?? '';

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) {
        continue;
      }

      let response: SolidWorksWorkerResponse;
      try {
        response = JSON.parse(trimmed) as SolidWorksWorkerResponse;
      } catch (error) {
        this.rejectAllPending(
          internalError('Worker emitted invalid JSON.', {
            line: trimmed,
            cause: error instanceof Error ? error.message : String(error),
          }),
        );
        continue;
      }

      const pending = this.pending.get(response.requestId);
      if (!pending) {
        continue;
      }

      clearTimeout(pending.timeout);
      this.pending.delete(response.requestId);
      pending.resolve(response);
    }
  }

  private handleStderr(chunk: string): void {
    const lines = chunk.split(/\r?\n/).map((line) => line.trim());
    for (const line of lines) {
      if (!line) {
        continue;
      }

      this.stderrLines.push(line);
      if (this.stderrLines.length > 50) {
        this.stderrLines.shift();
      }
    }
  }

  private rejectAllPending(error: Error): void {
    for (const [requestId, pending] of this.pending) {
      clearTimeout(pending.timeout);
      pending.reject(error);
      this.pending.delete(requestId);
    }
  }

  private clearPending(requestId: string): void {
    const pending = this.pending.get(requestId);
    if (!pending) {
      return;
    }

    clearTimeout(pending.timeout);
    this.pending.delete(requestId);
  }

  private async sendShutdownRequest(
    workerProcess: ChildProcessWithoutNullStreams,
  ): Promise<ShutdownResponse> {
    const requestId = randomUUID();
    const payload = `${JSON.stringify({
      messageType: 'shutdown_request',
      requestId,
      protocolVersion: SOLIDWORKS_WORKER_PROTOCOL_VERSION,
    })}${EOL}`;

    return await new Promise<ShutdownResponse>((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pending.delete(requestId);
        reject(
          internalError('Worker shutdown timed out.', {
            requestId,
            timeoutMs: this.shutdownTimeoutMs,
          }),
        );
      }, this.shutdownTimeoutMs);

      this.pending.set(requestId, {
        resolve: (response) => resolve(response as ShutdownResponse),
        reject,
        timeout,
      });

      workerProcess.stdin.write(payload, (error) => {
        if (!error) {
          return;
        }

        this.clearPending(requestId);
        reject(
          internalError('Failed to write shutdown request to worker stdin.', {
            requestId,
            cause: error.message,
          }),
        );
      });
    });
  }
}
