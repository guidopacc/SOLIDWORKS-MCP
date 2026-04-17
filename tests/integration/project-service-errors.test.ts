import { describe, expect, it } from 'vitest';
import { MockCadBackend } from '../../adapters/mock/src/mock-cad-backend.js';
import { ProjectService } from '../../mcp-server/src/application/project-service.js';
import {
  formatToolErrorEnvelope,
  preconditionFailed,
} from '../../mcp-server/src/domain/errors.js';
import { PUBLIC_ALPHA_TOOL_DEFINITIONS } from '../../mcp-server/src/tools/tool-definitions.js';

describe('ProjectService error handling', () => {
  it('does not mutate state when a command fails', async () => {
    const service = new ProjectService(new MockCadBackend());
    service.bootstrapAvailableTools(
      PUBLIC_ALPHA_TOOL_DEFINITIONS.map((tool) => tool.name),
    );

    await service.execute({ kind: 'new_part', name: 'FailureProbe' });
    const before = service.getState();

    await expect(
      service.execute({
        kind: 'extrude_boss',
        sketchId: 'sketch-404',
        depth: 5,
      }),
    ).rejects.toMatchObject({
      code: 'not_found',
    });

    expect(service.getState()).toEqual(before);
  });

  it('formats structured tool error envelopes', () => {
    const envelope = formatToolErrorEnvelope(
      preconditionFailed('Plane selection is required.', {
        command: 'start_sketch',
      }),
    );

    expect(envelope).toEqual({
      ok: false,
      error: {
        code: 'precondition_failed',
        message: 'Plane selection is required.',
        retryable: false,
        details: {
          command: 'start_sketch',
        },
      },
    });
  });
});
