import { describe, expect, it } from 'vitest';
import { MockCadBackend } from '../../adapters/mock/src/mock-cad-backend.js';
import { ProjectService } from '../../mcp-server/src/application/project-service.js';
import { PUBLIC_ALPHA_TOOL_DEFINITIONS } from '../../mcp-server/src/tools/tool-definitions.js';

describe('ProjectService integration', () => {
  it('drives an end-to-end mock workflow and exposes document state', async () => {
    const service = new ProjectService(new MockCadBackend());
    service.bootstrapAvailableTools(
      PUBLIC_ALPHA_TOOL_DEFINITIONS.map((tool) => tool.name),
    );

    await service.execute({ kind: 'new_part', name: 'Support' });
    await service.execute({ kind: 'select_plane', plane: 'Front Plane' });
    await service.execute({ kind: 'start_sketch' });
    await service.execute({
      kind: 'draw_centered_rectangle',
      center: { x: 0, y: 0 },
      corner: { x: 10, y: 5 },
    });
    await service.execute({ kind: 'close_sketch' });
    const result = await service.execute({
      kind: 'extrude_boss',
      sketchId: 'sketch-1',
      depth: 6,
    });

    expect(result.command).toBe('extrude_boss');
    expect(result.documentState?.features).toHaveLength(1);
    expect(result.documentState?.features[0]).toMatchObject({
      kind: 'extrude_boss',
      depth: 6,
    });
  });

  it('reads project and document resources', async () => {
    const service = new ProjectService(new MockCadBackend());
    service.bootstrapAvailableTools(
      PUBLIC_ALPHA_TOOL_DEFINITIONS.map((tool) => tool.name),
    );

    await service.execute({ kind: 'new_part', name: 'Housing' });

    const status = JSON.parse(
      service.readResource('solidworks://project/status'),
    );
    const document = JSON.parse(
      service.readResource('solidworks://document/current-state'),
    );
    const boundary = JSON.parse(
      service.readResource('solidworks://alpha/public-boundary'),
    );
    const catalog = JSON.parse(
      service.readResource('solidworks://tools/catalog'),
    );

    expect(status.currentMilestone).toContain(
      'Developer/public alpha readiness',
    );
    expect(status.alphaReadiness.publicAlphaSurface).toContain('draw_circle');
    expect(status.resourceReadiness.publicResourceSurface).toContain(
      'current_document_state',
    );
    expect(status.resourceReadiness.publicResourceSurface).toContain(
      'public_alpha_boundary',
    );
    expect(status.alphaReadiness.notYetSupported).toContain('add_dimension');
    expect(document.resourceType).toBe('current_document_state');
    expect(document.queryEquivalent).toBe('get_document_state');
    expect(document.documentState.name).toBe('Housing');
    expect(document.documentSummary).toMatchObject({
      name: 'Housing',
      isSaved: false,
      sketchCount: 0,
      featureCount: 0,
      exportCount: 0,
    });
    expect(boundary.resourceType).toBe('public_alpha_boundary');
    expect(boundary.supportedSurfaces.tools).toContain('draw_circle');
    expect(boundary.supportedSurfaces.prompts).toContain(
      'safe_modeling_session',
    );
    expect(boundary.supportedSurfaces.resources).toContain(
      'current_document_state',
    );
    expect(boundary.supportedSurfaces.resources).toContain(
      'public_alpha_boundary',
    );
    expect(boundary.explicitOutOfScope).toContain('add_dimension');
    expect(catalog.tools).toContain('new_part');
    expect(catalog.tools).not.toContain('add_dimension');
  });
});
