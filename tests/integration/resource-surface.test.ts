import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { InMemoryTransport } from '@modelcontextprotocol/sdk/inMemory.js';
import { describe, expect, it } from 'vitest';
import {
  CURRENT_DOCUMENT_STATE_RESOURCE_URI,
  PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI,
} from '../../mcp-server/src/resources/resource-definitions.js';
import { createSolidWorksMcpServer } from '../../mcp-server/src/server.js';

describe('resource surface', () => {
  it('exposes the two public alpha resources in MCP discovery', async () => {
    const { client, close, serverContext } = await connectResourceTestClient();

    try {
      const result = await client.listResources();

      expect(result.resources).toHaveLength(2);
      expect(result.resources[0]).toMatchObject({
        uri: CURRENT_DOCUMENT_STATE_RESOURCE_URI,
        name: 'current_document_state',
        title: 'Current Document State',
      });
      expect(result.resources[1]).toMatchObject({
        uri: PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI,
        name: 'public_alpha_boundary',
        title: 'Public Alpha Boundary',
      });
      expect(client.getServerCapabilities()?.resources).toBeDefined();
      expect(client.getInstructions()).toContain(
        'solidworks://document/current-state',
      );
      expect(client.getInstructions()).toContain(
        'solidworks://alpha/public-boundary',
      );
    } finally {
      await close();
      await serverContext.dispose();
    }
  });

  it('returns a read-only current document snapshot derived from the validated state model', async () => {
    const { client, close, serverContext } = await connectResourceTestClient();

    try {
      await client.callTool({
        name: 'new_part',
        arguments: {
          name: 'ResourceDemo',
        },
      });
      await client.callTool({
        name: 'select_plane',
        arguments: {
          plane: 'Front Plane',
        },
      });
      await client.callTool({
        name: 'start_sketch',
        arguments: {},
      });
      await client.callTool({
        name: 'draw_circle',
        arguments: {
          center: { x: 0, y: 0 },
          radius: 10,
        },
      });

      const result = await client.readResource({
        uri: CURRENT_DOCUMENT_STATE_RESOURCE_URI,
      });
      const resourceContent = result.contents[0];
      const payload = JSON.parse(
        resourceContent && 'text' in resourceContent
          ? resourceContent.text
          : 'null',
      );

      expect(resourceContent?.uri).toBe(CURRENT_DOCUMENT_STATE_RESOURCE_URI);
      expect(payload.resourceType).toBe('current_document_state');
      expect(payload.readOnly).toBe(true);
      expect(payload.queryEquivalent).toBe('get_document_state');
      expect(payload.promptCompanion).toBe('safe_modeling_session');
      expect(payload.documentPresent).toBe(true);
      expect(payload.documentState).toMatchObject({
        name: 'ResourceDemo',
        documentType: 'part',
        selectedPlane: 'Front Plane',
        activeSketchId: 'sketch-1',
      });
      expect(payload.documentSummary).toMatchObject({
        name: 'ResourceDemo',
        isSaved: false,
        sketchCount: 1,
        openSketchCount: 1,
        featureCount: 0,
        exportCount: 0,
      });
    } finally {
      await close();
      await serverContext.dispose();
    }
  });

  it('returns a static public alpha boundary resource that complements the dynamic session resource', async () => {
    const { client, close, serverContext } = await connectResourceTestClient();

    try {
      const result = await client.readResource({
        uri: PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI,
      });
      const resourceContent = result.contents[0];
      const payload = JSON.parse(
        resourceContent && 'text' in resourceContent
          ? resourceContent.text
          : 'null',
      );

      expect(resourceContent?.uri).toBe(PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI);
      expect(payload.resourceType).toBe('public_alpha_boundary');
      expect(payload.readOnly).toBe(true);
      expect(payload.scope).toBe('public_alpha');
      expect(payload.validatedOn).toMatchObject({
        operatingSystem: 'Windows',
        solidworksBaseline: '2022',
      });
      expect(payload.supportedSurfaces.tools).toContain('new_part');
      expect(payload.supportedSurfaces.prompts).toContain(
        'safe_modeling_session',
      );
      expect(payload.supportedSurfaces.resources).toEqual([
        'current_document_state',
        'public_alpha_boundary',
      ]);
      expect(payload.workflowShape.coreSequence).toEqual([
        'new_part',
        'select_plane',
        'start_sketch',
        'close_sketch',
        'extrude_boss',
        'save_part',
        'export_step',
      ]);
      expect(payload.workflowShape.allowedSketchPrimitives).toEqual([
        'draw_line',
        'draw_circle',
        'draw_centered_rectangle',
      ]);
      expect(payload.companionReferences).toMatchObject({
        prompt: 'safe_modeling_session',
        dynamicResource: 'current_document_state',
        dynamicResourceUri: CURRENT_DOCUMENT_STATE_RESOURCE_URI,
      });
      expect(payload.explicitOutOfScope).toContain('add_dimension');
      expect(payload.explicitOutOfScope).toContain('open_or_reopen_documents');
      expect(payload.knownLimits).toContain(
        'repo_first_alpha_not_installer_grade',
      );
    } finally {
      await close();
      await serverContext.dispose();
    }
  });
});

async function connectResourceTestClient(): Promise<{
  client: Client;
  close: () => Promise<void>;
  serverContext: ReturnType<typeof createSolidWorksMcpServer>;
}> {
  const serverContext = createSolidWorksMcpServer();
  const client = new Client(
    {
      name: 'resource-surface-test-client',
      version: '0.1.0',
    },
    {
      capabilities: {},
    },
  );
  const [clientTransport, serverTransport] =
    InMemoryTransport.createLinkedPair();

  await Promise.all([
    serverContext.server.connect(serverTransport),
    client.connect(clientTransport),
  ]);

  return {
    client,
    close: async (): Promise<void> => {
      await Promise.allSettled([client.close(), serverContext.server.close()]);
    },
    serverContext,
  };
}
