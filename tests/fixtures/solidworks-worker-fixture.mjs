import readline from 'node:readline';

const rl = readline.createInterface({
  input: process.stdin,
  crlfDelay: Infinity,
});

for await (const line of rl) {
  if (!line.trim()) {
    continue;
  }

  const request = JSON.parse(line);

  if (request.messageType === 'handshake_request') {
    process.stdout.write(
      `${JSON.stringify({
        messageType: 'handshake_response',
        requestId: request.requestId,
        protocolVersion: request.protocolVersion,
        worker: {
          name: 'fixture-worker',
          version: '0.1.0',
        },
        supportedSolidWorksMajorVersions: [2022],
        capabilities: {
          supportsStateRoundTrip: true,
          supportsObservedStateOnFailure: true,
        },
      })}\n`,
    );
    continue;
  }

  if (request.messageType === 'execute_command_request') {
    if (request.command.kind === 'new_part') {
      process.stdout.write(
        `${JSON.stringify({
          messageType: 'execute_command_response',
          requestId: request.requestId,
          protocolVersion: request.protocolVersion,
          ok: true,
          nextState: {
            ...request.stateBefore,
            backendMode: 'solidworks',
            currentDocument: {
              documentType: 'part',
              name: request.command.name ?? 'Part1',
              units: 'mm',
              sketches: [],
              features: [],
              exports: [],
              modified: false,
              baselineVersion: '2022',
            },
          },
          execution: {
            backendName: 'fixture-worker',
            durationMs: 5,
            solidWorksMajorVersion: 2022,
          },
        })}\n`,
      );
      continue;
    }

    if (request.command.kind === 'start_sketch') {
      process.stdout.write(
        `${JSON.stringify({
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
              source: 'fixture-worker',
            },
          },
          observedState: request.stateBefore,
          resyncRequired: true,
        })}\n`,
      );
      continue;
    }

    process.stdout.write(
      `${JSON.stringify({
        messageType: 'execute_command_response',
        requestId: request.requestId,
        protocolVersion: request.protocolVersion,
        ok: false,
        error: {
          code: 'unsupported_operation',
          message: `Fixture does not implement command ${request.command.kind}.`,
          retryable: false,
          details: {},
        },
        resyncRequired: false,
      })}\n`,
    );
    continue;
  }

  if (request.messageType === 'shutdown_request') {
    process.stdout.write(
      `${JSON.stringify({
        messageType: 'shutdown_response',
        requestId: request.requestId,
        protocolVersion: request.protocolVersion,
      })}\n`,
    );
    process.exit(0);
  }
}
